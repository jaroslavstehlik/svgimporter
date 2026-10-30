// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace SVGImporter
{
    [CustomEditor(typeof(SVGFileImporter))]
    [CanEditMultipleObjects]
    internal class SVGFileImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool openSpriteEditor = SVGImportSettingsGUI.Draw(serializedObject, !serializedObject.isEditingMultipleObjects);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
            ApplyRevertGUI();

            if (openSpriteEditor)
            {
                Selection.activeObject = assetTarget;
                EditorApplication.ExecuteMenuItem("Window/2D/Sprite Editor");
                GUIUtility.ExitGUI();
            }
        }

        [MenuItem("CONTEXT/SVGFileImporter/Report Import Issue...")]
        static void ReportImportIssue(MenuCommand command)
        {
            string name = System.IO.Path.GetFileName(((AssetImporter)command.context).assetPath);
            Application.OpenURL("https://github.com/jaroslavstehlik/svgimporter/issues/new?title=" +
                System.Uri.EscapeDataString("Wrong SVG file import: " + name) +
                "&body=" + System.Uri.EscapeDataString("Please attach the SVG file."));
        }
    }
}
