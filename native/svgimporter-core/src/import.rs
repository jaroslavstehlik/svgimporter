//! Parses an SVG with usvg and tessellates every fill and stroke with lyon.
//!
//! Each fill or stroke becomes one layer, in painting order. Vertices are in SVG user units with
//! all transforms applied (y pointing down). Unity-specific processing (scaling, pivot, render
//! formats, gradients in the mesh) happens in C#.

use std::collections::HashMap;
use std::sync::{Arc, OnceLock};

use lyon_tessellation::math::point;
use lyon_tessellation::path::Path as LyonPath;
use lyon_tessellation::{
    BuffersBuilder, FillOptions, FillTessellator, FillVertex, StrokeOptions, StrokeTessellator,
    StrokeVertex, VertexBuffers,
};
use tiny_skia_path::{PathSegment, Transform};

use crate::ffi::*;

pub struct ImportOptions {
    pub tolerance: f32,
    pub dpi: f32,
    pub convert_text: bool,
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

pub fn import(data: &[u8], options: &ImportOptions) -> Result<Output, String> {
    let mut usvg_options = usvg::Options {
        dpi: options.dpi,
        ..usvg::Options::default()
    };
    if options.convert_text {
        usvg_options.fontdb = system_fonts();
    }
    let tree = usvg::Tree::from_data(data, &usvg_options).map_err(|e| e.to_string())?;

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
    };
    importer.group(tree.root(), 1.0);
    Ok(importer.output)
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
}

impl Importer<'_> {
    fn group(&mut self, group: &usvg::Group, opacity: f32) {
        if group.clip_path().is_some() {
            self.output.diagnostics |= SVGI_DIAG_CLIP_PATH;
        }
        if group.mask().is_some() {
            self.output.diagnostics |= SVGI_DIAG_MASK;
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
        // non-uniform transforms; the tolerance is converted into those coordinates.
        let tolerance = self.options.tolerance / scale;

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
            .with_miter_limit(stroke.miterlimit().get().max(StrokeOptions::MINIMUM_MITER_LIMIT))
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
        if tessellated.is_ok() {
            self.push_layer(path.id(), SVGI_LAYER_STROKE, paint, opacity * stroke.opacity().get(), &buffers, transform);
        }
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
                let paint = self.gradient(
                    SVGI_PAINT_RADIAL,
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
        import(svg.as_bytes(), &ImportOptions { tolerance: 0.25, dpi: 96.0, convert_text: false }).unwrap()
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
}
