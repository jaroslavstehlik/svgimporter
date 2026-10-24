// Sets the platform settings of the native libraries in the package, so that each library is
// used only on its own platform. Run after adding or rebuilding a library:
//   Unity -batchmode -quit -projectPath . -executeMethod NativePluginSettings.Apply

using UnityEditor;
using UnityEngine;

public static class NativePluginSettings
{
    const string Plugins = "Packages/com.jaroslavstehlik.svgimporter/Runtime/Plugins/";

    public static void Apply()
    {
        // macOS: universal library (arm64 + x86_64) for the Editor and standalone players.
        var mac = (PluginImporter)AssetImporter.GetAtPath(Plugins + "macOS/libsvgimporter_core.dylib");
        mac.SetCompatibleWithAnyPlatform(false);
        mac.SetCompatibleWithEditor(true);
        mac.SetEditorData("OS", "OSX");
        mac.SetEditorData("CPU", "AnyCPU");
        mac.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);
        mac.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "AnyCPU");
        mac.SaveAndReimport();
        Debug.Log("[NativePluginSettings] macOS: editor " + mac.GetCompatibleWithEditor() + ", standalone " + mac.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX));
    }
}
