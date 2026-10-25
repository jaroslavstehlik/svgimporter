// SVG Renderer and the meshes derived from SVG Assets: colliders follow the asset, meshes made for
// a renderer or derived from an asset are freed with it, and assets imported with Use Layers
// work wherever a mesh is needed.
//
//   Unity -batchmode -projectPath . -executeMethod RendererTests.Run -out <dir>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SVGImporter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RendererTests
{
    const string Square = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><rect width=\"100\" height=\"100\" fill=\"red\"/></svg>";
    const string Wide = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"100\"><rect width=\"300\" height=\"100\" fill=\"blue\"/></svg>";
    const string Gradient = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\">"
        + "<linearGradient id=\"g\"><stop offset=\"0\" stop-color=\"red\"/><stop offset=\"1\" stop-color=\"blue\"/></linearGradient>"
        + "<rect width=\"60\" height=\"100\" fill=\"url(#g)\"/><circle cx=\"80\" cy=\"50\" r=\"20\" fill=\"lime\"/></svg>";

    public static void Run()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/RendererTests";
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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SVGImportOptions options = SVGImportOptions.Default;
            options.generateCollider = true;
            SVGAsset square = SVGAsset.Load(Square, options), wide = SVGAsset.Load(Wide, options);

            // A collider on a plain renderer (shared mesh) follows an asset swap and an asset removal.
            var go = new GameObject("collider");
            var renderer = go.AddComponent<SVGRenderer>();
            renderer.vectorGraphics = square;
            var collider = go.AddComponent<SVGCollider2D>();
            var polygon = go.GetComponent<PolygonCollider2D>();
            float before = polygon.bounds.size.x;
            renderer.vectorGraphics = wide;
            float after = polygon.bounds.size.x;
            renderer.vectorGraphics = null;
            Check("the collider follows the asset", Mathf.Abs(before - 1f) < 0.05f && Mathf.Abs(after - 3f) < 0.05f && polygon.pathCount == 0,
                "widths " + before.ToString("F2") + " then " + after.ToString("F2") + ", then " + polygon.pathCount + " paths");
            UnityEngine.Object.DestroyImmediate(go);

            // A tinted renderer draws its own mesh, which goes away with it.
            go = new GameObject("tinted");
            renderer = go.AddComponent<SVGRenderer>();
            renderer.vectorGraphics = square;
            renderer.color = new Color(1f, 1f, 1f, 0.5f);
            Mesh own = go.GetComponent<MeshFilter>().sharedMesh;
            bool distinct = own != null && own != square.sharedMesh;
            UnityEngine.Object.DestroyImmediate(go);
            Check("a renderer's own mesh is freed with it", distinct && own == null, distinct ? "" : "the renderer had no mesh of its own");

            // Meshes derived from an asset (here, gradient slots) go away with the asset.
            SVGAsset gradient = SVGAsset.Load(Gradient, SVGImportOptions.Default);
            Mesh derived = gradient.sharedMesh;
            UnityEngine.Object.DestroyImmediate(gradient);
            Check("an asset's derived meshes are freed with it", derived == null, "");

            // Use Layers: the asset keeps layers, yet has bounds and a mesh, and sliced renderers work.
            SVGImportOptions layered = SVGImportOptions.Default;
            layered.useLayers = true;
            layered.border = new Vector4(0.25f, 0.25f, 0.25f, 0.25f);
            SVGAsset layers = SVGAsset.Load(Gradient, layered);
            Check("a Use Layers asset has a mesh and bounds", layers.sharedMesh != null && layers.sharedMesh.vertexCount > 0 && layers.bounds.size.x > 0.5f,
                layers.sharedMesh == null ? "no mesh" : layers.sharedMesh.vertexCount + " vertices, bounds " + layers.bounds.size);
            go = new GameObject("sliced", typeof(RectTransform));
            ((RectTransform)go.transform).sizeDelta = new Vector2(3f, 2f);
            renderer = go.AddComponent<SVGRenderer>();
            renderer.vectorGraphics = layers;
            renderer.type = SVGRenderer.Type.Sliced;
            Vector3[] vertices = go.GetComponent<MeshFilter>().sharedMesh.vertices;
            bool finite = vertices.Length > 0 && vertices.All(v => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y));
            Check("a sliced renderer of a Use Layers asset has finite vertices", finite, vertices.Length + " vertices");
            UnityEngine.Object.DestroyImmediate(go);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Check("exception", false, exception.ToString());
        }

        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(Path.Combine(output, "renderer.txt"), results);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
