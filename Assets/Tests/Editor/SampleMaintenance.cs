// Re-saves the package samples in the current Unity version's format.
//
// Run in two passes, so the sample scripts are compiled before their scenes are re-saved:
//   Unity -batchmode -quit -projectPath . -executeMethod SampleMaintenance.ImportSamples
//   Unity -batchmode -quit -projectPath . -executeMethod SampleMaintenance.ReserializeSamples
// Then copy Assets/Samples/SVG Importer/<version>/* back into the package's Samples~ folder.
//
// Converting the samples' SVG Assets to .svg files, checking they render the same:
//   ... -executeMethod SampleMaintenance.RenderSamples -out <before dir>
//   ... -executeMethod SampleMaintenance.ConvertSamples
//   ... -executeMethod SampleMaintenance.RenderSamples -out <after dir>

using System;
using System.IO;
using System.Linq;
using SVGImporter;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
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

    public static void ConvertSamples()
    {
        string[] legacy = SVGAssetConverter.FindLegacySVGAssets().Where(p => p.StartsWith("Assets/Samples/")).ToArray();
        var result = SVGAssetConverter.Convert(legacy);
        Debug.Log("[SampleMaintenance] converted " + result.converted + " of " + legacy.Length + ", skipped " + result.skipped.Count
            + ", references updated: " + result.referencesUpdated + " in " + result.filesUpdated + " files");
        foreach (string skipped in result.skipped) Debug.Log("[SampleMaintenance] skipped " + skipped);
    }

    // Renders every sample scene with its last enabled camera, in edit mode.
    public static void RenderSamples()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).First();
        Directory.CreateDirectory(output);
        int errors = 0;
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception) errors++;
        };

        foreach (string scenePath in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Samples" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c => c.enabled && c.gameObject.activeInHierarchy).OrderBy(c => c.depth).LastOrDefault();
            if (camera == null) continue;

            var target = new RenderTexture(960, 540, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            camera.Render();
            var pixels = new Texture2D(960, 540, TextureFormat.RGBA32, false);
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            RenderTexture.active = null;
            camera.targetTexture = null;
            File.WriteAllBytes(Path.Combine(output, Path.GetFileNameWithoutExtension(scenePath) + ".png"), pixels.EncodeToPNG());
        }
        Debug.Log("[SampleMaintenance] errors while rendering: " + errors);
    }
}
