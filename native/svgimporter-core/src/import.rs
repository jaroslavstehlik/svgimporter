//! Parses an SVG with usvg and tessellates every fill and stroke with lyon.
//!
//! Each fill or stroke becomes one layer, in painting order, followed by its antialiasing fringe
//! when antialiasing is on (see `fringe`). Vertices are in SVG user units with
//! all transforms applied (y pointing down). Unity-specific processing (scaling, pivot, render
//! formats, gradients in the mesh) happens in C#.
//!
//! Clip paths are applied to the geometry: a clipped fill or stroke is intersected with the clip
//! region in document space and tessellated again.

use std::collections::{HashMap, HashSet};
use std::sync::{Arc, OnceLock};

use i_overlay::core::fill_rule::FillRule as OverlayFillRule;
use i_overlay::core::overlay_rule::OverlayRule;
use i_overlay::float::simplify::SimplifyShape;
use i_overlay::float::single::SingleFloatOverlay;
use lyon_tessellation::math::point;
use lyon_tessellation::path::iterator::PathIterator;
use lyon_tessellation::path::PathEvent;
use lyon_tessellation::path::Path as LyonPath;
use lyon_tessellation::{
    BuffersBuilder, FillOptions, FillTessellator, FillVertex, StrokeOptions, StrokeTessellator,
    StrokeVertex, VertexBuffers,
};
use tiny_skia_path::{PathSegment, Transform};

use crate::ffi::*;
use crate::fringe;

pub struct ImportOptions {
    pub tolerance: f32,
    pub dpi: f32,
    pub convert_text: bool,
    pub antialiasing: bool,
}

#[derive(Default)]
pub struct Output {
    pub width: f32,
    pub height: f32,
    pub layers: Vec<SvgiLayer>,
    pub vertices: Vec<f32>,
    pub indices: Vec<u32>,
    pub paints: Vec<SvgiPaint>,
    pub stops: Vec<SvgiStop>,
    pub strings: Vec<u8>,
    pub contours: Vec<SvgiContour>,
    pub contour_points: Vec<f32>,
    /// With antialiasing, per vertex: the extrusion vector (two floats) and the coverage.
    pub extrusions: Vec<f32>,
    pub coverage: Vec<f32>,
    pub diagnostics: u32,
}

/// System fonts, loaded once per process: loading them takes a noticeable fraction of a second.
fn system_fonts() -> Arc<usvg::fontdb::Database> {
    static FONTS: OnceLock<Arc<usvg::fontdb::Database>> = OnceLock::new();
    FONTS
        .get_or_init(|| {
            let mut db = usvg::fontdb::Database::new();
            db.load_system_fonts();
            Arc::new(db)
        })
        .clone()
}

/// Limits (DESIGN.md, 6.6): a document that exceeds one is an import error, not a hang or an
/// out-of-memory abort of the host. They also keep every count in the C ABI within `u32`.
pub const MAX_INPUT_BYTES: usize = 64 * 1024 * 1024;
pub const MAX_VERTICES: usize = 16 * 1024 * 1024;
pub const MAX_INDICES: usize = 48 * 1024 * 1024;
/// Curves are flattened no finer than this fraction of a path's size.
const MIN_RELATIVE_TOLERANCE: f32 = 1e-6;
/// Dash patterns that would produce more dashes than this are refused.
const MAX_DASHES: f32 = 1_000_000.0;

pub fn import(data: &[u8], options: &ImportOptions) -> Result<Output, String> {
    if data.len() > MAX_INPUT_BYTES {
        return Err(format!("the document is larger than {} MB", MAX_INPUT_BYTES / (1024 * 1024)));
    }
    let mut usvg_options = usvg::Options {
        dpi: options.dpi,
        ..usvg::Options::default()
    };
    if options.convert_text {
        usvg_options.fontdb = system_fonts();
    }
    let conical = conical_gradients(data);
    let tree = match &conical {
        Some((renamed, _)) => usvg::Tree::from_data(renamed, &usvg_options),
        None => usvg::Tree::from_data(data, &usvg_options),
    }
    .map_err(|e| e.to_string())?;

    let mut importer = Importer {
        options,
        output: Output {
            width: tree.size().width(),
            height: tree.size().height(),
            ..Output::default()
        },
        paint_ids: HashMap::new(),
        element: 0,
        fill: FillTessellator::new(),
        stroke: StrokeTessellator::new(),
        clip: None,
        limit_exceeded: None,
        conical_ids: conical.map(|(_, ids)| ids).unwrap_or_default(),
    };
    importer.group(tree.root(), 1.0);
    match importer.limit_exceeded {
        Some(limit) => Err(limit),
        None => Ok(importer.output),
    }
}

struct Importer<'a> {
    options: &'a ImportOptions,
    output: Output,
    /// Shared gradients, by `Arc` address, so each is output once.
    paint_ids: HashMap<usize, u32>,
    /// Index of the current path, shared by its fill and stroke layers.
    element: u32,
    fill: FillTessellator,
    stroke: StrokeTessellator,
    /// The clip region of the current group in document space, or None if nothing clips it.
    clip: Option<Region>,
    /// Set when the output reaches a limit; the walk stops and the import fails with this message.
    limit_exceeded: Option<String>,
    /// Ids of the `conicalGradient` elements, which usvg reads as radial gradients.
    conical_ids: HashSet<String>,
}

/// An area in document space: closed contours, without overlaps, holes wound opposite to their
/// outer contour, so it can be filled with either fill rule.
type Region = Vec<Vec<[f64; 2]>>;

impl Importer<'_> {
    fn group(&mut self, group: &usvg::Group, opacity: f32) {
        // A clip path clips the group's content in the group's coordinates, and so does a mask
        // that is a plain shape. Nested clips intersect.
        let mut region = group.clip_path().map(|clip_path| self.clip_region(clip_path, &group.abs_transform()));
        if let Some(mask) = group.mask() {
            match self.mask_region(mask, &group.abs_transform()) {
                Some(mask_region) => {
                    region = Some(match region {
                        Some(r) => intersect(&r, &mask_region),
                        None => mask_region,
                    })
                }
                // Not representable as geometry: drawn unmasked.
                None => self.output.diagnostics |= SVGI_DIAG_MASK,
            }
        }
        let outer_clip = region.map(|region| {
            let region = match &self.clip {
                Some(outer) => intersect(outer, &region),
                None => region,
            };
            std::mem::replace(&mut self.clip, Some(region))
        });
        if self.clip.as_ref().map_or(false, |c| c.is_empty()) {
            // Clipped away entirely.
            if let Some(outer) = outer_clip {
                self.clip = outer;
            }
            return;
        }

        if !group.filters().is_empty() {
            self.output.diagnostics |= SVGI_DIAG_FILTER;
        }
        if group.blend_mode() != usvg::BlendMode::Normal {
            self.output.diagnostics |= SVGI_DIAG_BLEND_MODE;
        }
        let group_opacity = group.opacity().get();
        if group_opacity < 1.0 && group.children().len() > 1 {
            // Applied per child, which differs where children overlap (DESIGN.md, section 16).
            self.output.diagnostics |= SVGI_DIAG_GROUP_OPACITY;
        }
        let opacity = opacity * group_opacity;

        for node in group.children() {
            if self.limit_exceeded.is_some() {
                break;
            }
            match node {
                usvg::Node::Group(child) => self.group(child, opacity),
                usvg::Node::Path(path) => self.path(path, opacity),
                usvg::Node::Image(_) => self.output.diagnostics |= SVGI_DIAG_IMAGE,
                usvg::Node::Text(text) => {
                    if self.options.convert_text {
                        self.group(text.flattened(), opacity);
                    } else {
                        self.output.diagnostics |= SVGI_DIAG_TEXT_SKIPPED;
                    }
                }
            }
        }

        if let Some(outer) = outer_clip {
            self.clip = outer;
        }
    }

    /// The area a clip path lets through, in document space. `transform` is the absolute
    /// transform of the element the clip path is applied to.
    fn clip_region(&mut self, clip_path: &usvg::ClipPath, transform: &Transform) -> Region {
        let mut contours = Vec::new();
        self.clip_contours(clip_path.root(), &transform.pre_concat(clip_path.transform()), &mut contours);
        // Each child was simplified with its own clip rule; their union is the clip region.
        let mut region = flatten_shapes(contours.simplify_shape(OverlayFillRule::NonZero));
        // A clip path can itself be clipped, in the same coordinates as the element.
        if let Some(inner) = clip_path.clip_path() {
            let inner = self.clip_region(inner, transform);
            region = intersect(&region, &inner);
        }
        region
    }

    /// The area a mask lets through, if the mask is a plain shape: its content is opaque and,
    /// for a luminance mask, white, so it passes everything it covers. None for other masks,
    /// which would need per-pixel alpha.
    fn mask_region(&mut self, mask: &usvg::Mask, transform: &Transform) -> Option<Region> {
        let mut contours = Vec::new();
        if !self.mask_contours(mask.root(), mask.kind(), transform, &mut contours) {
            return None;
        }
        let r = mask.rect();
        let rect = vec![[r.left(), r.top()], [r.right(), r.top()], [r.right(), r.bottom()], [r.left(), r.bottom()]]
            .into_iter()
            .map(|[x, y]| {
                let (x, y) = apply(transform, x, y);
                [x as f64, y as f64]
            })
            .collect::<Vec<_>>();
        let mut region = intersect(&flatten_shapes(contours.simplify_shape(OverlayFillRule::NonZero)), &vec![rect]);
        // A mask can itself be masked, in the same coordinates as the element.
        if let Some(inner) = mask.mask() {
            let inner = self.mask_region(inner, transform)?;
            region = intersect(&region, &inner);
        }
        Some(region)
    }

    /// Adds the outlines of a mask's content; false if the content isn't a plain opaque shape.
    fn mask_contours(&mut self, group: &usvg::Group, kind: usvg::MaskType, transform: &Transform, contours: &mut Region) -> bool {
        for node in group.children() {
            match node {
                usvg::Node::Group(child) => {
                    let plain = child.opacity().get() == 1.0
                        && child.clip_path().is_none()
                        && child.mask().is_none()
                        && child.filters().is_empty()
                        && child.blend_mode() == usvg::BlendMode::Normal;
                    if !plain || !self.mask_contours(child, kind, transform, contours) {
                        return false;
                    }
                }
                usvg::Node::Path(path) => {
                    if !path.is_visible() {
                        continue;
                    }
                    if path.stroke().is_some() {
                        return false;
                    }
                    let Some(fill) = path.fill() else { continue };
                    let opaque = match fill.paint() {
                        usvg::Paint::Color(c) => {
                            fill.opacity().get() == 1.0
                                && (kind == usvg::MaskType::Alpha || (c.red == 255 && c.green == 255 && c.blue == 255))
                        }
                        _ => false,
                    };
                    if !opaque {
                        return false;
                    }
                    let t = transform.pre_concat(path.abs_transform());
                    let shape = flatten(path.data(), &t, self.options.tolerance).simplify_shape(overlay_rule(fill.rule()));
                    contours.extend(flatten_shapes(shape));
                }
                usvg::Node::Text(_) | usvg::Node::Image(_) => return false,
            }
        }
        true
    }

    fn clip_contours(&mut self, group: &usvg::Group, transform: &Transform, contours: &mut Region) {
        let mut own = Vec::new();
        for node in group.children() {
            match node {
                usvg::Node::Group(child) => self.clip_contours(child, transform, &mut own),
                usvg::Node::Path(path) => {
                    // usvg gives clip path children a fill with the clip rule.
                    let Some(fill) = path.fill() else { continue };
                    if !path.is_visible() {
                        continue;
                    }
                    let t = transform.pre_concat(path.abs_transform());
                    let shape = flatten(path.data(), &t, self.options.tolerance).simplify_shape(overlay_rule(fill.rule()));
                    own.extend(flatten_shapes(shape));
                }
                usvg::Node::Text(text) => {
                    if self.options.convert_text {
                        self.clip_contours(text.flattened(), transform, &mut own);
                    }
                }
                usvg::Node::Image(_) => {}
            }
        }
        match group.clip_path() {
            // An element inside a clip path can be clipped itself, in its own coordinates.
            Some(clip_path) => {
                let region = flatten_shapes(own.simplify_shape(OverlayFillRule::NonZero));
                let clip = self.clip_region(clip_path, &transform.pre_concat(group.abs_transform()));
                contours.extend(intersect(&region, &clip));
            }
            None => contours.extend(own),
        }
    }

    /// Fill tessellation of a region in document space.
    fn tessellate_region(&mut self, region: &Region) -> Option<VertexBuffers<[f32; 2], u32>> {
        let mut builder = LyonPath::builder();
        for contour in region {
            if contour.len() < 3 {
                continue;
            }
            builder.begin(point(contour[0][0] as f32, contour[0][1] as f32));
            for p in &contour[1..] {
                builder.line_to(point(p[0] as f32, p[1] as f32));
            }
            builder.end(true);
        }
        let mut buffers: VertexBuffers<[f32; 2], u32> = VertexBuffers::new();
        self.fill
            .tessellate_path(
                &builder.build(),
                &FillOptions::default().with_fill_rule(lyon_tessellation::FillRule::NonZero),
                &mut BuffersBuilder::new(&mut buffers, |v: FillVertex| v.position().to_array()),
            )
            .ok()?;
        Some(buffers)
    }

    fn path(&mut self, path: &usvg::Path, opacity: f32) {
        if !path.is_visible() {
            return;
        }
        let transform = path.abs_transform();
        let scale = transform_scale(&transform);
        if !(scale > 0.0) {
            return;
        }
        // Tessellate in the path's own coordinates, so strokes follow SVG semantics under
        // non-uniform transforms; the tolerance is converted into those coordinates. A tolerance
        // far below the path's size would only produce a flood of vertices.
        let bounds = path.data().bounds();
        let size = bounds.width().max(bounds.height());
        let tolerance = (self.options.tolerance / scale).max(size * MIN_RELATIVE_TOLERANCE);

        self.element += 1;
        let fill_first = path.paint_order() == usvg::PaintOrder::FillAndStroke;
        if fill_first {
            self.fill_layer(path, &transform, tolerance, opacity);
            self.stroke_layer(path, &transform, tolerance, opacity);
        } else {
            self.stroke_layer(path, &transform, tolerance, opacity);
            self.fill_layer(path, &transform, tolerance, opacity);
        }
    }

    fn fill_layer(&mut self, path: &usvg::Path, transform: &Transform, tolerance: f32, opacity: f32) {
        let Some(fill) = path.fill() else { return };
        let Some(paint) = self.paint(fill.paint(), transform) else { return };

        if let Some(clip) = &self.clip {
            let shape = flatten(path.data(), transform, self.options.tolerance).simplify_shape(overlay_rule(fill.rule()));
            let region = intersect(&flatten_shapes(shape), clip);
            if let Some(buffers) = self.tessellate_region(&region) {
                self.push_layer(path.id(), SVGI_LAYER_FILL, paint, opacity * fill.opacity().get(), &buffers, &Transform::identity());
            }
            return;
        }

        let lyon_path = to_lyon(path.data());
        let options = FillOptions::tolerance(tolerance).with_fill_rule(match fill.rule() {
            usvg::FillRule::NonZero => lyon_tessellation::FillRule::NonZero,
            usvg::FillRule::EvenOdd => lyon_tessellation::FillRule::EvenOdd,
        });
        let mut buffers: VertexBuffers<[f32; 2], u32> = VertexBuffers::new();
        let tessellated = self.fill.tessellate_path(
            &lyon_path,
            &options,
            &mut BuffersBuilder::new(&mut buffers, |v: FillVertex| v.position().to_array()),
        );
        if tessellated.is_ok() {
            self.push_layer(path.id(), SVGI_LAYER_FILL, paint, opacity * fill.opacity().get(), &buffers, transform);
        }
    }

    fn stroke_layer(&mut self, path: &usvg::Path, transform: &Transform, tolerance: f32, opacity: f32) {
        let Some(stroke) = path.stroke() else { return };
        let Some(paint) = self.paint(stroke.paint(), transform) else { return };

        // Dashes are applied to the path before stroking.
        if let Some(array) = stroke.dasharray() {
            let period: f32 = array.iter().sum();
            if period > 0.0 && outline_length(path.data()) / period > MAX_DASHES {
                self.limit_exceeded = Some(format!("a dash pattern produces more than {} dashes", MAX_DASHES));
                return;
            }
        }
        let dashed;
        let data = match stroke
            .dasharray()
            .and_then(|array| tiny_skia_path::StrokeDash::new(array.to_vec(), stroke.dashoffset()))
            .and_then(|dash| path.data().dash(&dash, 1.0))
        {
            Some(d) => {
                dashed = d;
                &dashed
            }
            None if stroke.dasharray().is_some() => return, // Dashes with no visible part.
            None => path.data(),
        };

        let lyon_path = to_lyon(data);
        let options = StrokeOptions::tolerance(tolerance)
            .with_line_width(stroke.width().get())
            // lyon bevels when the miter ratio (1 / sin(angle / 2), SVG's definition) exceeds
            // twice its limit, so it gets half of SVG's. Its minimum of 1 means SVG limits
            // below 2 behave like 2.
            .with_miter_limit((stroke.miterlimit().get() * 0.5).max(StrokeOptions::MINIMUM_MITER_LIMIT))
            .with_line_join(match stroke.linejoin() {
                usvg::LineJoin::Miter => lyon_tessellation::LineJoin::Miter,
                usvg::LineJoin::MiterClip => lyon_tessellation::LineJoin::MiterClip,
                usvg::LineJoin::Round => lyon_tessellation::LineJoin::Round,
                usvg::LineJoin::Bevel => lyon_tessellation::LineJoin::Bevel,
            })
            .with_line_cap(line_cap(stroke.linecap()));
        let mut buffers: VertexBuffers<[f32; 2], u32> = VertexBuffers::new();
        let tessellated = self.stroke.tessellate_path(
            &lyon_path,
            &options,
            &mut BuffersBuilder::new(&mut buffers, |v: StrokeVertex| v.position().to_array()),
        );
        if tessellated.is_err() {
            return;
        }
        if let Some(clip) = &self.clip {
            // The stroke's triangles, consistently wound so that their union is the stroke's
            // area, intersected with the clip region.
            let triangles: Region = buffers
                .indices
                .chunks_exact(3)
                .map(|t| {
                    let mut triangle: Vec<[f64; 2]> = t
                        .iter()
                        .map(|&i| {
                            let v = buffers.vertices[i as usize];
                            let (x, y) = apply(transform, v[0], v[1]);
                            [x as f64, y as f64]
                        })
                        .collect();
                    if signed_area(&triangle) < 0.0 {
                        triangle.swap(1, 2);
                    }
                    triangle
                })
                .collect();
            let region = flatten_shapes(triangles.overlay(clip, OverlayRule::Intersect, OverlayFillRule::NonZero));
            if let Some(buffers) = self.tessellate_region(&region) {
                self.push_layer(path.id(), SVGI_LAYER_STROKE, paint, opacity * stroke.opacity().get(), &buffers, &Transform::identity());
            }
            return;
        }
        self.push_layer(path.id(), SVGI_LAYER_STROKE, paint, opacity * stroke.opacity().get(), &buffers, transform);
    }

    fn push_layer(
        &mut self,
        id: &str,
        kind: u8,
        paint: u32,
        opacity: f32,
        buffers: &VertexBuffers<[f32; 2], u32>,
        transform: &Transform,
    ) {
        if buffers.indices.is_empty() {
            return;
        }
        if self.output.vertices.len() / 2 + buffers.vertices.len() > MAX_VERTICES
            || self.output.indices.len() + buffers.indices.len() > MAX_INDICES
        {
            self.limit_exceeded = Some(format!("the document produces more than {} vertices or {} indices", MAX_VERTICES, MAX_INDICES));
            return;
        }
        // Only the vertices triangles use: lyon keeps the points of zero-area spans, which would
        // otherwise widen the layer's bounds.
        let mut remap = vec![u32::MAX; buffers.vertices.len()];
        let mut positions = Vec::with_capacity(buffers.vertices.len());
        let mut indices = Vec::with_capacity(buffers.indices.len());
        for &i in &buffers.indices {
            if remap[i as usize] == u32::MAX {
                remap[i as usize] = positions.len() as u32;
                let v = buffers.vertices[i as usize];
                let (x, y) = apply(transform, v[0], v[1]);
                positions.push([x, y]);
            }
            indices.push(remap[i as usize]);
        }
        let loops = boundary_loops(&positions, &indices);

        let out = &mut self.output;
        let contour_offset = out.contours.len() as u32;
        for contour in &loops {
            out.contours.push(SvgiContour {
                point_offset: (out.contour_points.len() / 2) as u32,
                point_count: contour.len() as u32,
            });
            for p in contour {
                out.contour_points.push(p[0]);
                out.contour_points.push(p[1]);
            }
        }
        let layer = SvgiLayer {
            vertex_offset: (out.vertices.len() / 2) as u32,
            vertex_count: positions.len() as u32,
            index_offset: out.indices.len() as u32,
            index_count: indices.len() as u32,
            paint,
            kind,
            opacity,
            name_offset: out.strings.len() as u32,
            name_length: id.len() as u32,
            element: self.element - 1,
            contour_offset,
            contour_count: loops.len() as u32,
        };
        for p in &positions {
            out.vertices.push(p[0]);
            out.vertices.push(p[1]);
        }
        // Consistent winding: positive area in these y-down coordinates, which is clockwise on
        // screen, the front face in Unity. lyon doesn't guarantee an orientation, and shaders
        // that cull back faces would otherwise drop triangles.
        for t in indices.chunks_exact(3) {
            let (a, b, c) = (positions[t[0] as usize], positions[t[1] as usize], positions[t[2] as usize]);
            let area = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
            if area < 0.0 {
                out.indices.extend_from_slice(&[t[0], t[2], t[1]]);
            } else {
                out.indices.extend_from_slice(t);
            }
        }
        out.strings.extend_from_slice(id.as_bytes());
        out.layers.push(layer);

        if self.options.antialiasing {
            let fringe = fringe::build(&loops);
            for p in &positions {
                let e = fringe.extrusion_of(*p);
                out.extrusions.extend_from_slice(&e);
                out.coverage.push(1.0);
            }
            if !fringe.indices.is_empty() {
                out.layers.push(SvgiLayer {
                    vertex_offset: (out.vertices.len() / 2) as u32,
                    vertex_count: fringe.positions.len() as u32,
                    index_offset: out.indices.len() as u32,
                    index_count: fringe.indices.len() as u32,
                    kind: SVGI_LAYER_FRINGE,
                    contour_offset: out.contours.len() as u32,
                    contour_count: 0,
                    ..layer
                });
                for (p, e) in fringe.positions.iter().zip(&fringe.extrusions) {
                    out.vertices.extend_from_slice(p);
                    out.extrusions.extend_from_slice(e);
                }
                out.coverage.extend_from_slice(&fringe.coverage);
                out.indices.extend_from_slice(&fringe.indices);
            }
            if out.vertices.len() / 2 > MAX_VERTICES || out.indices.len() > MAX_INDICES {
                self.limit_exceeded = Some(format!("the document produces more than {} vertices or {} indices", MAX_VERTICES, MAX_INDICES));
            }
        }
    }

    /// Returns the paint's index, or None for paints that can't be rendered (patterns).
    fn paint(&mut self, paint: &usvg::Paint, transform: &Transform) -> Option<u32> {
        let (key, svgi) = match paint {
            usvg::Paint::Color(c) => {
                let paint = SvgiPaint {
                    kind: SVGI_PAINT_SOLID,
                    color: [c.red as f32 / 255.0, c.green as f32 / 255.0, c.blue as f32 / 255.0, 1.0],
                    ..SvgiPaint::default()
                };
                (None, paint)
            }
            usvg::Paint::LinearGradient(g) => {
                let paint = self.gradient(
                    SVGI_PAINT_LINEAR,
                    g,
                    [g.x1(), g.y1(), g.x2(), g.y2(), 0.0, 0.0],
                    transform,
                );
                // Gradients are shared between paths, but their transform depends on the path,
                // so the key includes the path's transform.
                (Some(key_of(Arc::as_ptr(g) as usize, transform)), paint)
            }
            usvg::Paint::RadialGradient(g) => {
                let kind = if self.conical_ids.contains(g.id()) { SVGI_PAINT_CONICAL } else { SVGI_PAINT_RADIAL };
                let paint = self.gradient(
                    kind,
                    g,
                    [g.cx(), g.cy(), g.r().get(), g.fx(), g.fy(), g.fr().get()],
                    transform,
                );
                (Some(key_of(Arc::as_ptr(g) as usize, transform)), paint)
            }
            usvg::Paint::Pattern(_) => {
                self.output.diagnostics |= SVGI_DIAG_PATTERN;
                return None;
            }
        };
        if let Some(key) = key {
            if let Some(&index) = self.paint_ids.get(&key) {
                return Some(index);
            }
            let index = self.output.paints.len() as u32;
            self.output.paints.push(svgi);
            self.paint_ids.insert(key, index);
            Some(index)
        } else {
            let index = self.output.paints.len() as u32;
            self.output.paints.push(svgi);
            Some(index)
        }
    }

    fn gradient(&mut self, kind: u8, gradient: &usvg::BaseGradient, geometry: [f32; 6], transform: &Transform) -> SvgiPaint {
        // Gradient space -> the path's user space -> document space.
        let t = transform.pre_concat(gradient.transform());
        let stop_offset = self.output.stops.len() as u32;
        for stop in gradient.stops() {
            let c = stop.color();
            self.output.stops.push(SvgiStop {
                offset: stop.offset().get(),
                color: [
                    c.red as f32 / 255.0,
                    c.green as f32 / 255.0,
                    c.blue as f32 / 255.0,
                    stop.opacity().get(),
                ],
            });
        }
        SvgiPaint {
            kind,
            color: [0.0; 4],
            transform: [t.sx, t.ky, t.kx, t.sy, t.tx, t.ty],
            geometry,
            spread: match gradient.spread_method() {
                usvg::SpreadMethod::Pad => SVGI_SPREAD_PAD,
                usvg::SpreadMethod::Reflect => SVGI_SPREAD_REFLECT,
                usvg::SpreadMethod::Repeat => SVGI_SPREAD_REPEAT,
            },
            stop_offset,
            stop_count: gradient.stops().len() as u32,
        }
    }
}

/// `conicalGradient` is an SVG Importer extension that usvg doesn't know. Its elements are renamed
/// to `radialGradient` for usvg, which then resolves their attributes, stops and references as for
/// any gradient; the returned ids say which gradients were conical. None if the document has none.
fn conical_gradients(data: &[u8]) -> Option<(Vec<u8>, HashSet<String>)> {
    let text = std::str::from_utf8(data).ok()?;
    if !text.contains("conicalGradient") {
        return None;
    }
    let options = roxmltree::ParsingOptions { allow_dtd: true, ..roxmltree::ParsingOptions::default() };
    let document = roxmltree::Document::parse_with_options(text, options).ok()?;
    let ids: HashSet<String> = document
        .descendants()
        .filter(|node| node.tag_name().name() == "conicalGradient")
        .filter_map(|node| node.attribute("id").map(str::to_string))
        .collect();
    let renamed = text.replace("<conicalGradient", "<radialGradient").replace("</conicalGradient", "</radialGradient");
    Some((renamed.into_bytes(), ids))
}

/// The outline of a triangle mesh: the edges used by exactly one triangle, chained into closed
/// loops. Triangles are first oriented to a positive signed area, so outer boundaries come out
/// with a positive area and holes with a negative one.
fn boundary_loops(positions: &[[f32; 2]], indices: &[u32]) -> Vec<Vec<[f32; 2]>> {
    // Weld vertices at identical positions, so duplicates don't hide shared edges.
    let mut welded: HashMap<(u32, u32), u32> = HashMap::new();
    let mut points: Vec<[f32; 2]> = Vec::new();
    let ids: Vec<u32> = positions
        .iter()
        .map(|p| {
            *welded.entry((p[0].to_bits(), p[1].to_bits())).or_insert_with(|| {
                points.push(*p);
                (points.len() - 1) as u32
            })
        })
        .collect();

    // Count directed edges; an edge cancels against its reverse, which belongs to the
    // neighbouring triangle.
    let mut edges: HashMap<(u32, u32), i32> = HashMap::new();
    for t in indices.chunks_exact(3) {
        let (a, mut b, mut c) = (ids[t[0] as usize], ids[t[1] as usize], ids[t[2] as usize]);
        if a == b || b == c || c == a {
            continue;
        }
        let (pa, pb, pc) = (points[a as usize], points[b as usize], points[c as usize]);
        let area = (pb[0] - pa[0]) * (pc[1] - pa[1]) - (pb[1] - pa[1]) * (pc[0] - pa[0]);
        if area == 0.0 {
            continue;
        }
        if area < 0.0 {
            std::mem::swap(&mut b, &mut c);
        }
        for (u, v) in [(a, b), (b, c), (c, a)] {
            match edges.get_mut(&(v, u)) {
                Some(count) if *count > 0 => *count -= 1,
                _ => *edges.entry((u, v)).or_insert(0) += 1,
            }
        }
    }

    let mut outgoing: HashMap<u32, Vec<u32>> = HashMap::new();
    let mut boundary: Vec<(u32, u32)> = Vec::new();
    for (&(u, v), &count) in &edges {
        for _ in 0..count.max(0) {
            boundary.push((u, v));
        }
    }
    // Deterministic output regardless of hash order.
    boundary.sort_unstable();
    for &(u, v) in &boundary {
        outgoing.entry(u).or_default().push(v);
    }

    let mut loops = Vec::new();
    for &(start, _) in &boundary {
        let Some(first) = outgoing.get_mut(&start).and_then(|o| o.pop()) else { continue };
        let mut contour = vec![points[start as usize]];
        let mut current = first;
        let mut closed = false;
        for _ in 0..boundary.len() {
            if current == start {
                closed = true;
                break;
            }
            contour.push(points[current as usize]);
            match outgoing.get_mut(&current).and_then(|o| o.pop()) {
                Some(next) => current = next,
                None => break,
            }
        }
        if closed && contour.len() >= 3 {
            loops.push(contour);
        }
    }
    loops
}

/// The path's outline as closed polygons in document space, flattened to `tolerance` document
/// units. Every subpath counts as closed, as for filling.
fn flatten(path: &tiny_skia_path::Path, transform: &Transform, tolerance: f32) -> Region {
    let scale = transform_scale(transform);
    if !(scale > 0.0) {
        return Vec::new();
    }
    let mut contours = Vec::new();
    let mut contour: Vec<[f64; 2]> = Vec::new();
    for event in to_lyon(path).iter().flattened(tolerance / scale) {
        match event {
            PathEvent::Begin { at } => {
                contour.clear();
                let (x, y) = apply(transform, at.x, at.y);
                contour.push([x as f64, y as f64]);
            }
            PathEvent::Line { to, .. } => {
                let (x, y) = apply(transform, to.x, to.y);
                contour.push([x as f64, y as f64]);
            }
            PathEvent::End { .. } => {
                if contour.len() >= 3 {
                    contours.push(std::mem::take(&mut contour));
                }
            }
            _ => {}
        }
    }
    contours
}

/// An upper bound on a path's length: the length of its control polygon.
fn outline_length(path: &tiny_skia_path::Path) -> f32 {
    let points = path.points();
    points.windows(2).map(|p| ((p[1].x - p[0].x).powi(2) + (p[1].y - p[0].y).powi(2)).sqrt()).sum()
}

fn flatten_shapes(shapes: Vec<Vec<Vec<[f64; 2]>>>) -> Region {
    shapes.into_iter().flatten().collect()
}

fn intersect(a: &Region, b: &Region) -> Region {
    if a.is_empty() || b.is_empty() {
        return Vec::new();
    }
    flatten_shapes(a.overlay(b, OverlayRule::Intersect, OverlayFillRule::NonZero))
}

fn overlay_rule(rule: usvg::FillRule) -> OverlayFillRule {
    match rule {
        usvg::FillRule::NonZero => OverlayFillRule::NonZero,
        usvg::FillRule::EvenOdd => OverlayFillRule::EvenOdd,
    }
}

fn signed_area(c: &[[f64; 2]]) -> f64 {
    let mut a = 0.0;
    for i in 0..c.len() {
        let (p, q) = (c[i], c[(i + 1) % c.len()]);
        a += p[0] * q[1] - q[0] * p[1];
    }
    a * 0.5
}

fn key_of(address: usize, t: &Transform) -> usize {
    let mut h = address as u64;
    for v in [t.sx, t.kx, t.ky, t.sy, t.tx, t.ty] {
        h = h.rotate_left(7) ^ v.to_bits() as u64;
    }
    h as usize
}

fn line_cap(cap: usvg::LineCap) -> lyon_tessellation::LineCap {
    match cap {
        usvg::LineCap::Butt => lyon_tessellation::LineCap::Butt,
        usvg::LineCap::Round => lyon_tessellation::LineCap::Round,
        usvg::LineCap::Square => lyon_tessellation::LineCap::Square,
    }
}

fn apply(t: &Transform, x: f32, y: f32) -> (f32, f32) {
    (t.sx * x + t.kx * y + t.tx, t.ky * x + t.sy * y + t.ty)
}

/// How much the transform scales lengths, used to convert the tolerance into local units.
fn transform_scale(t: &Transform) -> f32 {
    let sx = (t.sx * t.sx + t.ky * t.ky).sqrt();
    let sy = (t.kx * t.kx + t.sy * t.sy).sqrt();
    sx.max(sy)
}

fn to_lyon(path: &tiny_skia_path::Path) -> LyonPath {
    let mut builder = LyonPath::builder();
    let mut open = false;
    for segment in path.segments() {
        match segment {
            PathSegment::MoveTo(p) => {
                if open {
                    builder.end(false);
                }
                builder.begin(point(p.x, p.y));
                open = true;
            }
            PathSegment::LineTo(p) => {
                builder.line_to(point(p.x, p.y));
            }
            PathSegment::QuadTo(c, p) => {
                builder.quadratic_bezier_to(point(c.x, c.y), point(p.x, p.y));
            }
            PathSegment::CubicTo(c1, c2, p) => {
                builder.cubic_bezier_to(point(c1.x, c1.y), point(c2.x, c2.y), point(p.x, p.y));
            }
            PathSegment::Close => {
                if open {
                    builder.end(true);
                    open = false;
                }
            }
        }
    }
    if open {
        builder.end(false);
    }
    builder.build()
}

#[cfg(test)]
mod tests {
    use super::*;

    fn signed_area(c: &[[f32; 2]]) -> f32 {
        let mut a = 0.0;
        for i in 0..c.len() {
            let (p, q) = (c[i], c[(i + 1) % c.len()]);
            a += p[0] * q[1] - q[0] * p[1];
        }
        a * 0.5
    }

    fn run(svg: &str) -> Output {
        import(svg.as_bytes(), &ImportOptions { tolerance: 0.25, dpi: 96.0, convert_text: false, antialiasing: false }).unwrap()
    }

    fn layer_contours(out: &Output, layer: &SvgiLayer) -> Vec<Vec<[f32; 2]>> {
        (0..layer.contour_count)
            .map(|i| {
                let c = out.contours[(layer.contour_offset + i) as usize];
                (0..c.point_count)
                    .map(|j| {
                        let k = ((c.point_offset + j) * 2) as usize;
                        [out.contour_points[k], out.contour_points[k + 1]]
                    })
                    .collect()
            })
            .collect()
    }

    #[test]
    fn square_has_one_positive_outline() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100"><rect x="10" y="10" width="50" height="50"/></svg>"#);
        assert_eq!(out.layers.len(), 1);
        let contours = layer_contours(&out, &out.layers[0]);
        assert_eq!(contours.len(), 1);
        assert!((signed_area(&contours[0]) - 2500.0).abs() < 1e-3);
    }

    #[test]
    fn hole_has_negative_outline() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <path fill-rule="evenodd" d="M0 0H90V90H0Z M30 30H60V60H30Z"/></svg>"#);
        let mut areas: Vec<f32> = layer_contours(&out, &out.layers[0]).iter().map(|c| signed_area(c)).collect();
        areas.sort_by(|a, b| a.partial_cmp(b).unwrap());
        assert_eq!(areas.len(), 2);
        assert!((areas[0] + 900.0).abs() < 1e-3, "hole: {}", areas[0]);
        assert!((areas[1] - 8100.0).abs() < 1e-3, "outer: {}", areas[1]);
    }

    #[test]
    fn mirrored_square_keeps_orientation() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <rect transform="scale(-1 1) translate(-100 0)" x="10" y="10" width="50" height="50"/></svg>"#);
        let contours = layer_contours(&out, &out.layers[0]);
        assert!(signed_area(&contours[0]) > 0.0);
    }

    #[test]
    fn triangles_have_consistent_winding() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <circle cx="50" cy="50" r="40"/><rect transform="scale(-1 1) translate(-100 0)" width="30" height="30"/>
            <path d="M10 10 L90 90 M90 10 L10 90" stroke="black" stroke-width="5"/></svg>"#);
        for layer in &out.layers {
            for t in out.indices[layer.index_offset as usize..(layer.index_offset + layer.index_count) as usize].chunks_exact(3) {
                let p = |i: u32| {
                    let k = ((layer.vertex_offset + i) * 2) as usize;
                    [out.vertices[k], out.vertices[k + 1]]
                };
                let (a, b, c) = (p(t[0]), p(t[1]), p(t[2]));
                let area = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
                assert!(area >= 0.0, "triangle with negative area {area}");
            }
        }
    }

    fn layer_area(out: &Output, layer: &SvgiLayer) -> f32 {
        let mut area = 0.0;
        for t in out.indices[layer.index_offset as usize..(layer.index_offset + layer.index_count) as usize].chunks_exact(3) {
            let p = |i: u32| {
                let k = ((layer.vertex_offset + i) * 2) as usize;
                [out.vertices[k], out.vertices[k + 1]]
            };
            let (a, b, c) = (p(t[0]), p(t[1]), p(t[2]));
            area += ((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])) * 0.5;
        }
        area
    }

    #[test]
    fn clip_path_intersects_fill() {
        // A 100x100 square clipped by a 50x50 square that overlaps it by 25x25.
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200">
            <clipPath id="c"><rect x="75" y="75" width="50" height="50"/></clipPath>
            <rect width="100" height="100" clip-path="url(#c)"/></svg>"#);
        assert_eq!(out.layers.len(), 1);
        assert!((layer_area(&out, &out.layers[0]) - 625.0).abs() < 1e-2);
        let contours = layer_contours(&out, &out.layers[0]);
        assert_eq!(contours.len(), 1);
    }

    #[test]
    fn clip_path_uses_element_transform_and_units() {
        // objectBoundingBox: the clip covers the left half of the shape, in the shape's
        // transformed coordinates.
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200">
            <clipPath id="c" clipPathUnits="objectBoundingBox"><rect width="0.5" height="1"/></clipPath>
            <g transform="translate(50 50) scale(2)"><rect width="40" height="20" clip-path="url(#c)"/></g></svg>"#);
        assert!((layer_area(&out, &out.layers[0]) - 20.0 * 20.0 * 4.0).abs() < 1e-1);
    }

    #[test]
    fn clip_path_clips_strokes_and_nests() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200">
            <clipPath id="a"><rect width="100" height="200"/></clipPath>
            <clipPath id="b"><rect width="200" height="100"/></clipPath>
            <g clip-path="url(#a)"><g clip-path="url(#b)">
              <path d="M0 50 H200" stroke="black" stroke-width="10"/>
              <rect x="150" y="150" width="10" height="10"/>
            </g></g></svg>"#);
        // The rectangle lies outside the clip; the stroke keeps its left half.
        assert_eq!(out.layers.len(), 1);
        assert_eq!(out.layers[0].kind, SVGI_LAYER_STROKE);
        assert!((layer_area(&out, &out.layers[0]) - 1000.0).abs() < 1e-1);
    }

    #[test]
    fn clip_rule_evenodd() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <clipPath id="c"><path clip-rule="evenodd" d="M0 0H90V90H0Z M30 30H60V60H30Z"/></clipPath>
            <rect width="100" height="100" clip-path="url(#c)"/></svg>"#);
        assert!((layer_area(&out, &out.layers[0]) - (8100.0 - 900.0)).abs() < 1e-1);
    }

    #[test]
    fn clip_path_on_clip_path_child() {
        // The clip path's rectangle is itself clipped to its left half.
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <clipPath id="inner"><rect width="50" height="100"/></clipPath>
            <clipPath id="c"><rect width="100" height="100" clip-path="url(#inner)"/></clipPath>
            <rect width="100" height="100" clip-path="url(#c)"/></svg>"#);
        assert!((layer_area(&out, &out.layers[0]) - 5000.0).abs() < 1e-1);
    }

    #[test]
    fn opaque_mask_clips() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <mask id="m"><rect width="50" height="100" fill="white"/></mask>
            <rect width="100" height="100" mask="url(#m)"/></svg>"#);
        assert_eq!(out.diagnostics & SVGI_DIAG_MASK, 0);
        assert!((layer_area(&out, &out.layers[0]) - 5000.0).abs() < 1e-1);
    }

    #[test]
    fn translucent_mask_is_reported() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <mask id="m"><rect width="50" height="100" fill="gray"/></mask>
            <rect width="100" height="100" mask="url(#m)"/></svg>"#);
        assert_ne!(out.diagnostics & SVGI_DIAG_MASK, 0);
        assert!((layer_area(&out, &out.layers[0]) - 10000.0).abs() < 1e-1);
    }

    #[test]
    fn oversized_input_is_refused() {
        let svg = format!("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><!--{}--></svg>", " ".repeat(MAX_INPUT_BYTES));
        let error = import(svg.as_bytes(), &ImportOptions { tolerance: 0.25, dpi: 96.0, convert_text: false, antialiasing: false }).err().expect("an error");
        assert!(error.contains("larger than"), "{error}");
    }

    #[test]
    fn dash_flood_is_refused() {
        let out = import(br#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <path d="M0 0 H1000000000" stroke="black" stroke-dasharray="0.001 0.001"/></svg>"#,
            &ImportOptions { tolerance: 0.25, dpi: 96.0, convert_text: false, antialiasing: false });
        assert!(out.err().expect("an error").contains("dash"));
    }

    #[test]
    fn tiny_tolerance_is_bounded_by_path_size() {
        // A 10-million-unit circle with a 1e-9 tolerance: the tolerance is raised to a millionth
        // of the circle, so the output stays small.
        let out = import(br#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <circle cx="0" cy="0" r="10000000"/></svg>"#,
            &ImportOptions { tolerance: 1e-9, dpi: 96.0, convert_text: false, antialiasing: false }).unwrap();
        assert!(out.vertices.len() / 2 < 100_000, "{} vertices", out.vertices.len() / 2);
    }

    #[test]
    fn fill_and_stroke_share_element() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <rect id="a" x="10" y="10" width="50" height="50" fill="red" stroke="blue" stroke-width="4"/>
            <circle id="b" cx="70" cy="70" r="10"/></svg>"#);
        assert_eq!(out.layers.len(), 3);
        assert_eq!(out.layers[0].element, out.layers[1].element);
        assert_ne!(out.layers[1].element, out.layers[2].element);
        assert_eq!(out.layers[1].kind, SVGI_LAYER_STROKE);
        // A stroked rectangle is a ring: one outer and one inner outline.
        assert_eq!(out.layers[1].contour_count, 2);
    }
    fn run_antialiased(svg: &str) -> Output {
        import(svg.as_bytes(), &ImportOptions { tolerance: 0.25, dpi: 96.0, convert_text: false, antialiasing: true }).unwrap()
    }

    fn extrusion(out: &Output, vertex: usize) -> [f32; 2] {
        [out.extrusions[vertex * 2], out.extrusions[vertex * 2 + 1]]
    }

    #[test]
    fn fringe_follows_each_layer() {
        let out = run_antialiased(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100"><rect x="10" y="10" width="50" height="50"/></svg>"#);
        assert_eq!(out.layers.len(), 2);
        let (fill, fringe) = (out.layers[0], out.layers[1]);
        assert_eq!(fringe.kind, SVGI_LAYER_FRINGE);
        assert_eq!((fringe.paint, fringe.element, fringe.contour_count), (fill.paint, fill.element, 0));
        assert_eq!(out.extrusions.len(), out.vertices.len());
        assert_eq!(out.coverage.len(), out.vertices.len() / 2);
        // Four square corners: a miter pair each, inner opaque, outer transparent.
        assert_eq!(fringe.vertex_count, 8);
        for v in fringe.vertex_offset..fringe.vertex_offset + fringe.vertex_count {
            let v = v as usize;
            let (p, e) = ([out.vertices[v * 2], out.vertices[v * 2 + 1]], extrusion(&out, v));
            let outward = [(p[0] - 35.0).signum(), (p[1] - 35.0).signum()];
            let along = e[0] * outward[0] + e[1] * outward[1];
            if out.coverage[v] == 0.0 {
                assert!((along - 2.0).abs() < 1e-5, "outer vertex {:?} extrusion {:?}", p, e);
            } else {
                assert!((along + 2.0).abs() < 1e-5, "inner vertex {:?} extrusion {:?}", p, e);
            }
        }
        // The fill's corners move inward with the fringe's inner edge.
        for v in fill.vertex_offset..fill.vertex_offset + fill.vertex_count {
            let v = v as usize;
            let (p, e) = ([out.vertices[v * 2], out.vertices[v * 2 + 1]], extrusion(&out, v));
            assert_eq!(e, [(35.0 - p[0]).signum(), (35.0 - p[1]).signum()]);
        }
    }

    #[test]
    fn fringe_bevels_sharp_corners_and_keeps_holes_outward() {
        let out = run_antialiased(r#"<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200"><path d="M85 180 L115 180 L100 8.5 Z"/></svg>"#);
        let fringe = out.layers[1];
        let max = (fringe.vertex_offset..fringe.vertex_offset + fringe.vertex_count)
            .map(|v| { let e = extrusion(&out, v as usize); (e[0] * e[0] + e[1] * e[1]).sqrt() })
            .fold(0.0f32, f32::max);
        assert!(max <= fringe::MITER_LIMIT + 1e-5, "extrusion {}", max);
        // The tip is bevelled: more than the six vertices of three mitered corners.
        assert!(fringe.vertex_count > 6);

        // A square with a square hole: the hole's fringe points into the hole.
        let out = run_antialiased(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100"><path d="M10 10 h80 v80 h-80 Z M40 40 v20 h20 v-20 Z"/></svg>"#);
        let fringe = out.layers[1];
        for v in fringe.vertex_offset..fringe.vertex_offset + fringe.vertex_count {
            let v = v as usize;
            let p = [out.vertices[v * 2], out.vertices[v * 2 + 1]];
            if p[0] < 30.0 || p[0] > 70.0 || out.coverage[v] != 0.0 {
                continue;
            }
            let e = extrusion(&out, v);
            let toward_centre = (50.0 - p[0]) * e[0] + (50.0 - p[1]) * e[1];
            assert!(toward_centre > 0.0, "hole vertex {:?} extrusion {:?}", p, e);
        }
    }

    #[test]
    fn conical_gradient_extension() {
        let out = run(r#"<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
            <conicalGradient id="c" cx="50" cy="50" r="40" gradientUnits="userSpaceOnUse">
                <stop offset="0" stop-color="red"/><stop offset="1" stop-color="blue"/></conicalGradient>
            <radialGradient id="r"><stop offset="0" stop-color="red"/><stop offset="1" stop-color="blue"/></radialGradient>
            <rect width="50" height="100" fill="url(#c)"/><rect x="50" width="50" height="100" fill="url(#r)"/></svg>"#);
        let kinds: Vec<u8> = out.layers.iter().map(|l| out.paints[l.paint as usize].kind).collect();
        assert_eq!(kinds, vec![SVGI_PAINT_CONICAL, SVGI_PAINT_RADIAL]);
        let conical = out.paints[out.layers[0].paint as usize];
        assert_eq!(&conical.geometry[..3], &[50.0, 50.0, 40.0]);
        assert_eq!(conical.stop_count, 2);
    }
}
