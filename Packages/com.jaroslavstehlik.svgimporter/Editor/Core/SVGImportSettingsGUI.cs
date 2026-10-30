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
        /// <summary>
        /// Whether Unity's Sprite Editor (2D Sprite package) is installed: it edits the pivot and
        /// border of SVG files (see SVGSpriteEditorDataProvider).
        /// </summary>
        internal static bool spriteEditorAvailable
        {
            get { return System.Type.GetType("UnityEditor.U2D.Sprites.SpriteEditorWindow, Unity.2D.Sprite.Editor") != null; }
        }

        /// <summary>Draws the settings. Returns true when the SVG Editor button was pressed.</summary>
        public static bool Draw(SerializedObject serializedObject, bool showSVGEditorButton)
        {
            SerializedProperty format = serializedObject.FindProperty("_format");
            SerializedProperty useGradients = serializedObject.FindProperty("_useGradients");
            SerializedProperty antialiasing = serializedObject.FindProperty("_antialiasing");
            SerializedProperty meshCompression = serializedObject.FindProperty("_meshCompression");
            SerializedProperty optimizeMesh = serializedObject.FindProperty("_optimizeMesh");
            SerializedProperty scale = serializedObject.FindProperty("_scale");
            SerializedProperty vpm = serializedObject.FindProperty("_vpm");
            SerializedProperty depthOffset = serializedObject.FindProperty("_depthOffset");
            SerializedProperty compressDepth = serializedObject.FindProperty("_compressDepth");
            SerializedProperty customPivotPoint = serializedObject.FindProperty("_customPivotPoint");
            SerializedProperty pivotPoint = serializedObject.FindProperty("_pivotPoint");
            SerializedProperty generateCollider = serializedObject.FindProperty("_generateCollider");
            SerializedProperty generateNormals = serializedObject.FindProperty("_generateNormals");
            SerializedProperty generateTangents = serializedObject.FindProperty("_generateTangents");
            SerializedProperty keepSVGFile = serializedObject.FindProperty("_keepSVGFile");
            SerializedProperty useLayers = serializedObject.FindProperty("_useLayers");
            SerializedProperty ignoreSVGCanvas = serializedObject.FindProperty("_ignoreSVGCanvas");
            SerializedProperty exactGroupOpacity = serializedObject.FindProperty("_exactGroupOpacity");

            bool openSVGEditor = false;

            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(format, SVGAssetEditor.FORMAT_LABEL);
            EditorGUILayout.PropertyField(useGradients, SVGAssetEditor.USE_GRADIENTS_LABEL);
            EditorGUILayout.PropertyField(antialiasing, SVGAssetEditor.ANTIALIASING_LABEL);
            if (!antialiasing.hasMultipleDifferentValues && !antialiasing.boolValue)
                EditorGUILayout.LabelField(" ", SVGAssetEditor.SHARP_EDGES_HINT, EditorStyles.miniLabel);
            if (exactGroupOpacity != null)
                EditorGUILayout.PropertyField(exactGroupOpacity, SVGAssetEditor.EXACT_GROUP_OPACITY_LABEL);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(meshCompression, SVGAssetEditor.MESH_COMPRESSION_LABEL);
            if(((SVGMeshCompression)meshCompression.enumValueIndex) != SVGMeshCompression.Off)
            {
                EditorGUILayout.HelpBox(SVGAssetEditor.MESH_COMPRESSION_HELPBOX_LABEL, MessageType.Warning);
            }
            EditorGUILayout.PropertyField(optimizeMesh, SVGAssetEditor.OPTIMIZE_MESH_LABEL);
            EditorGUILayout.PropertyField(scale, SVGAssetEditor.SCALE_LABEL);
            EditorGUILayout.PropertyField(vpm, SVGAssetEditor.QUALITY_LABEL);

            if(format.enumValueIndex == (int)SVGAssetFormat.Opaque)
            {
                EditorGUILayout.PropertyField(depthOffset, SVGAssetEditor.DEPTH_OFFSET_LABEL);
                EditorGUILayout.PropertyField(compressDepth, SVGAssetEditor.COMPRESS_DEPTH_LABEL);
            }

            SVGPivotGUI.Draw(customPivotPoint, pivotPoint);
            if(showSVGEditorButton)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if(GUILayout.Button(spriteEditorAvailable && serializedObject.targetObject is SVGFileImporter ? "Sprite Editor" : "SVG Editor", EditorStyles.miniButton, GUILayout.Width(80f)))
                {
                    openSVGEditor = true;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.PropertyField(generateCollider, SVGAssetEditor.GENERATE_COLLIDER_LABEL);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Normals & Tangents", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(generateNormals, SVGAssetEditor.GENERATE_NORMALS_LABEL);
            // Tangents need normals; the import ignores them without, so the setting is only disabled.
            using(new EditorGUI.DisabledScope(!generateNormals.boolValue && !generateNormals.hasMultipleDifferentValues))
            {
                EditorGUILayout.PropertyField(generateTangents, SVGAssetEditor.GENERATE_TANGENTS_LABEL);
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("SVG Document", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(keepSVGFile, SVGAssetEditor.KEEP_SVG_FILE_LABEL);
            EditorGUILayout.PropertyField(useLayers, SVGAssetEditor.USE_LAYERS_LABEL);
            EditorGUILayout.PropertyField(ignoreSVGCanvas, SVGAssetEditor.IGNORE_SVG_CANVAS_LABEL);
            EditorGUILayout.Space();

            return openSVGEditor;
        }

        public static void DrawErrors(SVGError[] errors, bool exactGroupOpacity = false)
        {
            if(errors == null)
                return;

            for(int i = 0; i < errors.Length; i++)
            {
                switch(errors[i])
                {
                    case SVGError.CorruptedFile:
                        EditorGUILayout.HelpBox("SVG file is corrupted", MessageType.Error);
                        break;
                    case SVGError.Syntax:
                        EditorGUILayout.HelpBox("SVG syntax is invalid", MessageType.Error);
                        break;
                    case SVGError.ClipPath:
                        EditorGUILayout.HelpBox("Clip paths are experimental", MessageType.Warning);
                        break;
                    case SVGError.Mask:
                        EditorGUILayout.HelpBox("Masks are not supported, except opaque shapes with the Native import engine. Unsupported masks are ignored.", MessageType.Warning);
                        break;
                    case SVGError.Symbol:
                        EditorGUILayout.HelpBox("Re-import for working symbols", MessageType.Warning);
                        break;
                    case SVGError.Image:
                        EditorGUILayout.HelpBox("Images are not supported and are left out", MessageType.Warning);
                        break;
                    case SVGError.Filter:
                        EditorGUILayout.HelpBox("Filters, such as blurs and drop shadows, are not supported and are ignored", MessageType.Warning);
                        break;
                    case SVGError.Pattern:
                        EditorGUILayout.HelpBox("Pattern fills and strokes are not supported and are left out", MessageType.Warning);
                        break;
                    case SVGError.BlendMode:
                        EditorGUILayout.HelpBox("Blend modes are not supported; shapes are blended normally", MessageType.Warning);
                        break;
                    case SVGError.GroupOpacity:
                        EditorGUILayout.HelpBox(exactGroupOpacity
                            ? "Semi-transparent shapes in semi-transparent groups still show through each other"
                            : "Shapes in semi-transparent groups show through each other. Exact Group Opacity fixes this", MessageType.Info);
                        break;
                    case SVGError.TextSkipped:
                        EditorGUILayout.HelpBox("Text was left out", MessageType.Warning);
                        break;
                    case SVGError.Unknown:
                        EditorGUILayout.HelpBox("Unknow error occurred", MessageType.Error);
                        break;
                }
            }
        }

        public static void DrawReportIssueButton(string assetName)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if(GUILayout.Button(new GUIContent("Report Import Issue", "Opens a new GitHub issue. Please attach the SVG file.")))
            {
                Application.OpenURL("https://github.com/jaroslavstehlik/svgimporter/issues/new?title=" +
                    System.Uri.EscapeDataString("Wrong SVG file import: " + assetName));
            }
            GUILayout.EndHorizontal();
        }
    }
}
