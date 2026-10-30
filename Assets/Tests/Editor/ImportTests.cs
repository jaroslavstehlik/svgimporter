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
            options.antialiasing = true;
            options.generateCollider = true;

            // Deterministic: the same text and options give the same hash and mesh.
            SVGImportResult a = SVGImport.Import(Svg, options), b = SVGImport.Import(Svg, options);
            Check("deterministic", a.succeeded && a.importHash == b.importHash && a.mesh.vertices.SequenceEqual(b.mesh.vertices)
                && a.mesh.triangles.SequenceEqual(b.mesh.triangles), "hash " + a.importHash.ToString("X16"));
            Check("result complete", a.shaders.Length > 0 && a.gradients != null && a.gradients.Length == 2 && a.colliderShape != null,
                a.shaders.Length + " shaders, " + (a.gradients == null ? 0 : a.gradients.Length) + " gradients, collider " + (a.colliderShape != null));
            options.scale = 0.02f;
            Check("options change the hash", SVGImport.Import(Svg, options).importHash != a.importHash, "");

            // SVG Importer's conicalGradient extension.
            SVGImportOptions layered = SVGImportOptions.Default;
            layered.useLayers = true;
            SVGImportResult conical = SVGImport.Import("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\">"
                + "<conicalGradient id=\"c\" cx=\"50\" cy=\"50\" r=\"50\" gradientUnits=\"userSpaceOnUse\"><stop offset=\"0\" stop-color=\"red\"/><stop offset=\"1\" stop-color=\"blue\"/></conicalGradient>"
                + "<circle cx=\"50\" cy=\"50\" r=\"50\" fill=\"url(#c)\"/></svg>", layered);
            bool isConical = conical.succeeded && conical.layers.SelectMany(l => l.shapes).Any(shape => shape.fill != null && shape.fill.gradientType == GRADIENT_TYPE.CONICAL);
            Check("conicalGradient imports as a conical gradient", isConical, string.Join(" | ", conical.messages));

            // Exact Group Opacity: what a later opaque child covers is removed from the lower one.
            const string group = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><g opacity=\"0.5\">"
                + "<rect width=\"60\" height=\"60\" fill=\"red\"/><rect x=\"40\" y=\"40\" width=\"60\" height=\"60\" fill=\"blue\"/></g></svg>";
            SVGImportOptions exact = SVGImportOptions.Default, perShape = SVGImportOptions.Default;
            perShape.exactGroupOpacity = false;
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
}
