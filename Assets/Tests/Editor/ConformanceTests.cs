// Conformance test (DESIGN.md, section 10): renders each SVG of the resvg test suite through the
// importer and its shaders, and compares it with the suite's reference PNG.
//
//   Unity -batchmode -projectPath . -executeMethod ConformanceTests.Run
//         -corpus <resvg>/crates/resvg/tests/tests -out <dir> [-perShapeGroupOpacity]
//         [-filter <path substring>] [-saveRenders] [-skip <file listing paths to skip>]
//
// An import that crashes Unity (the legacy parser overflows the stack on some files) can't be
// caught; current-<engine>.txt names the file being tested, so it can be added to the skip list.
//
// Writes conformance-<engine>.txt (pass rate per folder) and failures-<engine>.txt to <dir>.
// A file passes when at most 1% of its pixels differ by more than 32 in a channel, as in
// svgimporter-cli compare. Renders use the Built-in Render Pipeline and 8x MSAA instead of the
// importer's antialiasing, so both engines get the same edges, in Gamma color space, where
// edges blend in sRGB as in the references (the project's settings are restored afterwards).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SVGImporter;
using SVGImporter.Native;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public static class ConformanceTests
{
    static SVGImportEngine engine;

    public static void Run()
    {
        string corpus = Arg("-corpus") ?? throw new ArgumentException("-corpus <folder> is required");
        string output = Arg("-out") ?? "Temp/ConformanceTests";
        string filter = Arg("-filter");
        bool saveRenders = Environment.GetCommandLineArgs().Contains("-saveRenders");
        engine = SVGImportEngine.Native;
        string engineName = engine.ToString();
        Directory.CreateDirectory(output);
        string renders = Path.Combine(output, "renders-" + engineName);
        if (saveRenders) Directory.CreateDirectory(renders);

        ColorSpace originalColorSpace = PlayerSettings.colorSpace;
        int originalAntiAliasing = QualitySettings.antiAliasing;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        // Unity 6.0 only multisamples the camera's target with MSAA enabled in the quality settings.
        QualitySettings.antiAliasing = 8;
        try
        {
            Render(corpus, output, filter, saveRenders, engineName, renders);
        }
        finally
        {
            PlayerSettings.colorSpace = originalColorSpace;
            QualitySettings.antiAliasing = originalAntiAliasing;
        }
        EditorApplication.Exit(0);
    }

    static void Render(string corpus, string output, string filter, bool saveRenders, string engineName, string renders)
    {
        string skipList = Arg("-skip");
        string[] skip = skipList != null ? File.ReadAllLines(skipList).Where(l => l.Trim().Length > 0).Select(l => l.Trim()).ToArray() : new string[0];
        GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        camera.enabled = false;

        var created = new List<UnityEngine.Object>();
        var categories = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
        var failures = new List<string>();

        string[] files = Directory.GetFiles(corpus, "*.svg", SearchOption.AllDirectories)
            .Where(f => File.Exists(Path.ChangeExtension(f, ".png")))
            .Where(f => filter == null || f.Contains(filter))
            .Where(f => !skip.Any(f.EndsWith))
            .OrderBy(f => f, StringComparer.Ordinal).ToArray();
        foreach (string file in files)
        {
            string relative = file.Substring(corpus.Length).TrimStart('/', '\\');
            string category = Path.GetDirectoryName(relative).Replace('\\', '/');
            if (!categories.TryGetValue(category, out int[] counts)) categories[category] = counts = new int[2];
            counts[0]++;

            File.WriteAllText(Path.Combine(output, "current-" + engineName + ".txt"), relative);
            string reason;
            try
            {
                reason = Check(file, camera, created, saveRenders ? Path.Combine(renders, relative.Replace('/', '_').Replace('\\', '_')) : null);
            }
            catch (Exception e)
            {
                reason = "exception: " + e.GetBaseException().Message;
            }
            finally
            {
                foreach (var obj in created) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
                created.Clear();
            }
            if (reason == null) counts[1]++;
            else failures.Add(relative + "  " + reason);
        }

        var lines = new List<string> { "Engine: " + engineName + ", Unity " + Application.unityVersion };
        int total = 0, passed = 0;
        foreach (var pair in categories)
        {
            total += pair.Value[0];
            passed += pair.Value[1];
            lines.Add(string.Format("{0,-45} {1,4} / {2,-4} {3,5:0.0}%", pair.Key, pair.Value[1], pair.Value[0], pair.Value[1] * 100.0 / pair.Value[0]));
        }
        lines.Add(string.Format("{0,-45} {1,4} / {2,-4} {3,5:0.0}%", "total", passed, total, passed * 100.0 / Math.Max(total, 1)));
        File.WriteAllLines(Path.Combine(output, "conformance-" + engineName + ".txt"), lines);
        File.WriteAllLines(Path.Combine(output, "failures-" + engineName + ".txt"), failures);
        Debug.Log("[ConformanceTests] " + lines[0] + ": " + passed + " / " + total);
    }

    /// Returns null if the file renders like its reference, otherwise the reason.
    static string Check(string file, Camera camera, List<UnityEngine.Object> created, string savePath)
    {
        var reference = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        created.Add(reference);
        if (!reference.LoadImage(File.ReadAllBytes(Path.ChangeExtension(file, ".png")))) return "unreadable reference";

        // The document size, which the reference is scaled to: the native core reads it as
        // browsers do, whichever engine imports the file.
        byte[] data = File.ReadAllBytes(file);
        var size = SVGNativeCore.Import(data, 1f, false);
        if (size.status != SVGNativeCore.Status.Ok) return "not an SVG document (" + size.status + ")";

        SVGImportOptions options = SVGImportOptions.Default;
        options.engine = engine;
        options.ignoreSVGCanvas = false;
        options.antialiasing = false;
        options.format = SVGAssetFormat.Transparent;
        options.exactGroupOpacity = !Environment.GetCommandLineArgs().Contains("-perShapeGroupOpacity");
        // Quality 10,000 vertices per meter: curves within 0.1 user units, so the test measures
        // features rather than the default tessellation density.
        options.verticesPerMeter = 10000f;
        SVGAsset asset = SVGAsset.Load(Encoding.UTF8.GetString(data), options);
        if (asset == null || asset.sharedMesh == null) return "no mesh";
        created.Add(asset);

        int width = reference.width, height = reference.height;
        float scale = asset.scale;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
        created.Add(target);
        var go = new GameObject("svg");
        created.Add(go);
        var renderer = go.AddComponent<SVGRenderer>();
        renderer.vectorGraphics = asset;

        camera.orthographicSize = size.size.y * scale * 0.5f;
        camera.aspect = size.size.x / size.size.y;
        camera.targetTexture = target;
        // The first render fills the gradient atlas (see SVGRenderTests).
        camera.Render();
        camera.Render();
        camera.targetTexture = null;

        var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        created.Add(pixels);
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        RenderTexture.active = null;
        if (savePath != null) File.WriteAllBytes(Path.ChangeExtension(savePath, ".png"), pixels.EncodeToPNG());

        Color32[] actual = pixels.GetPixels32(), expected = reference.GetPixels32();
        int differing = 0;
        for (int i = 0; i < actual.Length; i++)
        {
            // The reference composited over the white background.
            Color32 e = expected[i];
            int r = (e.r * e.a + 255 * (255 - e.a)) / 255, g = (e.g * e.a + 255 * (255 - e.a)) / 255, b = (e.b * e.a + 255 * (255 - e.a)) / 255;
            int d = Math.Max(Math.Abs(actual[i].r - r), Math.Max(Math.Abs(actual[i].g - g), Math.Abs(actual[i].b - b)));
            if (d > 32) differing++;
        }
        float fraction = differing / (float)actual.Length;
        return fraction <= 0.01f ? null : (fraction * 100f).ToString("0.0") + "%";
    }

    static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
