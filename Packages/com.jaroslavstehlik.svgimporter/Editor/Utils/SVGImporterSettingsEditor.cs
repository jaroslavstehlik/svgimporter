// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEditor;

namespace SVGImporter {

    /// <summary>
    /// SVG Importer 2.0 kept its settings in an SVGImporterSettings asset; they now live in Project
    /// Settings. Such an asset only says so.
    /// </summary>
    [CustomEditor(typeof(SVGImporterSettings))]
    public class SVGImporterSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("SVG Importer's settings are in Edit > Project Settings > SVG Importer. This asset isn't used any more and can be deleted.", MessageType.Info);
            if (UnityEngine.GUILayout.Button("Open Project Settings"))
                SettingsService.OpenProjectSettings(SVGImporterSettingsProvider.settingsPath);
        }
    }
}
