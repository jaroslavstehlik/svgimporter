// Phase 1 checks for the native import core (DESIGN.md, section 13):
// the library loads, its ABI matches, errors come back as errors, and its speed compared with
// the legacy C# importer on the package samples.
//
//   Unity -batchmode -projectPath . -executeMethod NativeCoreTests.Run -out <dir>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using SVGImporter;
using SVGImporter.Native;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class NativeCoreTests
{
    public static void Run()
    {
        string output = Arg("-out") ?? "Temp/NativeCoreTests";
        Directory.CreateDirectory(output);
        var results = new List<string>();
        bool passed = true;
        void Check(string name, bool condition, string detail)
        {
            passed &= condition;
            results.Add((condition ? "PASS " : "FAIL ") + name + ": " + detail);
        }

        try
        {
            Check("supported", SVGNativeCore.isSupported, "platform " + Application.platform);
            Check("abi", SVGNativeCore.abiVersion == SVGNativeCore.expectedAbiVersion,
                "library " + SVGNativeCore.abiVersion + ", expected " + SVGNativeCore.expectedAbiVersion);

            var invalid = SVGNativeCore.Import(System.Text.Encoding.UTF8.GetBytes("<svg width=\"0\" height=\"0\"/>"));
            Check("parse error", invalid.status == SVGNativeCore.Status.ParseError && invalid.errorMessage.Length > 0,
                invalid.status + ": " + invalid.errorMessage);
            var garbage = SVGNativeCore.Import(new byte[] { 0xff, 0x00, 0x13, 0x37 });
            Check("garbage input", garbage.status == SVGNativeCore.Status.ParseError, garbage.status + ": " + garbage.errorMessage);
            var empty = SVGNativeCore.Import(new byte[0]);
            Check("empty input", empty.status == SVGNativeCore.Status.ParseError, empty.status + ": " + empty.errorMessage);

            string samples = Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SVGAsset).Assembly).resolvedPath, "Samples~");
            var files = Directory.GetFiles(samples, "*.svg", SearchOption.AllDirectories).OrderBy(f => f).ToArray();
            var data = files.Select(File.ReadAllBytes).ToArray();
            var texts = files.Select(File.ReadAllText).ToArray();

            // Native: core import plus a Unity Mesh, so it includes the cost of reaching Unity.
            int nativeLayers = 0, nativeTriangles = 0, nativeFailures = 0;
            SVGNativeCore.Import(data[0]); // Load the library and fonts before timing.
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < data.Length; i++)
            {
                var r = SVGNativeCore.Import(data[i]);
                if (r.status != SVGNativeCore.Status.Ok) { nativeFailures++; continue; }
                nativeLayers += r.layers.Length;
                nativeTriangles += r.indices.Length / 3;
                var mesh = ToMesh(r);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            double nativeMs = watch.Elapsed.TotalMilliseconds;
            Check("native samples", nativeFailures == 0,
                files.Length + " files, " + nativeLayers + " layers, " + nativeTriangles + " triangles, " + nativeMs.ToString("0.0") + " ms");

            // Legacy: the 2.x C# importer, building an SVG Asset with its default settings.
            var import = typeof(SVGAsset).GetMethod("_editor_Import", BindingFlags.NonPublic | BindingFlags.Instance);
            var created = new List<UnityEngine.Object>();
            Action<string, UnityEngine.Object> add = (id, obj) => created.Add(obj);
            LegacyImport(import, texts[0], add, created);
            watch.Restart();
            for (int i = 0; i < texts.Length; i++)
                LegacyImport(import, texts[i], add, created);
            double legacyMs = watch.Elapsed.TotalMilliseconds;
            results.Add("INFO legacy samples: " + files.Length + " files, " + legacyMs.ToString("0.0") + " ms");
            results.Add("INFO speed: native is " + (legacyMs / nativeMs).ToString("0.0") + "x faster on the samples");

            // The largest sample on its own.
            int tiger = Array.FindIndex(files, f => f.EndsWith("Basic Examples" + Path.DirectorySeparatorChar + "tiger.svg"));
            if (tiger >= 0)
            {
                watch.Restart();
                for (int n = 0; n < 10; n++) UnityEngine.Object.DestroyImmediate(ToMesh(SVGNativeCore.Import(data[tiger])));
                double nativeTiger = watch.Elapsed.TotalMilliseconds / 10;
                watch.Restart();
                for (int n = 0; n < 10; n++) LegacyImport(import, texts[tiger], add, created);
                double legacyTiger = watch.Elapsed.TotalMilliseconds / 10;
                results.Add("INFO tiger.svg: native " + nativeTiger.ToString("0.00") + " ms, legacy " + legacyTiger.ToString("0.00") + " ms");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Check("exception", false, e.ToString());
        }

        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(Path.Combine(output, "native-core-" + Application.unityVersion + ".txt"), results);
        foreach (string line in results) Debug.Log("[NativeCoreTests] " + line);
        EditorApplication.Exit(passed ? 0 : 1);
    }

    static void LegacyImport(MethodInfo import, string svg, Action<string, UnityEngine.Object> add, List<UnityEngine.Object> created)
    {
        var asset = ScriptableObject.CreateInstance<SVGAsset>();
        import.Invoke(asset, new object[] { svg, add, SVGImportEngine.Legacy });
        foreach (var obj in created) UnityEngine.Object.DestroyImmediate(obj);
        created.Clear();
        UnityEngine.Object.DestroyImmediate(asset);
    }

    static Mesh ToMesh(SVGNativeCore.Result r)
    {
        var vertices = new Vector3[r.vertices.Length];
        for (int i = 0; i < vertices.Length; i++) vertices[i] = new Vector3(r.vertices[i].x * 0.01f, -r.vertices[i].y * 0.01f, 0f);
        var indices = new int[r.indices.Length];
        foreach (var layer in r.layers)
            for (int i = 0; i < layer.indexCount; i++)
                indices[layer.indexOffset + i] = r.indices[layer.indexOffset + i] + layer.vertexOffset;
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        return mesh;
    }

    // Layer parity between the engines: for each SVG in Assets/Samples, the layer count and names
    // that modifiers rely on must match.
    //   Unity -batchmode -projectPath . -executeMethod NativeCoreTests.Parity -out <dir>
    public static void Parity()
    {
        string output = Arg("-out") ?? "Temp/NativeCoreTests";
        Directory.CreateDirectory(output);
        var lines = new List<string>();
        int mismatches = 0, files = 0;
        long legacyVertices = 0, nativeVertices = 0;
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var import = typeof(SVGAsset).GetMethod("_editor_Import", flags);
        var created = new List<UnityEngine.Object>();
        Action<string, UnityEngine.Object> add = (id, obj) => created.Add(obj);
        {
            foreach (string path in Directory.GetFiles("Assets/Samples", "*.svg", SearchOption.AllDirectories).OrderBy(f => f))
            {
                files++;
                string text = File.ReadAllText(path);
                List<Rect> legacyBounds, nativeBounds;
                var legacy = Layers(import, text, SVGImportEngine.Legacy, add, created, out legacyBounds);
                var native = Layers(import, text, SVGImportEngine.Native, add, created, out nativeBounds);
                legacyVertices += lastVertices[SVGImportEngine.Legacy];
                nativeVertices += lastVertices[SVGImportEngine.Native];
                bool countMatches = legacy.Count == native.Count;
                bool namesMatch = legacy.Zip(native, SameName).All(same => same);
                if (!countMatches || !namesMatch)
                {
                    mismatches++;
                    lines.Add("MISMATCH " + path + ": legacy " + legacy.Count + " layers, native " + native.Count
                        + "; first difference: " + FirstDifference(legacy, native));
                    // Layers only one engine produced, matched by bounds.
                    foreach (string extra in Unmatched(nativeBounds, legacyBounds, native)) lines.Add("  native only: " + extra);
                    foreach (string extra in Unmatched(legacyBounds, nativeBounds, legacy)) lines.Add("  legacy only: " + extra);
                }
            }
        }
        lines.Insert(0, files + " files, " + mismatches + " with different layers; vertices: legacy " + legacyVertices + ", native " + nativeVertices);
        File.WriteAllLines(Path.Combine(output, "parity.txt"), lines);
        foreach (string line in lines.Take(40)) Debug.Log("[NativeCoreTests] " + line);
        EditorApplication.Exit(0);
    }

    static readonly Dictionary<SVGImportEngine, long> lastVertices = new Dictionary<SVGImportEngine, long>();

    static List<string> Layers(MethodInfo import, string svg, SVGImportEngine engine, Action<string, UnityEngine.Object> add, List<UnityEngine.Object> created, out List<Rect> bounds)
    {
        var asset = ScriptableObject.CreateInstance<SVGAsset>();
        typeof(SVGAsset).GetField("_useLayers", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asset, true);
        import.Invoke(asset, new object[] { svg, add, engine });
        var names = asset.layers == null ? new List<string>() : asset.layers.Select(l => l.name ?? "").ToList();
        lastVertices[engine] = asset.layers == null ? 0 : asset.layers.Sum(l => l.shapes.Sum(sh => (long)sh.vertexCount));
        bounds = asset.layers == null ? new List<Rect>() : asset.layers.Select(LayerBounds).ToList();
        foreach (var obj in created) UnityEngine.Object.DestroyImmediate(obj);
        created.Clear();
        UnityEngine.Object.DestroyImmediate(asset);
        return names;
    }

    // Without an id, legacy names a layer after its element ("Circle Element"); native output is all paths.
    static bool SameName(string legacy, string native)
    {
        return legacy == native || (native == "Path Element" && legacy.EndsWith(" Element"));
    }

    static Rect LayerBounds(SVGLayer layer)
    {
        if (layer.shapes == null || layer.shapes.Length == 0) return new Rect();
        Rect r = layer.shapes[0].bounds;
        foreach (SVGShape shape in layer.shapes)
            r = Rect.MinMaxRect(Mathf.Min(r.xMin, shape.bounds.xMin), Mathf.Min(r.yMin, shape.bounds.yMin),
                Mathf.Max(r.xMax, shape.bounds.xMax), Mathf.Max(r.yMax, shape.bounds.yMax));
        return r;
    }

    static IEnumerable<string> Unmatched(List<Rect> a, List<Rect> b, List<string> names)
    {
        var used = new bool[b.Count];
        for (int i = 0; i < a.Count; i++)
        {
            float tolerance = Mathf.Max(0.01f, 0.05f * Mathf.Max(a[i].width, a[i].height));
            int j = Enumerable.Range(0, b.Count).FirstOrDefault(k => !used[k]
                && Vector2.Distance(a[i].min, b[k].min) < tolerance && Vector2.Distance(a[i].max, b[k].max) < tolerance) ;
            if (b.Count > 0 && !used[j] && Vector2.Distance(a[i].min, b[j].min) < tolerance && Vector2.Distance(a[i].max, b[j].max) < tolerance)
                used[j] = true;
            else
                yield return "layer " + i + " '" + names[i] + "' bounds " + a[i];
        }
    }

    static string FirstDifference(List<string> a, List<string> b)
    {
        for (int i = 0; i < Math.Max(a.Count, b.Count); i++)
        {
            string x = i < a.Count ? a[i] : "(none)", y = i < b.Count ? b[i] : "(none)";
            if (!SameName(x, y)) return "layer " + i + ": legacy '" + x + "', native '" + y + "'";
        }
        return "none";
    }

    static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
