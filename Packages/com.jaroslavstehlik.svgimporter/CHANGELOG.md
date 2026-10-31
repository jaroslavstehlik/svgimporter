# Changelog

## [2.0.0] - Unreleased
- **SVG Importer is now under the MIT license.** The package lists the third-party software it includes, the Rust crates of the native library, in `Third Party Notices.md`.
- **SVG files are now imported like any other asset.** An `.svg` file stays in the project and is imported as an SVG Asset, with its settings in the Inspector, instead of being converted into an `.asset` file and deleted.
- SVG Importer takes over SVG files added to the project. Turn off **Import New SVG Files** in the settings to keep Unity's own SVG importer, or switch individual files in the Inspector.
- New: **Window > SVG Importer > Convert SVG Assets to SVG Files...** converts 1.x SVG Assets back to `.svg` files and keeps their references.
- The samples now use `.svg` files.
- Removed the per-user "Asset Postprocessor" on/off switch; use the Import New SVG Files project setting.
- Converted to a Unity Package Manager package (`com.jaroslavstehlik.svgimporter`), installable from a git URL.
- Requires Unity 6.0 or newer. Unity 2017 users can use the `legacy/v1` branch (tag `v1.1.6`).
- Removed analytics tracking and the bug report window.
- Example projects are now samples, importable from the Package Manager.
- Added Universal Render Pipeline (URP) support, including the URP 2D Renderer. The Built-in Render Pipeline is still supported.
- Fixed colors rendering washed out in Linear color space projects (the Unity 6 default).
- Fixed gradients not updating at runtime in URP.
- Fixed importing SVG files failing with a NullReferenceException on Unity 6.
- Fixed SVG Image not rendering when added through Add Component on Unity 6.
- The Diffuse shaders now render in URP (lit by the main light) and the URP 2D Renderer (lit by 2D lights).
- Removed the "Standard SVG" materials, which referenced a shader that was never included.
- Settings are in **Edit > Project Settings > SVG Importer** (also Window > SVG Importer > Settings), stored in `ProjectSettings/SVGImporterSettings.asset` with the project's other settings, instead of an asset in `Assets`. Projects that have the settings asset of an earlier 2.0 build get its values once; the asset can then be deleted. Builds include the one runtime setting, the antialiasing width, automatically.
- The plugin no longer creates an `Assets/Gizmos` folder in your project.
- Only SVG files inside the `Assets` folder are converted. Previously the importer also tried to convert, and delete, SVG files that other packages ship.
- Samples are re-saved in the Unity 6 format, without the removed GUILayer component.
- Fixed gradients that use bounding box units, the SVG default: coordinates such as `x2="1"` now span the shape instead of one pixel, and `gradientTransform` applies inside the bounding box.
- Fixed `gradientUnits="userSpaceOnUse"` never being recognised (the attribute name was misspelled).
- Fixed the radius of radial gradients with a percentage radius on shapes away from the canvas origin.
- Reimport existing SVG assets to apply these gradient fixes.
- **New import engine.** SVG files are parsed and tessellated by a native core (usvg and lyon), about 15 times faster than the C# parser of 1.x and supporting more of SVG, such as gradient strokes, text (with the system's fonts) and clip paths. It ships for the Windows, macOS and Linux Editors and players, iOS and Android. The C# parser, LibTessDotNet and the Import Engine setting are removed; files import differently in places (see the conformance numbers in DESIGN.md), and every SVG file reimports once. SVG Importer's `conicalGradient` extension is still supported. Runtime import (`SVGAsset.Load`) isn't available on other platforms, such as WebGL, where it returns null and logs why; SVG Assets imported in the Editor render everywhere.
- Fixed shape and mesh bounds that could miss their right or bottom edge.
- Clip paths are applied as geometry, including `objectBoundingBox` units, nested clip paths and clipped elements inside clip paths, and applies masks that are plain opaque shapes. Strokes now bevel at the right angle for their `stroke-miterlimit`.
- The Inspector lists more unsupported features found during import: filters, patterns, blend modes, group opacity and skipped text.
- Gradients are now smooth when zoomed in: shaders compute the position along a gradient instead of reading it from a 512×512 lookup texture, which made radial gradients blocky and left a seam in conical gradients. The gradient shape texture is gone, and `SVGAtlas.gradientShapeTexture` is obsolete.
- Gradient colours are exact: the gradient atlas stores each gradient's stops (up to 64) instead of 128 sampled colours, and shaders interpolate between them in sRGB, as browsers do. Hard stops are sharp at any size, colours of neighbouring gradients can't bleed in, and gradients look the same in Gamma and Linear projects. Requires shader model 3.5 (OpenGL ES 3.0, Metal, Vulkan or Direct3D 11).
- Fixed gradients showing the wrong colours or disappearing in the Editor after entering or leaving play mode, script reloads, reimports or long editing sessions: the gradient atlas is rebuilt consistently after each of them, and grows instead of running out of space (512 gradients before).
- Gradients are identified by exactly what they draw. The old text hash rounded stop offsets and alphas to 1/999, so slightly different gradients could share one atlas slot. `SVGGradient.GetStops()` returns a gradient's merged stops; `SVGGradient.Evaluate` now matches the shaders, including hard stops.
- Removed the per-machine "Support Service" switch; drag and drop and gradient updates are always on. Its Start button used to turn it off.
- Fixed dropping an SVG Asset on empty space in the Hierarchy throwing an exception.
- One import pipeline, `SVGImport.Import(svgText, SVGImportOptions)`, used by the Editor importer, `SVGAsset.Load` and 1.x SVG Assets. Imports depend only on the SVG text and the options: the same file imports the same way every time (no time stamps), and no state is left between imports.
- `SVGAsset.Load` works in edit mode too, and imports with the settings a new SVG file gets, so it gives the same asset as importing the file. It has an overload taking `SVGImportOptions`, and returns null when the text can't be imported; the new `SVGAsset.TryLoad` also returns the import's messages. Neither logs to the Console. Assets they create destroy their mesh when destroyed.
- Import warnings and errors are logged by the importer, so they show again when an import comes from the cache, instead of once per import in the console.
- Gradients support `spreadMethod` (pad, reflect and repeat) and the focal point of radial gradients (`fx`, `fy` and SVG 2's `fr`), including focal points outside the circle, as browsers draw them. All 87 resvg gradient tests pass. `CCGradient` has `spreadMethod`, `focalOffset` and `focalRadius`. SVG files reimport once.
- New antialiasing: the soft edge is centred on the shape's outline, so antialiased shapes keep their true size instead of growing by half a pixel, and holes no longer shrink. Its width is measured in screen pixels for any transform, camera or canvas, including rotation, non-uniform scale and perspective; sharp corners are bevelled instead of shooting out spikes; and semi-transparent shapes no longer show a brighter or darker line where the fill meets the edge. The Antialiasing Width setting keeps its meaning (2 is a one-pixel edge). Reimport to update existing files; this happens automatically.
- Fixed SVG Collider 2D keeping the previous asset's shape when the renderer's asset changed or was reimported.
- Fixed meshes leaking: a renderer's own mesh (tinted, sliced or layered) is freed with the renderer, and meshes derived from an asset with it.
- Fixed assets imported with Use Layers drawing a white rectangle in SVG Image, and producing invalid vertices in sliced renderers: they now have a mesh and bounds built from their layers.
- The **Antialiasing** import setting is now called **Soft Edges**, and the project setting **Antialiasing Width** is **Soft Edge Width**, in screen pixels (the default, 1, is the previous 2). It adds a soft edge to the mesh; it isn't MSAA. For MSAA, turn Soft Edges off and enable MSAA on the camera. In scripts it's `softEdges`.
- **Exact Group Opacity** (import setting, off by default): semi-transparent groups can look as they do in browsers, Illustrator and Figma. Where a group's children overlap, the parts hidden by opaque children are removed at import, so lower shapes no longer show through upper ones. It adds vertices along the cuts and can show hairline seams where cut shapes meet, so turn it on for the assets that need it. Translucent children that overlap still show through each other; the Inspector flags files with group opacity.
- The pivot is set like a sprite's: a **Pivot** popup (Center, Top Left, …, Custom) and, for Custom, a **Custom Pivot** with (0, 0) at the bottom left, as in Unity's sprite importer. Existing pivots are kept.
- SVG files open in Unity's **Sprite Editor** (the Sprite Editor button in the import settings, or Window > 2D > Sprite Editor) to drag the pivot and the slicing border. SVG Importer now depends on the 2D Sprite package (`com.unity.2d.sprite`), which Unity adds with it.
- Removed code that was no longer used: the `SVGStrokeRenderer`, `SVGRenderTexture` and `StrokeRendererLegacy` components, the additive and multiply "Others" shaders, the SVG utility window, `SVGCombineMesh`, `SVGMeshCutter`, unused math, Bézier and mesh helpers (for example `SVGMeshUtils.MergeMeshes`, `SVGAsset.MergeLayersToShapes`, `SVGAtlas.AssignMaterialGradients`), Unity 4 and 5 code paths and commented-out code.
- New: `SVGAsset.importOptions`, `SVGFileImporter.importOptions` (settable, as `TextureImporter`'s settings are) and `SVGAsset.importHash`.
- Fixed the Opaque format with **Compress Depth** throwing an exception or drawing overlapping shapes in the wrong order when an element has both a fill and a stroke.
- Meshes may have more than 65,535 vertices (32-bit indices); before, such files imported as an empty asset with only a console error.
- Fixed SVG files imported while the game runs in the Editor being saved empty.
- Fixed the Color Modifier with a layer selection darkening colours once per vertex.
- SVG Images no longer reset the canvas's additional shader channels, which TextMeshPro on the same canvas needs.
- Fixed tinted or sliced SVG Renderers keeping the previous asset's colours or vertices after switching asset, and `SVGRenderer.color` and `type` having no effect when set from a script.
- Gradient shaders read the atlas from global shader properties (`_SVGGradientAtlas`), so custom materials on SVG Renderers and Images always draw gradients correctly, also after the atlas grows or is replaced.
- Fixed the Texture Overlay shaders looking washed out in Linear color space. Vertex colours use the exact sRGB curve, as gradient stops do.
- The native core refuses documents larger than 64 MB and imports producing more than 16 million vertices, instead of possibly running out of memory.
- Fixed SVG Renderers with **Use Layers** (used by modifiers) drawing another asset's gradient colours.
- Scenes update when an SVG file changes on disk; before, only after **Apply** in the Inspector.
- Gradients are dithered, as in browsers: up to half an 8-bit step of noise per pixel, so subtle gradients don't show bands. Flat colours are unchanged.
- Fixed meshes placed one canvas height too low when **Ignore SVG Canvas** is off. Reimport affected assets; their pivot now matches the canvas.
- Modifiers no longer rebuild the renderer from scratch every frame. One rebuild per frame applies all of a renderer's modifiers. It reuses the renderer's mesh and buffers instead of allocating new ones. It also leaves materials and SVG Collider 2D alone, where every frame used to rebuild the collider's physics shape.
- SVG Image reuses its vertex buffers when the Canvas rebuilds it instead of allocating new ones every time; reading `SVGAsset.sharedMesh` and `hasGradients` no longer allocates; SVG Renderers set their materials only when they change.
- The gradient atlas releases its textures, materials and meshes however it is destroyed. When it fills up during a renderer's rebuild, it is replaced without rebuilding that renderer halfway through, and SVG Images rebuild after the Canvas update instead of logging errors.
- Fixed `SVGRenderer.meshFilter` and `meshRenderer` returning null on objects that were never enabled.
- Fixed gradients drawn with the wrong colours in SVG Image and sliced renderers for assets imported with Use Layers.
- Fixed tinted, sliced or gradient copies of meshes over 65,535 vertices.
- Fixed SVG Sorter counting the sorting order up on every sort, and marking the scene modified every frame.
- Fixed `SVGPath` bounds using the horizontal extent for the vertical one.
- Modifiers on an SVG Image have no effect (they work with SVG Renderer); they now say so in the Inspector and the console.
- SVG Image's **Apply** button changes an imported `.svg` file's format setting and reimports it, instead of rewriting the imported asset.
- Selecting an SVG file no longer marks its import settings as modified.
- Removed the SVG Editor window, which the Sprite Editor replaces, and the About window and the Help menus under Window > SVG Importer. The components and the importer have help buttons that open the documentation.
- 1.x SVG Assets (`.asset` files) are read-only in the Inspector, with a **Convert to SVG File** button; their own Apply, Revert, Recover SVG File and Save Mesh File buttons are removed. Convert them to change their settings, or use Window > SVG Importer > Convert SVG Assets to SVG Files....
- The import settings, the 1.x SVG Asset Inspector and the defaults in Project Settings are drawn by the same code, so they show the same settings, with the same names and help texts. Project Settings now groups them as the Inspector does.
- **Report Import Issue** moved from the bottom of the import settings to the importer's context menu (the ⋮ button).
- SVG Image's Inspector is built on uGUI's Graphic Inspector, so it has Maskable and Raycast Padding like Image, and edits every selected image.
- The SVG Renderer's Sorting Layer field no longer uses Unity's internal API.
- **Generate Collider** outlines are computed by the native core, in one pass, instead of merging shape after shape with Clipper, which made large files slow to import. Holes are kept: a ring gets a ring-shaped collider instead of a disc, unless other shapes cover the hole. The C# Clipper library is removed. Files with Generate Collider on reimport once.
- SVG Renderer looks after its MeshFilter and MeshRenderer, which are now hidden, as a sprite's are, and are removed with it. Their settings are on SVG Renderer: Sorting Layer and Order in Layer, and under Additional Settings shadows, light probes and the Rendering Layer Mask. New renderers don't cast or receive shadows, as sprites don't; existing ones keep what their MeshRenderer had.
- Sorting uses Unity's Sorting Layers, Order in Layer and Sorting Group. **SVG Sorter** is now **SVG Opaque Sorting Group**: it places the SVG Renderers below it along z in hierarchy order, for Opaque + Transparent assets, which the depth test orders. It no longer changes Order in Layer, and it places renderers again only when something changes, rather than every frame: the hierarchy below it (renderers moved at any depth, the group's or a renderer's children reordered) or an asset; in the Editor also any hierarchy change, undo and redo, and renderers moved by hand. In play mode, after reordering the children of other objects below it, call `Sort()`. **SVG Opaque Sort Override** leaves an object (and optionally its children) where it is, replacing SVG Renderer's Override Sorter settings. Scenes with an SVG Sorter keep it as an SVG Opaque Sorting Group.
- **2D lights** light SVG Renderers in the URP 2D Renderer, as they light sprites with the default lit material, including light blend styles and the HDR emulation scale. Without 2D lights nothing changes. Turn off **Receive 2D Lights** on an SVG Renderer (`receive2DLights`) to draw it unlit, like the Sprite-Unlit material. Lit renderers on a sorting layer that no light reaches are dark, as sprites are. The Universal Renderer and the Built-in Render Pipeline still draw SVGs unlit, and SVG Images are never lit.

### Script API changes (2.0)

2.0 cleans up the script API, and scripts written for 1.x need these changes. Everything users need is in the `SVGImporter` namespace; the rest of the package (geometry and mesh code, the gradient atlas, the native bindings, the import pipeline, the debug components and the Inspectors) is internal. Serialized data keeps loading: assets, scenes and prefabs don't need changes.

| 1.x | 2.0 |
|---|---|
| `CCGradient`, `CCGradientColorKey`, `CCGradientAlphaKey` | `SVGGradient`, `SVGGradientColorKey`, `SVGGradientAlphaKey` |
| `FILL_BLEND.OPAQUE`, `FILL_BLEND.ALPHA_BLENDED` | `SVGFillBlend.Opaque`, `SVGFillBlend.Transparent` |
| `FILL_TYPE.SOLID` / `GRADIENT` / `TEXTURE` | `SVGFillType.Solid` / `Gradient` / `Texture` |
| `GRADIENT_TYPE.LINEAR` / `RADIAL` / `CONICAL` | `SVGGradientType.Linear` / `Radial` / `Conical` |
| `SVGShapeType.NONE` / `FILL` / `STROKE` / `ANTIALIASING` | `SVGShapeType.None` / `Fill` / `Stroke` / `SoftEdge` |
| `SVGAssetFormat.Opaque`, `SVGAssetFormat.uGUI` | `SVGAssetFormat.OpaqueAndTransparent`, `SVGAssetFormat.Canvas` |
| `SVGRenderer.Type`, `SVGImage.Type` | `SVGDrawMode` |
| `SVGRenderer.type` | `SVGRenderer.drawMode` (`SVGImage.type` is unchanged, as on Image) |
| `SVGRenderer.onVectorGraphicsChanged` (a field) | `SVGRenderer.vectorGraphicsChanged` (an event) |
| `SVGRenderer.OnPrepareForRendering` | `SVGRenderer.willBuildMesh`, an event without the `force` argument |
| `SVGRenderer.UpdateRenderer()`, `UpdateMaterials()` | `SVGRenderer.SetAllDirty()` |
| `SVGModifier.PrepareForRendering(layers, asset, force)` | `SVGModifier.ModifyLayers(layers, asset)`; modifiers require an SVG Renderer |
| `SVGTransformModifier.Properties.positon` | `position` |
| `SVGAsset.layersClone` | `SVGAsset.CloneLayers()` |
| `SVGAsset.antialiasing`, `vpm`, `scale` and the other import settings | `SVGAsset.importOptions.softEdges`, `.verticesPerMeter`, `.scale`, … |
| `SVGAsset.Load(text, SVGImporterSettings)` | `SVGAsset.Load(text, SVGImportOptions)` or `TryLoad` |
| `SVGImporterSettings.default…` | Project Settings > SVG Importer; `SVGImportOptions.Default` in scripts |
| `SVGPath` (global namespace) | `SVGImporter.SVGPath` |
| `SVGSorter` (`depthOffset`, `layerIndex`, `sort`) | `SVGOpaqueSortingGroup` (`depthOffset`, `Sort()`); Order in Layer is Unity's |
| `SVGRenderer.overrideSorter`, `overrideSorterChildren` | An `SVGOpaqueSortOverride` component (`includeChildren`) |
| `SVGImportEngine`, `importEngine`, `ISVGReference`, `AddReference`, `RemoveReference`, `SVGLegacyAnimator`, the obsolete atlas members | Removed |

## Legacy releases


Version 1.1.6
Release Date: 2 August. 2017
- Fixed, Antialiasing on negative scale
- Updated Example projects
- Added new Tutorials on Youtube
https://www.youtube.com/watch?v=uYBqOPY8y2M

Version 1.1.5
Release Date: 19 July. 2017
- Removed, SVG Camera
- Fixed, Antialiasing
- Code revision

Version 1.1.4
Release Date: 30 May. 2017
- Added, Virtual Reality Support
- Fixed, Shader compatibility for VR
- Fixed, SVG Editor
- Fixed, SVGImporterSettings
- Updated, About screen

Version 1.1.3
Release Date: X July. 2016

- Added, SVG Layers for better SVG manipulation
- Added, Dynamic gradient atlasing
- Added, Dynamic antialiasing
- Added, Demo mesh dodifiers SVGImporter/Plugins/Core/Modifiers
- Added, Hologram demo SVGImporter/Example Projects/Hologram Demo
- Added, Infinite scene demo SVGImporter/Example Projects/Infinite Scene Demo
- Added, Realtime import demo SVGImporter/Example Projects/Realtime Import Demo
- Added, Motion Blur demo SVGImporter/Example Projects/SVG Modifier Demos/Motion Blur Demo
- Added, UV Modifier demo SVGImporter/Example Projects/SVG Modifier Demos/Overlay Texture Demo

- Improved SVG Frame Animator

- Fixed, Instancing prefabs having purple material
- Fixed, Missing mesh on Undo operation
- Fixed, Gradient missing issues
- Fixed, Gradient caching bug
- Fixed, Antialiasing rendering

Version 1.1.2
Release Date: 8 February. 2016

- Added, Realtime SVG Import "experimental"
- Added, SVG Renderer Image Type "Support for 9-slicing"

- Improved, 9-slicing

- Fixed Most Warnings
- Fixed Windows Phone Support
- Fixed Opaque import

--------------------------------------------
Version 1.1.1
Release Date: 4. December. 2015

- Fixed, Transparency on elements
- Fixed, Transparency on gradients

--------------------------------------------

Version 1.1.0
Release Date: 29. November. 2015

- Added, Standard and Diffuse Shader - does not support gradients
- Added, Use Gradients option for tweaking shader performance
- Added, Antialiasing which will create feathered edges on every element
- Added, SVG Render Texture, for special use cases
- Added, SVG Renderer, Override sorting children
- Added, SVG Frame Animator, previous and next frame buttons and current frame is hilighted
- Added, SVG Asset, Ignore canvas option when set to off it will preserve the original canvas composition

- Fixed, SVG Sorter, wrong sorting order
- Fixed, SVG Renderer, sortingLayerName
- Fixed, SVG Renderer, when colored and duplicated the mesh disapeared

--------------------------------------------

Version 1.0.9
Release Date: 1. November. 2015

- Added, Hierarchy window Drag And Drop Support
- Added, SVG Editor for Unity4.6
- Added, Optimize mesh
- Added, Generate Normals and Tangents
- Added, SVG Collider offset

- Improved, Inkscape, CorelDraw & Flash compatibility
- Improved, SVG Frame Animator Drag And Drop support
- Improved, SVG Renderer sorting layer inspector

- Fixed, SVG Renderer & SVG Image unnecessary Garbage Collection
- Fixed, Stroke Opening & Closing on SVG Path

--------------------------------------------

Version 1.0.8
Release Date: 15. October. 2015

- Added, Custom Material Overrides
- Added, Frame Animator UI Support
- Added, Mesh Modifier support
- Added, Stroke Renderer
- Added, SVG Sorter

--------------------------------------------

Version 1.0.7
Release Date: 5. September. 2015

- Added, Unity 5.2 UI support
- Added, 9-slice mesh slicing "experimental"
- Added, Mesh Compression
- Added, SVG File include/exclude option

- Fixed, SVG Renderer, recoloring artwork
- Fixed, SVG Asset, triangle count info
- Fixed, Curves Demo, startup

--------------------------------------------

Version 1.0.6
Release Date: 3. September. 2015

- Added, Experimental Clip Path support
- Added, Symbol support
- Added, 9-slice support
- Added, Basic CSS support
- Added, SVG Editor, modify pivot-point and 9-slice border
- Added, SVG Image, sliced type

- Fixed, SVG Atlas texel offset for Mobile devices.
- Fixed, SVG Atlas increased resolution for less artefacts
- Fixed, Parsing Fill Opacity and Stroke Opacity
- Fixed, Stroke exceptions
- Fixed, Generate colliders default settings
- Fixed, Revert prefab

--------------------------------------------

Version 1.0.5
Release Date: 31. July. 2015

- Fixed, SVG Relative Path Import Short Codes
- Fixed, SVG Polyline fill rendering
- Fixed, Stroke Rendering
- Fixed, SVG Image Black on Reimport
- Fixed, Polygon simplification
- Fixed, Recolor SVG Renderer switching from Opaque to Transparent

--------------------------------------------

Version 1.0.4
Release Date: 25. July. 2015

- Added, Automatic Colliders
- Added, Frame by Frame animation
- Added, Real-Time Stroke Renderer
- Added, Report Bad Import Button
- Added, Full SVG 1.1 stroke support
- Added, Miter, Miter-clip, Round, Bevel join types
- Added, But, Round, Square cap types
- Added, Dash Arrays

- Fixed, SVG shortcodes
- Fixed, SVG Atlas
- Fixed, Line endings

--------------------------------------------

Version 1.0.3
Release Date: 25. June. 2015

- Added, Example project file

- Fixed, Inkscape SVG files
- Fixed, SVG Image. Switching of the image during runtime

--------------------------------------------

Version 1.0.2
Release Date: 14. June. 2015

- Fixed, support of group transparency on gradients
- Fixed, Switching Vector Graphics Asset during runtime
- Fixed, Wrong triangle winding
- Fixed, Recoloring opaque geometry
- Fixed, Correct error reporting
- Fixed, Wrong gradient rendering on desktop

- Cleaned, commented SVG Renderer component
- Cleaned, warnings on unused code

--------------------------------------------

Version 1.0.1
Release Date: 7. June. 2015

- Unity 4.6.0f1 support
- SVG Asset serializes only necessary assets
- Multiple SVG Asset editing
- Point reduction

--------------------------------------------

Version 1.0.0
Release Date: 1. June. 2015

- Unity 5.0.0 support
- first stable build
