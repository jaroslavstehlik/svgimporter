# Changelog

## [2.0.0] - Unreleased
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
- The Diffuse shaders now render in URP (lit by the main light) and the URP 2D Renderer (unlit).
- Removed the "Standard SVG" materials, which referenced a shader that was never included.
- Importer settings now live in the project (Window > SVG Importer > Settings creates `Assets/SVG Importer/Resources/svgImporterSettings.asset`), because packages installed from git are read-only. Without that asset the defaults are used.
- The plugin no longer creates an `Assets/Gizmos` folder in your project.
- Only SVG files inside the `Assets` folder are converted. Previously the importer also tried to convert, and delete, SVG files that other packages ship.
- Samples are re-saved in the Unity 6 format, without the removed GUILayer component.
- Fixed gradients that use bounding box units, the SVG default: coordinates such as `x2="1"` now span the shape instead of one pixel, and `gradientTransform` applies inside the bounding box.
- Fixed `gradientUnits="userSpaceOnUse"` never being recognised (the attribute name was misspelled).
- Fixed the radius of radial gradients with a percentage radius on shapes away from the canvas origin.
- Reimport existing SVG assets to apply these gradient fixes.

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
