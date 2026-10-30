// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>Edit > Project Settings > SVG Importer.</summary>
    static class SVGImporterSettingsProvider
    {
        internal const string settingsPath = "Project/SVG Importer";

        [SettingsProvider]
        static SettingsProvider Create()
        {
            return new SettingsProvider(settingsPath, SettingsScope.Project)
            {
                label = "SVG Importer",
                guiHandler = _ => Draw(),
                keywords = new HashSet<string>(new[] { "SVG", "Import", "Native", "Antialiasing", "Soft Edges", "MSAA", "Vector" }),
            };
        }

        static SerializedObject s_Settings;

        static void Draw()
        {
            SVGImporterProjectSettings settings = SVGImporterProjectSettings.instance;
            if (s_Settings == null || s_Settings.targetObject != settings)
                s_Settings = new SerializedObject(settings);
            s_Settings.Update();

            EditorGUIUtility.labelWidth = 220f;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Import", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(s_Settings.FindProperty("_importNewSVGFiles"), new GUIContent("Import New SVG Files",
                "Import SVG files added to the project with SVG Importer. Turn this off when the project uses Unity's own SVG import, for example for UI Toolkit. Individual files can still be switched in the Inspector."));
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            // Stored in half pixels, the unit the shaders and 1.x projects use; shown in pixels.
            SerializedProperty width = s_Settings.FindProperty("_antialiasingWidth");
            EditorGUI.BeginChangeCheck();
            float pixels = EditorGUILayout.FloatField(SVGAssetEditor.ANTIALIASING_WIDTH_LABEL, width.floatValue * 0.5f);
            if (EditorGUI.EndChangeCheck())
                width.floatValue = Mathf.Max(0f, pixels) * 2f;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Defaults for New SVG Files", EditorStyles.boldLabel);
            SerializedProperty d = s_Settings.FindProperty("_defaults");
            EditorGUILayout.PropertyField(d.FindPropertyRelative("format"), SVGAssetEditor.FORMAT_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("useGradients"), SVGAssetEditor.USE_GRADIENTS_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("antialiasing"), SVGAssetEditor.ANTIALIASING_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("exactGroupOpacity"), SVGAssetEditor.EXACT_GROUP_OPACITY_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("meshCompression"), SVGAssetEditor.MESH_COMPRESSION_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("optimizeMesh"), SVGAssetEditor.OPTIMIZE_MESH_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("scale"), SVGAssetEditor.SCALE_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("verticesPerMeter"), SVGAssetEditor.QUALITY_LABEL);
            if (d.FindPropertyRelative("format").enumValueIndex == (int)SVGAssetFormat.Opaque)
            {
                EditorGUILayout.PropertyField(d.FindPropertyRelative("depthOffset"), SVGAssetEditor.DEPTH_OFFSET_LABEL);
                EditorGUILayout.PropertyField(d.FindPropertyRelative("compressDepth"), SVGAssetEditor.COMPRESS_DEPTH_LABEL);
            }
            SVGPivotGUI.Draw(d.FindPropertyRelative("customPivotPoint"), d.FindPropertyRelative("pivotPoint"));
            EditorGUILayout.PropertyField(d.FindPropertyRelative("generateCollider"), SVGAssetEditor.GENERATE_COLLIDER_LABEL);
            SerializedProperty normals = d.FindPropertyRelative("generateNormals");
            EditorGUILayout.PropertyField(normals, SVGAssetEditor.GENERATE_NORMALS_LABEL);
            using (new EditorGUI.DisabledScope(!normals.boolValue))
                EditorGUILayout.PropertyField(d.FindPropertyRelative("generateTangents"), SVGAssetEditor.GENERATE_TANGENTS_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("keepSVGFile"), SVGAssetEditor.KEEP_SVG_FILE_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("useLayers"), SVGAssetEditor.USE_LAYERS_LABEL);
            EditorGUILayout.PropertyField(d.FindPropertyRelative("ignoreSVGCanvas"), SVGAssetEditor.IGNORE_SVG_CANVAS_LABEL);

            if (EditorGUI.EndChangeCheck())
            {
                s_Settings.ApplyModifiedPropertiesWithoutUndo();
                settings.Save();
            }
        }
    }
}
