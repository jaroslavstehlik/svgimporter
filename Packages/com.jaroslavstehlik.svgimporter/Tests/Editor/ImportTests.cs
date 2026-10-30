// The import pipeline (SVGImport): deterministic, no state left between imports, the same for the
// Editor importer and SVGAsset.Load, and failures that don't leak into the next import.
//
//   Unity -batchmode -projectPath . -executeMethod ImportTests.Run -out <dir>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SVGImporter;
using SVGImporter.Rendering;
using UnityEditor;
using UnityEngine;

public static class ImportTests
{
    const string Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"50\">"
        + "<linearGradient id=\"g\"><stop offset=\"0\" stop-color=\"red\"/><stop offset=\"1\" stop-color=\"blue\"/></linearGradient>"
        + "<rect width=\"60\" height=\"50\" fill=\"url(#g)\"/><circle cx=\"75\" cy=\"25\" r=\"20\" fill=\"lime\" stroke=\"black\"/></svg>";

    public static void Run()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/ImportTests";
        Directory.CreateDirectory(output);
        var results = new List<string>();
        bool passed = true;
        void Check(string name, bool condition, string detail)
        {
            passed &= condition;
            results.Add((condition ? "PASS " : "FAIL ") + name + ": " + detail);
        }

        const string path = "Assets/ImportTests.svg";
        try
        {
            SVGImportOptions options = SVGImportOptions.Default;
            options.softEdges = true;
            options.generateCollider = true;

            // Deterministic: the same text and options give the same hash and mesh.
            SVGImportResult a = SVGImport.Import(Svg, options), b = SVGImport.Import(Svg, options);
            Check("deterministic", a.succeeded && a.importHash == b.importHash && a.mesh.vertices.SequenceEqual(b.mesh.vertices)
                && a.mesh.triangles.SequenceEqual(b.mesh.triangles), "hash " + a.importHash.ToString("X16"));
            Check("result complete", a.shaders.Length > 0 && a.gradients != null && a.gradients.Length == 2 && a.colliderShape != null,
                a.shaders.Length + " shaders, " + (a.gradients == null ? 0 : a.gradients.Length) + " gradients, collider " + (a.colliderShape != null));
            options.scale = 0.02f;
            Check("options change the hash", SVGImport.Import(Svg, options).importHash != a.importHash, "");

            // Collider outlines: the union of all shapes, holes kept unless another shape covers them.
            // A 100 x 100 ring with a 60 x 60 hole and a 20 x 20 square in the hole, at the default
            // scale (0.01 units per SVG unit): areas 1, -0.36 and 0.04.
            SVGImportOptions collider = SVGImportOptions.Default;
            collider.generateCollider = true;
            SVGPath[] ring = SVGImport.Import("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\">"
                + "<path fill-rule=\"evenodd\" d=\"M0 0H100V100H0Z M20 20H80V80H20Z\"/><rect x=\"40\" y=\"40\" width=\"20\" height=\"20\" fill=\"red\"/></svg>", collider).colliderShape;
            float[] areas = ring == null ? new float[0] : ring.Select(p => Area(p.points)).OrderBy(a => a).ToArray();
            Check("collider keeps holes", areas.Length == 3 && Mathf.Abs(Mathf.Abs(areas[0]) - 0.36f) < 0.001f && Mathf.Abs(Mathf.Abs(areas[1]) - 0.04f) < 0.001f
                && Mathf.Abs(Mathf.Abs(areas[2]) - 1f) < 0.001f && areas[0] < 0f && areas[2] > 0f, string.Join(", ", areas.Select(a => a.ToString("F3"))));
            SVGPath[] overlap = SVGImport.Import("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"100\">"
                + "<rect width=\"100\" height=\"100\"/><rect x=\"50\" width=\"100\" height=\"100\"/></svg>", collider).colliderShape;
            Check("collider unites overlapping shapes", overlap != null && overlap.Length == 1 && Mathf.Abs(Mathf.Abs(Area(overlap[0].points)) - 1.5f) < 0.001f,
                overlap == null ? "none" : overlap.Length + " paths, area " + Area(overlap[0].points).ToString("F3"));

            // SVG Importer's conicalGradient extension.
            SVGImportOptions layered = SVGImportOptions.Default;
            layered.useLayers = true;
            SVGImportResult conical = SVGImport.Import("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\">"
                + "<conicalGradient id=\"c\" cx=\"50\" cy=\"50\" r=\"50\" gradientUnits=\"userSpaceOnUse\"><stop offset=\"0\" stop-color=\"red\"/><stop offset=\"1\" stop-color=\"blue\"/></conicalGradient>"
                + "<circle cx=\"50\" cy=\"50\" r=\"50\" fill=\"url(#c)\"/></svg>", layered);
            bool isConical = conical.succeeded && conical.layers.SelectMany(l => l.shapes).Any(shape => shape.fill != null && shape.fill.gradientType == SVGGradientType.Conical);
            Check("conicalGradient imports as a conical gradient", isConical, string.Join(" | ", conical.messages));

            // Exact Group Opacity: what a later opaque child covers is removed from the lower one.
            const string group = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><g opacity=\"0.5\">"
                + "<rect width=\"60\" height=\"60\" fill=\"red\"/><rect x=\"40\" y=\"40\" width=\"60\" height=\"60\" fill=\"blue\"/></g></svg>";
            SVGImportOptions exact = SVGImportOptions.Default, perShape = SVGImportOptions.Default;
            exact.exactGroupOpacity = true;
            SVGImportResult exactResult = SVGImport.Import(group, exact), perShapeResult = SVGImport.Import(group, perShape);
            Check("Exact Group Opacity", exactResult.succeeded && perShapeResult.succeeded
                && !exactResult.errors.Contains(SVGError.GroupOpacity) && perShapeResult.errors.Contains(SVGError.GroupOpacity)
                && exactResult.mesh.vertexCount != perShapeResult.mesh.vertexCount,
                exactResult.mesh.vertexCount + " vertices exact, " + perShapeResult.mesh.vertexCount + " per shape");

            // A failed import reports errors and leaves nothing behind for the next one.
            SVGImportResult failed = SVGImport.Import("<svg><rect", SVGImportOptions.Default);
            SVGImportResult after = SVGImport.Import(Svg, SVGImportOptions.Default);
            Check("failure is contained", !failed.succeeded && failed.hasCriticalErrors && failed.mesh == null && after.succeeded,
                string.Join(", ", failed.errors) + "; " + string.Join(" | ", failed.messages));

            // SVGAsset.Load works in edit mode and gives what the Editor importer gives.
            File.WriteAllText(path, Svg);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            SVGAsset imported = AssetDatabase.LoadAssetAtPath<SVGAsset>(path);
            SVGImportOptions importedOptions = imported.importOptions;
            SVGAsset loaded = SVGAsset.Load(Svg, importedOptions);
            Check("Load matches the importer", loaded != null && loaded.sharedMesh.vertexCount == imported.sharedMesh.vertexCount
                && loaded.sharedShaders.SequenceEqual(imported.sharedShaders) && loaded.sharedGradients.Length == imported.sharedGradients.Length,
                (loaded == null ? "null" : loaded.sharedMesh.vertexCount + " vertices") + " / " + imported.sharedMesh.vertexCount + " vertices");
            UnityEngine.Object.DestroyImmediate(loaded);

            // Reimporting an unchanged file gives the same asset: no time stamps.
            long hash = imported.importHash;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Check("reimport is identical", AssetDatabase.LoadAssetAtPath<SVGAsset>(path).importHash == hash, hash.ToString("X16"));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Check("exception", false, e.ToString());
        }
        finally
        {
            AssetDatabase.DeleteAsset(path);
        }

        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(Path.Combine(output, "import.txt"), results);
        EditorApplication.Exit(passed ? 0 : 1);
    }

    // Signed area of a closed polygon.
    static float Area(Vector2[] points)
    {
        float area = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 p = points[i], q = points[(i + 1) % points.Length];
            area += p.x * q.y - q.x * p.y;
        }
        return area * 0.5f;
    }
}
