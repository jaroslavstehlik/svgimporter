// Checks for the native import core (DESIGN.md, section 13): the library loads, its ABI
// matches, errors come back as errors, and it imports every package sample (with timings).
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

            // The largest sample on its own.
            int tiger = Array.FindIndex(files, f => f.EndsWith("Basic Examples" + Path.DirectorySeparatorChar + "tiger.svg"));
            if (tiger >= 0)
            {
                watch.Restart();
                for (int n = 0; n < 10; n++) UnityEngine.Object.DestroyImmediate(ToMesh(SVGNativeCore.Import(data[tiger])));
                double nativeTiger = watch.Elapsed.TotalMilliseconds / 10;
                results.Add("INFO tiger.svg: " + nativeTiger.ToString("0.00") + " ms");
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

    static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
