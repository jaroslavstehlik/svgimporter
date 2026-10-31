# SVG Importer

Import SVG files into Unity as lightweight, GPU-friendly meshes. Drop an `.svg` file into your project and drag it into the scene, or use it in Unity UI, just as you would a sprite. It stays sharp at any size and any zoom.

[<img src="https://img.youtube.com/vi/XDEVNreEWYU/maxresdefault.jpg" width="50%" alt="SVG Importer feature video">](https://www.youtube.com/watch?v=XDEVNreEWYU)

## Installation

Requires Unity 6.0 or newer. In **Window → Package Manager**, click **+ → Install package from git URL…** and enter:

```text
https://github.com/jaroslavstehlik/svgimporter.git?path=/Packages/com.jaroslavstehlik.svgimporter
```

Example scenes are in the **Samples** tab of the package in the Package Manager. See the [package README](Packages/com.jaroslavstehlik.svgimporter/README.md) for usage, settings and platforms, and the [changelog](Packages/com.jaroslavstehlik.svgimporter/CHANGELOG.md) for what's new in 2.0.

## Features

- **Files from any vector editor**: Adobe Illustrator, Affinity Designer, Inkscape, CorelDRAW, Figma and others that export SVG.
- **Fast native import**, in the Editor and at runtime (`SVGAsset.Load`) on Windows, macOS, Linux, iOS and Android.
- **Shapes and strokes**: fills and strokes with transparency, holes, symbols, clip paths, masks made of plain opaque shapes, and text, converted to shapes with the installed fonts.
- **Exact gradients**: linear, radial and conical, with spread methods and focal points, computed per pixel and interpolated in sRGB as browsers do, so they are smooth at any zoom.
- **Soft edges**: an antialiased edge one screen pixel wide, whatever the transform or camera, without MSAA.
- **Every render pipeline**: Built-in, URP and the URP 2D Renderer, in Linear and Gamma color spaces.
- **Unity's 2D tools**: Sorting Layers and Sorting Group, 2D lights, Sprite Masks, 2D physics colliders and the Sprite Editor.
- **Unity UI**: the SVG Image component, with sliced images.
- **Opaque rendering**: opaque shapes use the depth buffer, so large scenes draw less.
- **Modifiers** that change graphics at runtime: color, transform, blur, bones, twirl, sphere and UV.

## Not supported

- Filter effects such as blur and drop shadow, patterns, blend modes, SVG animation and embedded images.
- Group opacity is approximate. The **Exact Group Opacity** import setting matches browsers for groups whose children are opaque.
- Tools that need a `Sprite` asset, such as 2D Animation, Sprite Shape and Tilemap. For those files, use Unity's own SVG importer, which you can pick per file.

The Inspector lists the unsupported features a file uses.

## Unity 2017

The last version for Unity 2017 is on the [`legacy/v1`](https://github.com/jaroslavstehlik/svgimporter/tree/legacy/v1) branch.

## License

SVG Importer is free and open source under the [MIT license](Packages/com.jaroslavstehlik.svgimporter/LICENSE.md): use it in commercial and non-commercial projects.

It includes third-party software, listed in [Third Party Notices](Packages/com.jaroslavstehlik.svgimporter/Third%20Party%20Notices.md). Games and apps that include the native library should reproduce those notices, for example in their credits or legal notices.

Made something with SVG Importer? A "Made with SVG Importer" in your credits is appreciated, never required.

More at [svgimporter.com](https://svgimporter.com/) and on the [YouTube channel](https://www.youtube.com/@Svgimporter).
