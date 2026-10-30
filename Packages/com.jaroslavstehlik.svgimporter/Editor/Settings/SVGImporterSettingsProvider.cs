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

        static class Styles
        {
            public static readonly GUIContent import = EditorGUIUtility.TrTextContent("Import");
            public static readonly GUIContent importNewSVGFiles = EditorGUIUtility.TrTextContent("Import New SVG Files",
                "Import SVG files added to the project with SVG Importer. Turn this off when the project uses Unity's own SVG import, for example for UI Toolkit. Individual files can still be switched in the Inspector.");
            public static readonly GUIContent defaults = EditorGUIUtility.TrTextContent("Defaults for New SVG Files");
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
            EditorGUILayout.LabelField(Styles.import, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(s_Settings.FindProperty("_importNewSVGFiles"), Styles.importNewSVGFiles);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(SVGImportSettingsGUI.Styles.rendering, EditorStyles.boldLabel);
            // Stored in half pixels, the unit the shaders and 1.x projects use; shown in pixels.
            SerializedProperty width = s_Settings.FindProperty("_antialiasingWidth");
            EditorGUI.BeginChangeCheck();
            float pixels = EditorGUILayout.FloatField(SVGImportSettingsGUI.Styles.antialiasingWidth, width.floatValue * 0.5f);
            if (EditorGUI.EndChangeCheck())
                width.floatValue = Mathf.Max(0f, pixels) * 2f;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.defaults, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                // The defaults are named as SVGImportOptions names them.
                SerializedProperty defaults = s_Settings.FindProperty("_defaults");
                SVGImportSettingsGUI.Draw(name => defaults.FindPropertyRelative(name == "vpm" ? "verticesPerMeter" : name == "antialiasing" ? "softEdges" : name), false);
            }

            if (EditorGUI.EndChangeCheck())
            {
                s_Settings.ApplyModifiedPropertiesWithoutUndo();
                settings.Save();
            }
        }
    }
}
