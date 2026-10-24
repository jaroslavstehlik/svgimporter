// Re-saves the package samples in the current Unity version's format.
//
// Run in two passes, so the sample scripts are compiled before their scenes are re-saved:
//   Unity -batchmode -quit -projectPath . -executeMethod SampleMaintenance.ImportSamples
//   Unity -batchmode -quit -projectPath . -executeMethod SampleMaintenance.ReserializeSamples
// Then copy Assets/Samples/SVG Importer/<version>/* back into the package's Samples~ folder.

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public static class SampleMaintenance
{
    const string Package = "com.jaroslavstehlik.svgimporter";

    public static void ImportSamples()
    {
        foreach (var sample in Sample.FindByPackage(Package, null))
        {
            bool imported = sample.Import(Sample.ImportOptions.OverridePreviousImports | Sample.ImportOptions.HideImportWindow);
            Debug.Log("[SampleMaintenance] imported " + sample.displayName + ": " + imported + " -> " + sample.importPath);
        }
    }

    public static void ReserializeSamples()
    {
        string[] paths = AssetDatabase.FindAssets("", new[] { "Assets/Samples" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !AssetDatabase.IsValidFolder(p))
            .Distinct()
            .ToArray();
        AssetDatabase.ForceReserializeAssets(paths);
        Debug.Log("[SampleMaintenance] re-serialized " + paths.Length + " assets");

        int legacyComponents = paths.Where(p => p.EndsWith(".unity") || p.EndsWith(".prefab"))
            .Count(p => File.ReadAllText(p).Contains("--- !u!92 "));
        Debug.Log("[SampleMaintenance] files still containing GUILayer: " + legacyComponents);
    }
}
