// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using UnityEditor;

namespace SVGImporter
{
    /// <summary>
    /// Draws the import settings. SVG Assets and the .svg importer use the same serialized
    /// field names (_format, _antialiasing, ...), so one implementation serves both.
    /// </summary>
    internal static class SVGImportSettingsGUI
    {
        internal static class Styles
        {
            public static readonly GUIContent rendering = EditorGUIUtility.TrTextContent("Rendering");
            public static readonly GUIContent meshes = EditorGUIUtility.TrTextContent("Meshes");
            public static readonly GUIContent normalsAndTangents = EditorGUIUtility.TrTextContent("Normals & Tangents");
            public static readonly GUIContent svgDocument = EditorGUIUtility.TrTextContent("SVG Document");

            public static readonly GUIContent format = EditorGUIUtility.TrTextContent("Rendering", "Transparent: every shape is drawn with transparency, in drawing order; the asset sorts as one transparent object.\nOpaque + Transparent: opaque shapes are drawn as opaque geometry with depth, so hidden pixels are skipped; transparent shapes and soft edges are drawn on top. Shapes are spread slightly along z.\nUI (Canvas): for SVG Image on a Canvas.");
            public static readonly GUIContent useGradients = EditorGUIUtility.TrTextContent("Gradients", "Whether the mesh and shaders support gradients. Auto: only when the SVG has gradients.");
            public static readonly GUIContent softEdges = EditorGUIUtility.TrTextContent("Soft Edges", "Adds a soft one-pixel edge to every shape in the mesh. For MSAA instead, turn this off and enable MSAA on the camera.");
            public static readonly GUIContent sharpEdgesHint = EditorGUIUtility.TrTextContent("Edges are sharp unless the camera uses MSAA.");
            public static readonly GUIContent antialiasingWidth = EditorGUIUtility.TrTextContent("Soft Edge Width", "Width of the soft edge, in screen pixels.");
            public static readonly GUIContent exactGroupOpacity = EditorGUIUtility.TrTextContent("Exact Group Opacity", "Stops shapes in a semi-transparent group from showing through each other. Adds vertices.");
            public static readonly GUIContent meshCompression = EditorGUIUtility.TrTextContent("Mesh Compression", "Reduces the mesh's file size, but may introduce irregularities and visual artefacts.");
            public static readonly string meshCompressionWarning = L10n.Tr("Mesh compression can introduce visual artefacts. The higher the compression, the higher the risk.");
            public static readonly GUIContent optimizeMesh = EditorGUIUtility.TrTextContent("Optimize Mesh", "Reorders vertices and indices for better GPU performance.");
            public static readonly GUIContent scale = EditorGUIUtility.TrTextContent("Scale", "Size of the mesh in units per SVG pixel. Doesn't affect the mesh's detail.");
            public static readonly GUIContent quality = EditorGUIUtility.TrTextContent("Quality", "Vertices per unit: higher values give smoother curves and more vertices.");
            public static readonly GUIContent depthOffset = EditorGUIUtility.TrTextContent("Depth Offset", "Distance along z between shapes that overlap, so the depth test keeps their drawing order.");
            public static readonly GUIContent compressDepth = EditorGUIUtility.TrTextContent("Compress Depth", "Shapes that don't overlap share a depth, so the asset is thinner along z.");
            public static readonly GUIContent spriteEditor = EditorGUIUtility.TrTextContent("Sprite Editor", "Edit the pivot and the slicing border in the Sprite Editor.");
            public static readonly GUIContent generateCollider = EditorGUIUtility.TrTextContent("Generate Collider", "Stores the outline for SVG Collider 2D.");
            public static readonly GUIContent generateNormals = EditorGUIUtility.TrTextContent("Normals", "Generates normals for lighting.");
            public static readonly GUIContent generateTangents = EditorGUIUtility.TrTextContent("Tangents", "Generates tangents for normal-mapped lighting. Needs normals.");
            public static readonly GUIContent keepSVGFile = EditorGUIUtility.TrTextContent("Keep SVG File", "Keeps the SVG text in the asset, in builds too. Increases the asset's size.");
            public static readonly GUIContent useLayers = EditorGUIUtility.TrTextContent("Use Layers", "Keeps the SVG's layers instead of one mesh, for modifiers and scripts.");
            public static readonly GUIContent ignoreSVGCanvas = EditorGUIUtility.TrTextContent("Ignore SVG Canvas", "Trims the asset to its content's bounds instead of the SVG canvas.");
        }

        /// <summary>
        /// Draws the import settings found by find, which takes a setting's name as SVG Assets
        /// serialize it, without the underscore ("format", "vpm", ...). Returns true when the
        /// Sprite Editor button, drawn if asked, was pressed.
        /// </summary>
        public static bool Draw(System.Func<string, SerializedProperty> find, bool showSpriteEditorButton)
        {
            SerializedProperty format = find("format");
            SerializedProperty antialiasing = find("antialiasing");
            SerializedProperty meshCompression = find("meshCompression");
            SerializedProperty generateNormals = find("generateNormals");
            bool openSpriteEditor = false;

            EditorGUILayout.LabelField(Styles.rendering, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(format, Styles.format);
            EditorGUILayout.PropertyField(find("useGradients"), Styles.useGradients);
            EditorGUILayout.PropertyField(antialiasing, Styles.softEdges);
            if (!antialiasing.hasMultipleDifferentValues && !antialiasing.boolValue)
                EditorGUILayout.LabelField(GUIContent.none, Styles.sharpEdgesHint, EditorStyles.miniLabel);
            SerializedProperty exactGroupOpacity = find("exactGroupOpacity");
            if (exactGroupOpacity != null)
                EditorGUILayout.PropertyField(exactGroupOpacity, Styles.exactGroupOpacity);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.meshes, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(meshCompression, Styles.meshCompression);
            if (!meshCompression.hasMultipleDifferentValues && meshCompression.enumValueIndex != (int)SVGMeshCompression.Off)
                EditorGUILayout.HelpBox(Styles.meshCompressionWarning, MessageType.Warning);
            EditorGUILayout.PropertyField(find("optimizeMesh"), Styles.optimizeMesh);
            EditorGUILayout.PropertyField(find("scale"), Styles.scale);
            EditorGUILayout.PropertyField(find("vpm"), Styles.quality);
            if (format.hasMultipleDifferentValues || format.enumValueIndex == (int)SVGAssetFormat.OpaqueAndTransparent)
            {
                EditorGUILayout.PropertyField(find("depthOffset"), Styles.depthOffset);
                EditorGUILayout.PropertyField(find("compressDepth"), Styles.compressDepth);
            }
            SVGPivotGUI.Draw(find("customPivotPoint"), find("pivotPoint"));
            if (showSpriteEditorButton)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    openSpriteEditor = GUILayout.Button(Styles.spriteEditor, EditorStyles.miniButton);
                }
            }
            EditorGUILayout.PropertyField(find("generateCollider"), Styles.generateCollider);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.normalsAndTangents, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(generateNormals, Styles.generateNormals);
            // Tangents need normals; the import ignores them without, so the setting is only disabled.
            using (new EditorGUI.DisabledScope(!generateNormals.boolValue && !generateNormals.hasMultipleDifferentValues))
                EditorGUILayout.PropertyField(find("generateTangents"), Styles.generateTangents);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.svgDocument, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(find("keepSVGFile"), Styles.keepSVGFile);
            EditorGUILayout.PropertyField(find("useLayers"), Styles.useLayers);
            EditorGUILayout.PropertyField(find("ignoreSVGCanvas"), Styles.ignoreSVGCanvas);

            return openSpriteEditor;
        }

        /// <summary>The settings of SVG files or 1.x SVG Assets, which serialize them as _format, _vpm, ...</summary>
        public static bool Draw(SerializedObject serializedObject, bool showSpriteEditorButton)
        {
            return Draw(name => serializedObject.FindProperty("_" + name), showSpriteEditorButton);
        }

        public static void DrawErrors(SVGError[] errors, bool exactGroupOpacity = false)
        {
            if (errors == null)
                return;

            foreach (SVGError error in errors)
            {
                switch (error)
                {
                    case SVGError.CorruptedFile:
                        EditorGUILayout.HelpBox(L10n.Tr("The SVG file can't be read."), MessageType.Error);
                        break;
                    case SVGError.Syntax:
                        EditorGUILayout.HelpBox(L10n.Tr("The SVG file has a syntax error."), MessageType.Error);
                        break;
                    case SVGError.Mask:
                        EditorGUILayout.HelpBox(L10n.Tr("Masks are left out, except masks made of opaque shapes."), MessageType.Warning);
                        break;
                    case SVGError.Image:
                        EditorGUILayout.HelpBox(L10n.Tr("Images are left out."), MessageType.Warning);
                        break;
                    case SVGError.Filter:
                        EditorGUILayout.HelpBox(L10n.Tr("Filters, such as blurs and drop shadows, are left out."), MessageType.Warning);
                        break;
                    case SVGError.Pattern:
                        EditorGUILayout.HelpBox(L10n.Tr("Pattern fills and strokes are left out."), MessageType.Warning);
                        break;
                    case SVGError.BlendMode:
                        EditorGUILayout.HelpBox(L10n.Tr("Blend modes aren't supported: shapes are blended normally."), MessageType.Warning);
                        break;
                    case SVGError.GroupOpacity:
                        EditorGUILayout.HelpBox(exactGroupOpacity
                            ? L10n.Tr("Semi-transparent shapes in semi-transparent groups still show through each other.")
                            : L10n.Tr("Shapes in semi-transparent groups show through each other. Exact Group Opacity fixes this."), MessageType.Info);
                        break;
                    case SVGError.TextSkipped:
                        EditorGUILayout.HelpBox(L10n.Tr("Some text was left out."), MessageType.Warning);
                        break;
                    case SVGError.Unknown:
                        EditorGUILayout.HelpBox(L10n.Tr("The SVG file couldn't be imported."), MessageType.Error);
                        break;
                }
            }
        }
    }
}
