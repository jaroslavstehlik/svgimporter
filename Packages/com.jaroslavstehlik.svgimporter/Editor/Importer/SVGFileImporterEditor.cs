// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace SVGImporter
{
    [CustomEditor(typeof(SVGFileImporter))]
    [CanEditMultipleObjects]
    public class SVGFileImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool openSVGEditor = SVGImportSettingsGUI.Draw(serializedObject, !serializedObject.isEditingMultipleObjects);

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();

            if(openSVGEditor)
            {
                Selection.activeObject = assetTarget;
                SVGEditorWindow.GetWindow();
            }

            if(!serializedObject.isEditingMultipleObjects && assetTarget != null)
            {
                EditorGUILayout.Space();
                SVGImportSettingsGUI.DrawReportIssueButton(assetTarget.name);
            }
        }
    }
}
