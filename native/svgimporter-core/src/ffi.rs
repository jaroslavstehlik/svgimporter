//! C ABI of the core. The C# bindings in the Unity package are generated from this file by
//! csbindgen (see build.rs), so every type here must stay `#[repr(C)]` and blittable.

use std::panic::{catch_unwind, AssertUnwindSafe};

use crate::import::{self, ImportOptions, Output};

/// Increment whenever the ABI changes. C# checks it before using the library.
pub const SVGI_ABI_VERSION: u32 = 4;

pub const SVGI_OK: i32 = 0;
/// The SVG could not be parsed; see `SvgiResult::error_message`.
pub const SVGI_ERROR_PARSE: i32 = 1;
/// A null pointer or an invalid option was passed.
pub const SVGI_ERROR_ARGUMENT: i32 = 2;
/// The core panicked. The import failed, but the process is unaffected.
pub const SVGI_ERROR_INTERNAL: i32 = 3;

pub const SVGI_LAYER_FILL: u8 = 0;
pub const SVGI_LAYER_STROKE: u8 = 1;
/// The antialiasing fringe of the layer before it (see `SvgiOptions::antialiasing`).
pub const SVGI_LAYER_FRINGE: u8 = 2;

pub const SVGI_PAINT_SOLID: u8 = 0;
pub const SVGI_PAINT_LINEAR: u8 = 1;
pub const SVGI_PAINT_RADIAL: u8 = 2;
/// SVG Importer's `conicalGradient` extension: the colour goes round the centre, starting on the
/// positive x axis. Geometry as radial.
pub const SVGI_PAINT_CONICAL: u8 = 3;

pub const SVGI_SPREAD_PAD: u8 = 0;
pub const SVGI_SPREAD_REFLECT: u8 = 1;
pub const SVGI_SPREAD_REPEAT: u8 = 2;

/// Flags in `SvgiResult::diagnostics`: content that was skipped or approximated.
/// (Plain numbers rather than shifts, so csbindgen can export them to C#.)
pub const SVGI_DIAG_MASK: u32 = 1;
pub const SVGI_DIAG_FILTER: u32 = 2;
pub const SVGI_DIAG_CLIP_PATH: u32 = 4;
pub const SVGI_DIAG_IMAGE: u32 = 8;
pub const SVGI_DIAG_PATTERN: u32 = 16;
pub const SVGI_DIAG_BLEND_MODE: u32 = 32;
pub const SVGI_DIAG_TEXT_SKIPPED: u32 = 64;
pub const SVGI_DIAG_GROUP_OPACITY: u32 = 128;

#[repr(C)]
#[derive(Clone, Copy)]
pub struct SvgiOptions {
    /// Maximum distance, in SVG user units, between a curve and its flattened line segments.
    pub tolerance: f32,
    /// DPI used to resolve physical units such as `mm` and `in`. Browsers use 96.
    pub dpi: f32,
    /// 1 to convert text to paths with the system fonts, 0 to skip text.
    pub convert_text: u8,
    /// 1 to add an antialiasing fringe after every fill and stroke layer (DESIGN.md, 7.2) and
    /// fill `SvgiResult::extrusions` and `coverage`.
    pub antialiasing: u8,
}

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct SvgiLayer {
    /// Range in `vertices` (pairs of floats: x, y in SVG user units, y pointing down).
    pub vertex_offset: u32,
    pub vertex_count: u32,
    /// Range in `indices`; three per triangle, relative to `vertex_offset`.
    pub index_offset: u32,
    pub index_count: u32,
    /// Index into `paints`.
    pub paint: u32,
    /// `SVGI_LAYER_FILL`, `SVGI_LAYER_STROKE` or `SVGI_LAYER_FRINGE`.
    pub kind: u8,
    /// Product of the fill or stroke opacity and the opacities of all enclosing groups.
    pub opacity: f32,
    /// The element's `id`, UTF-8, as a range in `strings`. Empty if the element has none.
    pub name_offset: u32,
    pub name_length: u32,
    /// Index of the source element, counted in painting order. A path's fill and stroke share it.
    pub element: u32,
    /// The layer's outline (outer boundaries and holes), as a range in `contours`. Fringes have none.
    pub contour_offset: u32,
    pub contour_count: u32,
}

/// A closed outline: a range in `contour_points` (pairs of floats, the same coordinates as
/// `vertices`). Outer boundaries have a positive signed area (shoelace formula on these
/// coordinates) and holes a negative one; with y pointing down, the shape lies to the right of
/// the direction of travel.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct SvgiContour {
    pub point_offset: u32,
    pub point_count: u32,
}

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct SvgiPaint {
    /// `SVGI_PAINT_SOLID`, `SVGI_PAINT_LINEAR`, `SVGI_PAINT_RADIAL` or `SVGI_PAINT_CONICAL`.
    pub kind: u8,
    /// Solid colour, RGBA in 0..1 (sRGB). Opacity is in the layer, not here.
    pub color: [f32; 4],
    /// Gradients: transform from gradient space to SVG user space, as a b c d e f
    /// (x' = a*x + c*y + e, y' = b*x + d*y + f).
    pub transform: [f32; 6],
    /// Linear: x1 y1 x2 y2. Radial and conical: cx cy r fx fy fr. In gradient space.
    pub geometry: [f32; 6],
    /// `SVGI_SPREAD_PAD`, `SVGI_SPREAD_REFLECT` or `SVGI_SPREAD_REPEAT`.
    pub spread: u8,
    /// Range in `stops`.
    pub stop_offset: u32,
    pub stop_count: u32,
}

#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct SvgiStop {
    pub offset: f32,
    /// RGBA in 0..1 (sRGB), with the stop opacity in alpha.
    pub color: [f32; 4],
}

/// The import result. All arrays belong to the result and are freed by `svgi_free`.
#[repr(C)]
pub struct SvgiResult {
    /// Width and height of the document in user units (from `width`/`height` or the viewBox).
    pub width: f32,
    pub height: f32,
    pub layers: *const SvgiLayer,
    pub layer_count: u32,
    pub vertices: *const f32,
    /// Number of floats in `vertices` (two per vertex).
    pub vertex_float_count: u32,
    pub indices: *const u32,
    pub index_count: u32,
    pub paints: *const SvgiPaint,
    pub paint_count: u32,
    pub stops: *const SvgiStop,
    pub stop_count: u32,
    pub strings: *const u8,
    pub string_length: u32,
    pub contours: *const SvgiContour,
    pub contour_count: u32,
    pub contour_points: *const f32,
    /// Number of floats in `contour_points` (two per point).
    pub contour_point_float_count: u32,
    /// With antialiasing: per vertex, the vector the vertex shader moves it along (pairs of floats,
    /// in the coordinates of `vertices`; zero for vertices inside a layer), and its coverage (0 on
    /// the outer edge of a fringe, 1 elsewhere). Empty without antialiasing.
    pub extrusions: *const f32,
    pub extrusion_float_count: u32,
    pub coverage: *const f32,
    pub coverage_count: u32,
    /// `SVGI_DIAG_*` flags.
    pub diagnostics: u32,
    /// UTF-8 message for `SVGI_ERROR_PARSE` and `SVGI_ERROR_INTERNAL`; empty otherwise.
    pub error_message: *const u8,
    pub error_message_length: u32,
}

/// Owns the arrays a `SvgiResult` points into. The header must stay the first field, so that a
/// pointer to it is also a pointer to this struct.
#[repr(C)]
struct OwnedResult {
    header: SvgiResult,
    output: Output,
    error_message: String,
}

fn into_raw(output: Output, error_message: String) -> *mut SvgiResult {
    let mut owned = Box::new(OwnedResult {
        header: SvgiResult {
            width: output.width,
            height: output.height,
            layers: std::ptr::null(),
            layer_count: 0,
            vertices: std::ptr::null(),
            vertex_float_count: 0,
            indices: std::ptr::null(),
            index_count: 0,
            paints: std::ptr::null(),
            paint_count: 0,
            stops: std::ptr::null(),
            stop_count: 0,
            strings: std::ptr::null(),
            string_length: 0,
            contours: std::ptr::null(),
            contour_count: 0,
            contour_points: std::ptr::null(),
            contour_point_float_count: 0,
            extrusions: std::ptr::null(),
            extrusion_float_count: 0,
            coverage: std::ptr::null(),
            coverage_count: 0,
            diagnostics: output.diagnostics,
            error_message: std::ptr::null(),
            error_message_length: 0,
        },
        output,
        error_message,
    });
    // The vectors don't move when the box is created, so their pointers stay valid.
    let o = &owned.output;
    let h = SvgiResult {
        width: o.width,
        height: o.height,
        layers: o.layers.as_ptr(),
        layer_count: o.layers.len() as u32,
        vertices: o.vertices.as_ptr(),
        vertex_float_count: o.vertices.len() as u32,
        indices: o.indices.as_ptr(),
        index_count: o.indices.len() as u32,
        paints: o.paints.as_ptr(),
        paint_count: o.paints.len() as u32,
        stops: o.stops.as_ptr(),
        stop_count: o.stops.len() as u32,
        strings: o.strings.as_ptr(),
        string_length: o.strings.len() as u32,
        contours: o.contours.as_ptr(),
        contour_count: o.contours.len() as u32,
        contour_points: o.contour_points.as_ptr(),
        contour_point_float_count: o.contour_points.len() as u32,
        extrusions: o.extrusions.as_ptr(),
        extrusion_float_count: o.extrusions.len() as u32,
        coverage: o.coverage.as_ptr(),
        coverage_count: o.coverage.len() as u32,
        diagnostics: o.diagnostics,
        error_message: owned.error_message.as_ptr(),
        error_message_length: owned.error_message.len() as u32,
    };
    owned.header = h;
    Box::into_raw(owned) as *mut SvgiResult
}

/// ABI version of this library (`SVGI_ABI_VERSION`).
#[no_mangle]
pub extern "C" fn svgi_version() -> u32 {
    SVGI_ABI_VERSION
}

/// Imports an SVG document. On `SVGI_OK` and `SVGI_ERROR_PARSE`, `*result` receives a result
/// that must be released with `svgi_free`. On other errors, `*result` is set to null.
///
/// # Safety
/// `svg` must point to `length` readable bytes; `options` and `result` must be valid pointers.
#[no_mangle]
pub unsafe extern "C" fn svgi_import(
    svg: *const u8,
    length: usize,
    options: *const SvgiOptions,
    result: *mut *mut SvgiResult,
) -> i32 {
    if result.is_null() {
        return SVGI_ERROR_ARGUMENT;
    }
    *result = std::ptr::null_mut();
    if options.is_null() || (svg.is_null() && length > 0) {
        return SVGI_ERROR_ARGUMENT;
    }
    // Callers may pass null for empty input (C# does for an empty array).
    let data: &[u8] = if length == 0 { &[] } else { std::slice::from_raw_parts(svg, length) };
    let options = *options;
    if !(options.tolerance > 0.0) || !(options.dpi > 0.0) {
        return SVGI_ERROR_ARGUMENT;
    }

    let import_options = ImportOptions {
        tolerance: options.tolerance,
        dpi: options.dpi,
        convert_text: options.convert_text != 0,
        antialiasing: options.antialiasing != 0,
    };
    match catch_unwind(AssertUnwindSafe(|| import::import(data, &import_options))) {
        Ok(Ok(output)) => {
            *result = into_raw(output, String::new());
            SVGI_OK
        }
        Ok(Err(message)) => {
            *result = into_raw(Output::default(), message);
            SVGI_ERROR_PARSE
        }
        Err(panic) => {
            let message = panic
                .downcast_ref::<&str>()
                .map(|s| s.to_string())
                .or_else(|| panic.downcast_ref::<String>().cloned())
                .unwrap_or_else(|| "internal error".to_string());
            *result = into_raw(Output::default(), message);
            SVGI_ERROR_INTERNAL
        }
    }
}

/// Releases a result returned by `svgi_import`. Null is ignored.
///
/// # Safety
/// `result` must be null or a pointer returned by `svgi_import` that hasn't been freed.
#[no_mangle]
pub unsafe extern "C" fn svgi_free(result: *mut SvgiResult) {
    if !result.is_null() {
        drop(Box::from_raw(result as *mut OwnedResult));
    }
}
