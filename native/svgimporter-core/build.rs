// Generates the C# bindings for the Unity package from the C ABI in src/ffi.rs.
// The generated file is committed; CI checks that it's up to date (DESIGN.md, section 6.3).

fn main() {
    println!("cargo:rerun-if-changed=src/ffi.rs");
    csbindgen::Builder::default()
        .input_extern_file("src/ffi.rs")
        .csharp_dll_name("svgimporter_core")
        // iOS links the library statically.
        .csharp_dll_name_if("UNITY_IOS && !UNITY_EDITOR", "__Internal")
        .csharp_namespace("SVGImporter.Native")
        .csharp_class_name("NativeMethods")
        .csharp_class_accessibility("internal")
        .csharp_generate_const_filter(|name| name.starts_with("SVGI_"))
        .generate_csharp_file("../../Packages/com.jaroslavstehlik.svgimporter/Runtime/Native/NativeMethods.g.cs")
        .expect("failed to generate the C# bindings");
}
