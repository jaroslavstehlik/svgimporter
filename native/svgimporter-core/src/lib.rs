//! svgimporter-core: SVG parsing (usvg) and tessellation (lyon) for SVG Importer.
//! See DESIGN.md at the repository root. The public interface is the C ABI in `ffi`.

pub mod ffi;
pub mod fringe;
pub mod import;
