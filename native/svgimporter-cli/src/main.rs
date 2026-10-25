//! Internal tool: imports SVG files with svgimporter-core outside Unity.
//!
//!   svgimporter-cli check <file or folder>...   per-file layers, triangles, diagnostics and time
//!   svgimporter-cli bench <file or folder>...   totals over many files: failures, panics, time
//!   svgimporter-cli compare <folder> [failures.txt]
//!       rasterizes the output of every SVG with tiny-skia and compares it with the PNG next to it
//!       (the resvg test suite layout); prints the pass rate per folder
//!
//! It calls the same C ABI that Unity uses.

use std::collections::BTreeMap;
use std::path::{Path, PathBuf};
use std::time::Instant;

use svgimporter_core::ffi::*;

struct Imported {
    code: i32,
    /// Layers for which no closed outline could be extracted.
    layers_without_outline: u32,
    layers: u32,
    triangles: u32,
    vertices: u32,
    diagnostics: u32,
    error: String,
}

fn import(data: &[u8], tolerance: f32) -> Imported {
    let options = SvgiOptions { tolerance, dpi: 96.0, convert_text: 1, antialiasing: 0 };
    let mut result: *mut SvgiResult = std::ptr::null_mut();
    let code = unsafe { svgi_import(data.as_ptr(), data.len(), &options, &mut result) };
    let mut imported = Imported { code, layers_without_outline: 0, layers: 0, triangles: 0, vertices: 0, diagnostics: 0, error: String::new() };
    if !result.is_null() {
        let r = unsafe { &*result };
        imported.layers = r.layer_count;
        imported.triangles = r.index_count / 3;
        imported.vertices = r.vertex_float_count / 2;
        imported.diagnostics = r.diagnostics;
        for i in 0..r.layer_count as usize {
            if unsafe { (*r.layers.add(i)).contour_count } == 0 {
                imported.layers_without_outline += 1;
            }
        }
        if r.error_message_length > 0 {
            let bytes = unsafe { std::slice::from_raw_parts(r.error_message, r.error_message_length as usize) };
            imported.error = String::from_utf8_lossy(bytes).into_owned();
        }
        unsafe { svgi_free(result) };
    }
    imported
}

/// Rasterizes the triangles of every layer, as Unity would draw them, with tiny-skia, scaled to
/// `width` x `height` pixels. Each layer is one path of consistently wound triangles, so shared
/// edges inside it don't show seams. `tolerance` is in pixels.
fn render(data: &[u8], tolerance: f32, width: u32, height: u32) -> Option<tiny_skia::Pixmap> {
    use tiny_skia::Transform;

    // The document size is only known after importing; import once to get it.
    let mut size = (1.0f32, 1.0f32);
    for pass in 0..2 {
        let scale = (width as f32 / size.0).min(height as f32 / size.1).max(1e-6);
        let options = SvgiOptions { tolerance: tolerance / if pass == 0 { 1.0 } else { scale }, dpi: 96.0, convert_text: 1, antialiasing: 0 };
        let mut result: *mut SvgiResult = std::ptr::null_mut();
        let code = unsafe { svgi_import(data.as_ptr(), data.len(), &options, &mut result) };
        if result.is_null() {
            return None;
        }
        if pass == 0 {
            let r = unsafe { &*result };
            size = (r.width.max(1e-6), r.height.max(1e-6));
            unsafe { svgi_free(result) };
            if code != SVGI_OK {
                return None;
            }
            continue;
        }
        let view = Transform::from_scale(width as f32 / size.0, height as f32 / size.1);
        let pixmap = draw(unsafe { &*result }, width, height, view);
        unsafe { svgi_free(result) };
        return pixmap;
    }
    None
}

fn draw(r: &SvgiResult, width: u32, height: u32, view: tiny_skia::Transform) -> Option<tiny_skia::Pixmap> {
    use tiny_skia::{Color, FillRule, GradientStop, LinearGradient, Paint, PathBuilder, Pixmap, Point, RadialGradient, Shader, SpreadMode, Transform};

    fn slice<'a, T>(ptr: *const T, len: u32) -> &'a [T] {
        if len == 0 { &[] } else { unsafe { std::slice::from_raw_parts(ptr, len as usize) } }
    }
    let mut pixmap = Pixmap::new(width, height);
    if let Some(pixmap) = pixmap.as_mut() {
        let vertices: &[f32] = slice(r.vertices, r.vertex_float_count);
        let indices: &[u32] = slice(r.indices, r.index_count);
        let layers: &[SvgiLayer] = slice(r.layers, r.layer_count);
        let paints: &[SvgiPaint] = slice(r.paints, r.paint_count);
        let stops: &[SvgiStop] = slice(r.stops, r.stop_count);
        for layer in layers {
            let mut builder = PathBuilder::new();
            for t in indices[layer.index_offset as usize..(layer.index_offset + layer.index_count) as usize].chunks_exact(3) {
                let p = |i: u32| {
                    let k = ((layer.vertex_offset + i) * 2) as usize;
                    (vertices[k], vertices[k + 1])
                };
                let (a, b, c) = (p(t[0]), p(t[1]), p(t[2]));
                builder.move_to(a.0, a.1);
                builder.line_to(b.0, b.1);
                builder.line_to(c.0, c.1);
                builder.close();
            }
            let Some(path) = builder.finish() else { continue };
            let p = &paints[layer.paint as usize];
            let color = |c: [f32; 4], alpha: f32| Color::from_rgba(c[0], c[1], c[2], (c[3] * alpha).clamp(0.0, 1.0)).unwrap_or(Color::TRANSPARENT);
            let shader = if p.kind == SVGI_PAINT_SOLID {
                Shader::SolidColor(color(p.color, layer.opacity))
            } else {
                let gradient_stops: Vec<GradientStop> = stops[p.stop_offset as usize..(p.stop_offset + p.stop_count) as usize]
                    .iter()
                    .map(|s| GradientStop::new(s.offset, color(s.color, layer.opacity)))
                    .collect();
                let mode = match p.spread {
                    SVGI_SPREAD_REFLECT => SpreadMode::Reflect,
                    SVGI_SPREAD_REPEAT => SpreadMode::Repeat,
                    _ => SpreadMode::Pad,
                };
                let t = p.transform;
                // fill_path applies the view transform to the shader too.
                let transform = Transform::from_row(t[0], t[1], t[2], t[3], t[4], t[5]);
                let g = p.geometry;
                let shader = if p.kind == SVGI_PAINT_LINEAR {
                    LinearGradient::new(Point::from_xy(g[0], g[1]), Point::from_xy(g[2], g[3]), gradient_stops.clone(), mode, transform)
                } else {
                    RadialGradient::new(Point::from_xy(g[3], g[4]), g[5], Point::from_xy(g[0], g[1]), g[2], gradient_stops.clone(), mode, transform)
                };
                // Degenerate gradients paint the last stop's colour, as in SVG.
                let last = stops[(p.stop_offset + p.stop_count).max(1) as usize - 1];
                shader.unwrap_or_else(|| Shader::SolidColor(if p.stop_count == 0 { Color::TRANSPARENT } else { color(last.color, layer.opacity) }))
            };
            let paint = Paint { shader, anti_alias: true, ..Paint::default() };
            pixmap.fill_path(&path, &paint, FillRule::Winding, view, None);
        }
    }
    pixmap
}

/// Fraction of pixels whose premultiplied RGBA differs by more than `threshold` in any channel.
fn difference(a: &tiny_skia::Pixmap, b: &tiny_skia::Pixmap, threshold: u8) -> f64 {
    if a.width() != b.width() || a.height() != b.height() {
        return 1.0;
    }
    let differing = a
        .data()
        .chunks_exact(4)
        .zip(b.data().chunks_exact(4))
        .filter(|(p, q)| p.iter().zip(q.iter()).any(|(x, y)| x.abs_diff(*y) > threshold))
        .count();
    differing as f64 / (a.width() * a.height()) as f64
}

fn diagnostics_names(flags: u32) -> String {
    let names = [
        (SVGI_DIAG_MASK, "mask"),
        (SVGI_DIAG_FILTER, "filter"),
        (SVGI_DIAG_CLIP_PATH, "clip-path"),
        (SVGI_DIAG_IMAGE, "image"),
        (SVGI_DIAG_PATTERN, "pattern"),
        (SVGI_DIAG_BLEND_MODE, "blend-mode"),
        (SVGI_DIAG_TEXT_SKIPPED, "text-skipped"),
        (SVGI_DIAG_GROUP_OPACITY, "group-opacity"),
    ];
    names.iter().filter(|(f, _)| flags & f != 0).map(|(_, n)| *n).collect::<Vec<_>>().join(",")
}

fn collect(paths: &[String]) -> Vec<PathBuf> {
    let mut files = Vec::new();
    fn walk(path: &Path, files: &mut Vec<PathBuf>) {
        if path.is_dir() {
            let mut entries: Vec<_> = std::fs::read_dir(path).into_iter().flatten().flatten().map(|e| e.path()).collect();
            entries.sort();
            for entry in entries {
                walk(&entry, files);
            }
        } else if path.extension().map_or(false, |e| e.eq_ignore_ascii_case("svg")) {
            files.push(path.to_path_buf());
        }
    }
    for p in paths {
        walk(Path::new(p), &mut files);
    }
    files
}

fn main() {
    let args: Vec<String> = std::env::args().skip(1).collect();
    let tolerance = 0.25;
    match args.first().map(String::as_str) {
        Some("check") => {
            for file in collect(&args[1..]) {
                let data = std::fs::read(&file).unwrap_or_default();
                let start = Instant::now();
                let r = import(&data, tolerance);
                let ms = start.elapsed().as_secs_f64() * 1000.0;
                let status = match r.code {
                    SVGI_OK => "ok".to_string(),
                    SVGI_ERROR_PARSE => format!("parse error: {}", r.error),
                    SVGI_ERROR_INTERNAL => format!("INTERNAL ERROR: {}", r.error),
                    c => format!("error {c}"),
                };
                println!(
                    "{}  {}  layers {}  triangles {}  vertices {}  {:.2} ms  [{}]",
                    file.display(), status, r.layers, r.triangles, r.vertices, ms, diagnostics_names(r.diagnostics)
                );
            }
        }
        Some("bench") => {
            let files = collect(&args[1..]);
            let data: Vec<Vec<u8>> = files.iter().map(|f| std::fs::read(f).unwrap_or_default()).collect();
            let (mut ok, mut parse_errors, mut internal, mut triangles, mut no_outline) = (0, 0, 0, 0u64, 0u32);
            let mut internal_files = Vec::new();
            let start = Instant::now();
            for (i, d) in data.iter().enumerate() {
                let r = import(d, tolerance);
                match r.code {
                    SVGI_OK => ok += 1,
                    SVGI_ERROR_PARSE => parse_errors += 1,
                    _ => {
                        internal += 1;
                        internal_files.push(format!("{}: {}", files[i].display(), r.error));
                    }
                }
                triangles += r.triangles as u64;
                no_outline += r.layers_without_outline;
            }
            let seconds = start.elapsed().as_secs_f64();
            println!(
                "files {}  ok {}  parse errors {}  internal errors {}  triangles {}  layers without outline {}  total {:.1} ms  per file {:.3} ms",
                files.len(), ok, parse_errors, internal, triangles, no_outline, seconds * 1000.0, seconds * 1000.0 / files.len().max(1) as f64
            );
            for f in internal_files {
                println!("internal error: {f}");
            }
        }
        Some("compare") => {
            // A file passes when at most 1% of its pixels differ noticeably from the reference.
            // Edges differ slightly: curves are flattened, and resvg draws them exactly.
            let root = PathBuf::from(args.get(1).expect("usage: svgimporter-cli compare <folder> [failures.txt]"));
            let mut categories: BTreeMap<String, (u32, u32)> = BTreeMap::new();
            let mut failures = Vec::new();
            for file in collect(&args[1..2]) {
                let reference = file.with_extension("png");
                let Ok(expected) = tiny_skia::Pixmap::load_png(&reference) else { continue };
                let category = file.parent().and_then(|p| p.strip_prefix(&root).ok()).map_or(String::new(), |p| p.display().to_string());
                let data = std::fs::read(&file).unwrap_or_default();
                let passed = match render(&data, tolerance, expected.width(), expected.height()) {
                    Some(actual) => {
                        // Set SVGI_COMPARE_OUTPUT to a folder to keep the renders.
                        if let Ok(dir) = std::env::var("SVGI_COMPARE_OUTPUT") {
                            let name = file.strip_prefix(&root).unwrap_or(&file).display().to_string().replace('/', "_");
                            let _ = actual.save_png(Path::new(&dir).join(name).with_extension("png"));
                        }
                        let d = difference(&actual, &expected, 32);
                        if d > 0.01 {
                            failures.push(format!("{}  {:.1}%", file.display(), d * 100.0));
                        }
                        d <= 0.01
                    }
                    None => {
                        failures.push(format!("{}  not imported", file.display()));
                        false
                    }
                };
                let entry = categories.entry(category).or_default();
                entry.0 += 1;
                entry.1 += passed as u32;
            }
            let (total, passed) = categories.values().fold((0, 0), |(t, p), (ct, cp)| (t + ct, p + cp));
            for (category, (count, ok)) in &categories {
                println!("{:<45} {:>4} / {:<4} {:>5.1}%", category, ok, count, *ok as f64 * 100.0 / *count as f64);
            }
            println!("{:<45} {:>4} / {:<4} {:>5.1}%", "total", passed, total, passed as f64 * 100.0 / total.max(1) as f64);
            if let Some(out) = args.get(2) {
                std::fs::write(out, failures.join("\n")).expect("can't write the failure list");
            }
        }
        _ => {
            eprintln!("usage: svgimporter-cli check|bench|compare <file or folder>...");
            std::process::exit(2);
        }
    }
}
