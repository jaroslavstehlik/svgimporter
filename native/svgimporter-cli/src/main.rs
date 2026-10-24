//! Internal tool: imports SVG files with svgimporter-core outside Unity.
//!
//!   svgimporter-cli check <file or folder>...   per-file layers, triangles, diagnostics and time
//!   svgimporter-cli bench <file or folder>...   totals over many files: failures, panics, time
//!
//! It calls the same C ABI that Unity uses.

use std::path::{Path, PathBuf};
use std::time::Instant;

use svgimporter_core::ffi::*;

struct Imported {
    code: i32,
    layers: u32,
    triangles: u32,
    vertices: u32,
    diagnostics: u32,
    error: String,
}

fn import(data: &[u8], tolerance: f32) -> Imported {
    let options = SvgiOptions { tolerance, dpi: 96.0, convert_text: 1 };
    let mut result: *mut SvgiResult = std::ptr::null_mut();
    let code = unsafe { svgi_import(data.as_ptr(), data.len(), &options, &mut result) };
    let mut imported = Imported { code, layers: 0, triangles: 0, vertices: 0, diagnostics: 0, error: String::new() };
    if !result.is_null() {
        let r = unsafe { &*result };
        imported.layers = r.layer_count;
        imported.triangles = r.index_count / 3;
        imported.vertices = r.vertex_float_count / 2;
        imported.diagnostics = r.diagnostics;
        if r.error_message_length > 0 {
            let bytes = unsafe { std::slice::from_raw_parts(r.error_message, r.error_message_length as usize) };
            imported.error = String::from_utf8_lossy(bytes).into_owned();
        }
        unsafe { svgi_free(result) };
    }
    imported
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
            let (mut ok, mut parse_errors, mut internal, mut triangles) = (0, 0, 0, 0u64);
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
            }
            let seconds = start.elapsed().as_secs_f64();
            println!(
                "files {}  ok {}  parse errors {}  internal errors {}  triangles {}  total {:.1} ms  per file {:.3} ms",
                files.len(), ok, parse_errors, internal, triangles, seconds * 1000.0, seconds * 1000.0 / files.len().max(1) as f64
            );
            for f in internal_files {
                println!("internal error: {f}");
            }
        }
        _ => {
            eprintln!("usage: svgimporter-cli check|bench <file or folder>...");
            std::process::exit(2);
        }
    }
}
