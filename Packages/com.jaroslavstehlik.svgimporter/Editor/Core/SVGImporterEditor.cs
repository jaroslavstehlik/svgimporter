// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEditor;

namespace SVGImporter
{
    public class SVGImporterEditor
    {
        [MenuItem("Window/SVG Importer/Settings")]
        static public void ShowSettings () {
            SettingsService.OpenProjectSettings(SVGImporterSettingsProvider.settingsPath);
        }

        [System.Obsolete("Use SVGImporterProjectSettings.instance.")]
        public static SVGImporterSettings settings
        {
            get { return SVGImporterSettings.Get; }
        }
    }
}
