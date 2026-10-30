# SVG Importer

Import SVG files into Unity as lightweight, GPU-friendly meshes. The workflow is as simple as Unity sprites: drop an `.svg` file into your project and drag it into the scene, or use it in Unity UI.

## Installation

Requires Unity 6.0 or newer.

1. Open **Window → Package Manager**.
2. Click **+ → Install package from git URL…**
3. Enter:

```
https://github.com/jaroslavstehlik/svgimporter.git?path=/Packages/com.jaroslavstehlik.svgimporter
```

To pin a specific version, append a tag, for example `#v2.0.0`.

Example scenes can be imported from the **Samples** tab of the package in the Package Manager.

## Usage

Drop an `.svg` file into your project. It is imported as an SVG Asset, just like a texture is imported as a Sprite: select the file to change its import settings in the Inspector, then drag it into the scene or assign it to an **SVG Image** in your UI.

- Settings, including the defaults for new files: **Edit > Project Settings > SVG Importer**.
- Unity 6 has its own SVG importer, used by UI Toolkit. SVG Importer takes over SVG files *added* to the project. To keep using Unity's importer for a file, pick it in the importer dropdown at the top of the file's Inspector. To keep Unity's importer for all new files, turn off **Import New SVG Files** in the settings.
- SVG files that were already in the project: select them and use **Assets > SVG Importer > Import with SVG Importer**.

## Upgrading from SVG Importer 1.x

Version 1 converted every SVG file into an `.asset` file and deleted the original. Those SVG Assets still work. To convert them back to `.svg` files, use **Window > SVG Importer > Convert SVG Assets to SVG Files...**: each asset keeps its import settings and its references in scenes and prefabs. Commit or back up your project first; the project must use text serialization (Project Settings > Editor > Asset Serialization: Force Text).

## Platforms

SVG files are imported by a native library included in the package.

| | Import in the Editor | Import at runtime (`SVGAsset.Load`) | Rendering imported SVG Assets |
|---|---|---|---|
| Windows x86_64, macOS, Linux x86_64 | Yes | Yes | Yes |
| iOS (arm64), Android (arm64-v8a, armeabi-v7a) | | Yes | Yes |
| WebGL and other platforms | | **No** | Yes |

SVG Assets imported in the Editor render on every platform. Runtime import needs the native library, so on WebGL (and other platforms without one) `SVGAsset.Load` returns null, and `SVGAsset.TryLoad` says why: import the files in the Editor instead.

## Known limitations

- Filter effects, patterns, blend modes, group opacity, SVG animation and embedded images are not supported; masks are supported only when they are plain opaque shapes. The Inspector lists the unsupported features a file uses.
- Text is converted to shapes with the fonts installed on the machine that imports it, so it can look different on another machine. Convert text to curves for exact results.

## Documentation

See [svgimporter.com](https://svgimporter.com/) and the [YouTube channel](https://www.youtube.com/@Svgimporter).

## License

SVG Importer is free and open source under the [MIT license](https://github.com/jaroslavstehlik/svgimporter/blob/master/Packages/com.jaroslavstehlik.svgimporter/LICENSE.md): use it in commercial and non-commercial projects.

It includes third-party software, listed in [Third Party Notices](https://github.com/jaroslavstehlik/svgimporter/blob/master/Packages/com.jaroslavstehlik.svgimporter/Third%20Party%20Notices.md). Games and apps that include the native library should reproduce those notices, for example in their credits or legal notices.

Made something with SVG Importer? A "Made with SVG Importer" in your credits is appreciated, never required.

## Unity 2017

The last version for Unity 2017 and the Built-in Render Pipeline only is on the [`legacy/v1`](https://github.com/jaroslavstehlik/svgimporter/tree/legacy/v1) branch.
