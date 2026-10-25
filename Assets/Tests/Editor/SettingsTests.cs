// Project settings: stored in ProjectSettings, read in one place, reloaded after outside changes,
// migrated from the 2.0 asset, used by the importer, and given to players through the build.
//
//   Unity -batchmode -projectPath . -executeMethod SettingsTests.Run -out <dir>
//
// Restores the project's settings file afterwards.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SVGImporter;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SettingsTests
{
    const string FilePath = "ProjectSettings/SVGImporterSettings.asset";
    const string OldAssetFolder = "Assets/SettingsTestsOld/Resources";

    public static void Run()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/SettingsTests";
        Directory.CreateDirectory(output);
        var results = new List<string>();
        bool passed = true;
        void Check(string name, bool condition, string detail)
        {
            passed &= condition;
            results.Add((condition ? "PASS " : "FAIL ") + name + ": " + detail);
        }

        byte[] original = File.Exists(FilePath) ? File.ReadAllBytes(FilePath) : null;
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        Action reload = () => typeof(SVGImporterProjectSettings).GetMethod("ReloadIfChanged", flags).Invoke(null, null);
        Action unload = () => typeof(SVGImporterProjectSettings).GetMethod("Unload", flags).Invoke(null, null);
        try
        {
            // Changing a setting writes the text file in ProjectSettings.
            SVGImporterProjectSettings settings = SVGImporterProjectSettings.instance;
            settings.antialiasingWidth = 3.5f;
            string text = File.ReadAllText(FilePath);
            Check("saved to ProjectSettings", text.Contains("_antialiasingWidth: 3.5"), FilePath);
            Check("runtime settings follow", Mathf.Approximately(SVGImporterSettings.Get.defaultAntialiasingWidth, 3.5f),
                "SVGImporterSettings.Get.defaultAntialiasingWidth = " + SVGImporterSettings.Get.defaultAntialiasingWidth);

            // A change made outside Unity (a version control update) is picked up.
            System.Threading.Thread.Sleep(1100); // File times can have a resolution of one second.
            File.WriteAllText(FilePath, text.Replace("_antialiasingWidth: 3.5", "_antialiasingWidth: 1.25"));
            reload();
            Check("outside change reloaded", Mathf.Approximately(SVGImporterProjectSettings.instance.antialiasingWidth, 1.25f),
                "antialiasingWidth = " + SVGImporterProjectSettings.instance.antialiasingWidth);

            // 2.0 projects: settings in an asset in a Resources folder are migrated once.
            Directory.CreateDirectory(OldAssetFolder);
            var old = ScriptableObject.CreateInstance<SVGImporterSettings>();
#pragma warning disable 618
            old.importEngine = SVGImportEngine.Native;
            old.importNewSVGFiles = false;
#pragma warning restore 618
            old.defaultScale = 0.5f;
            old.defaultAntialiasingWidth = 4f;
            AssetDatabase.CreateAsset(old, OldAssetFolder + "/svgImporterSettings.asset");
            File.Delete(FilePath);
            unload();
            settings = SVGImporterProjectSettings.instance;
            Check("migrated from the 2.0 asset", !settings.importNewSVGFiles
                && Mathf.Approximately(settings.defaults.scale, 0.5f) && Mathf.Approximately(settings.antialiasingWidth, 4f) && File.Exists(FilePath),
                "import new files " + settings.importNewSVGFiles + ", scale " + settings.defaults.scale + ", width " + settings.antialiasingWidth);
            AssetDatabase.DeleteAsset("Assets/SettingsTestsOld");

            // Builds preload a copy of the runtime settings, removed after the build.
            var build = Activator.CreateInstance(typeof(SVGImporterProjectSettings).Assembly.GetType("SVGImporter.SVGImporterBuildSettings"), true);
            build.GetType().GetMethod("OnPreprocessBuild").Invoke(build, new object[] { null });
            var preloaded = PlayerSettings.GetPreloadedAssets().OfType<SVGImporterSettings>().FirstOrDefault();
            Check("build preloads the runtime settings", preloaded != null && Mathf.Approximately(preloaded.defaultAntialiasingWidth, 4f),
                preloaded == null ? "none" : "width " + preloaded.defaultAntialiasingWidth);
            build.GetType().GetMethod("OnPostprocessBuild").Invoke(build, new object[] { null });
            Check("build settings removed afterwards", !PlayerSettings.GetPreloadedAssets().OfType<SVGImporterSettings>().Any()
                && !File.Exists("Assets/SVGImporterBuildSettings.asset"), "");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Check("exception", false, e.ToString());
        }
        finally
        {
            if (original != null) File.WriteAllBytes(FilePath, original); else File.Delete(FilePath);
            unload();
        }

        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(Path.Combine(output, "settings.txt"), results);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
