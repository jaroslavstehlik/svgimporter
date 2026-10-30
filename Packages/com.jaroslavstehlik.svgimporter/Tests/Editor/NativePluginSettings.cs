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

        // Windows and Linux, x86_64: the Editor on that OS and standalone players.
        Desktop("Windows/x86_64/svgimporter_core.dll", "Windows", BuildTarget.StandaloneWindows64);
        Desktop("Linux/x86_64/libsvgimporter_core.so", "Linux", BuildTarget.StandaloneLinux64);

        // iOS: a static library, linked into the app (the bindings use __Internal there).
        var ios = Player("iOS/libsvgimporter_core.a", BuildTarget.iOS);
        ios.SetPlatformData(BuildTarget.iOS, "CPU", "ARM64");
        ios.SaveAndReimport();

        // Android: one library per ABI.
        foreach (var abi in new[] { ("arm64-v8a", "ARM64"), ("armeabi-v7a", "ARMv7") })
        {
            var android = Player("Android/" + abi.Item1 + "/libsvgimporter_core.so", BuildTarget.Android);
            android.SetPlatformData(BuildTarget.Android, "CPU", abi.Item2);
            android.SaveAndReimport();
        }
        Debug.Log("[NativePluginSettings] Windows, Linux, iOS and Android set");
    }

    static void Desktop(string path, string os, BuildTarget target)
    {
        var plugin = Player(path, target);
        plugin.SetCompatibleWithEditor(true);
        plugin.SetEditorData("OS", os);
        plugin.SetEditorData("CPU", "x86_64");
        plugin.SetPlatformData(target, "CPU", "x86_64");
        plugin.SaveAndReimport();
    }

    // A library used only by one player platform.
    static PluginImporter Player(string path, BuildTarget target)
    {
        var plugin = (PluginImporter)AssetImporter.GetAtPath(Plugins + path);
        plugin.SetCompatibleWithAnyPlatform(false);
        plugin.SetCompatibleWithEditor(false);
        plugin.SetCompatibleWithPlatform(target, true);
        return plugin;
    }
}
