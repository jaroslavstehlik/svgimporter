// Regression tests for bugs found in the 2.0 code review: each renders or imports the case that
// failed and checks the result.
//
//   Unity -batchmode -projectPath . -executeMethod BugFixTests.Run -out <dir>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SVGImporter;
using SVGImporter.Native;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BugFixTests
{
    const string Root = "Assets/BugFixTests";
    const int Width = 300, Height = 100;

    public static void Run()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/BugFixTests";
        Directory.CreateDirectory(output);
        var results = new List<string>();
        bool passed = true;
        void Check(string name, bool condition, string detail)
        {
            passed &= condition;
            results.Add((condition ? "PASS " : "FAIL ") + name + ": " + detail);
        }

        RenderPipelineAsset pipeline = GraphicsSettings.defaultRenderPipeline;
        ColorSpace colorSpace = PlayerSettings.colorSpace;
        GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
        AssetDatabase.DeleteAsset(Root);
        Directory.CreateDirectory(Root);
        try
        {
            // Opaque + Compress Depth with an element that has a fill and a stroke, overlapped by a
            // later element: the depth pass looked up shapes by their index within one layer and
            // threw, or put shapes at the wrong depth.
            SVGAsset opaque = Import("opaque", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"100\">"
                + "<rect x=\"10\" y=\"10\" width=\"180\" height=\"80\" fill=\"red\" stroke=\"lime\" stroke-width=\"4\"/>"
                + "<rect x=\"110\" y=\"20\" width=\"180\" height=\"60\" fill=\"blue\"/></svg>",
                i => { i.FindProperty("_format").enumValueIndex = (int)SVGAssetFormat.OpaqueAndTransparent; i.FindProperty("_compressDepth").boolValue = true; i.FindProperty("_antialiasing").boolValue = false; });
            Color32 overlap = Render("opaque", 150, 50);
            Check("Opaque depth: the later shape is in front", opaque.sharedMesh != null && overlap.b > 200 && overlap.r < 60, "overlap pixel " + overlap);

            // More than 65,535 vertices: the mesh was cleared and the asset saved empty.
            var circles = new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"364\" height=\"124\">");
            // An odd number of circles each way, so the middle pixel the test reads is on one.
            for (int y = 0; y < 31; y++)
                for (int x = 0; x < 91; x++)
                    circles.AppendFormat("<circle cx=\"{0}\" cy=\"{1}\" r=\"1.9\" fill=\"#00ff00\"/>", 2 + x * 4, 2 + y * 4);
            circles.Append("</svg>");
            SVGAsset big = Import("big", circles.ToString(), i => { i.FindProperty("_vpm").floatValue = 200000f; i.FindProperty("_antialiasing").boolValue = false; });
            int vertices = big.sharedMesh == null ? 0 : big.sharedMesh.vertexCount;
            Color32 bigPixel = vertices > 0 ? Render("big", 150 + 0.5f, 50) : default(Color32);
            Check("meshes over 65,535 vertices", vertices > 65535 && big.sharedMesh.indexFormat == IndexFormat.UInt32 && bigPixel.g > 200,
                vertices + " vertices, " + (big.sharedMesh == null ? "no mesh" : big.sharedMesh.indexFormat.ToString()) + ", pixel " + bigPixel);

            // Colour modifier with a layer selection multiplied the colour once per vertex.
            SVGAsset layered = Import("layered", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"100\">"
                + "<rect width=\"150\" height=\"100\" fill=\"white\"/><circle cx=\"225\" cy=\"50\" r=\"40\" fill=\"white\"/></svg>",
                i => { i.FindProperty("_useLayers").boolValue = true; i.FindProperty("_antialiasing").boolValue = false; });
            Color32 modified = Render("layered", 75, 50, go =>
            {
                var modifier = go.AddComponent<SVGColorModifier>();
                modifier.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                modifier.useSelection = true;
                modifier.layerSelection = new LayerSelection();
                modifier.layerSelection.Add(0);
                go.GetComponent<SVGRenderer>().UpdateRenderer();
            });
            Check("colour modifier with a selection", Mathf.Abs(modified.r - 128) <= 3, "white x 0.5 = " + modified);

            // A tinted renderer switching asset kept the previous asset's colours; setting the
            // colour from a script did nothing until something else rebuilt the renderer.
            SVGAsset white = Import("white", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"100\"><rect width=\"300\" height=\"100\" fill=\"white\"/></svg>", null);
            SVGAsset black = Import("black", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"100\"><rect width=\"300\" height=\"100\" fill=\"#808080\"/></svg>", null);
            Color32 swapped = Render("white", 150, 50, go =>
            {
                // Tinted and built (caching the white asset's colours), then switched.
                var renderer = go.GetComponent<SVGRenderer>();
                renderer.color = Color.red;
                renderer.UpdateRenderer();
                renderer.vectorGraphics = Load("black");
            });
            Check("tinted renderer switching asset", swapped.r > 100 && swapped.r < 160 && swapped.g < 30, "grey x red = " + swapped);
            Color32 recoloured = Render("white", 150, 50, go => go.GetComponent<SVGRenderer>().color = Color.green);
            Check("colour set from a script", recoloured.g > 200 && recoloured.r < 30, "white x green = " + recoloured);

            // A gradient drawn with a custom material: the atlas texture was only given to the
            // atlas's own materials.
            SVGAsset gradient = Import("gradient", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"100\"><linearGradient id=\"g\"><stop offset=\"0\" stop-color=\"red\"/><stop offset=\"1\" stop-color=\"blue\"/></linearGradient><rect width=\"300\" height=\"100\" fill=\"url(#g)\"/></svg>", null);
            // Also after the atlas is replaced, which destroys its texture.
            Action<GameObject> customMaterial = go =>
            {
                go.GetComponent<SVGRenderer>().transparentMaterial = new Material(Shader.Find("SVG Importer/GradientColor/GradientColorAlphaBlended"));
                SVGAtlas.ResetAtlas();
            };
            Color32 left = Render("gradient", 10, 50, customMaterial);
            Color32 right = Render("gradient", 290, 50, customMaterial);
            Check("gradient with a custom material", left.r > 200 && left.b < 60 && right.b > 200 && right.r < 60, "left " + left + ", right " + right);

            // SVG Images replaced the canvas's extra shader channels (TextMeshPro needs Tangent).
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = new GameObject("Canvas").AddComponent<Canvas>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
            var image = new GameObject("Image").AddComponent<SVGImage>();
            image.transform.SetParent(canvas.transform, false);
            image.vectorGraphics = AssetDatabase.LoadAssetAtPath<SVGAsset>(Root + "/gradient.svg");
            Canvas.ForceUpdateCanvases();
            image.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
            Check("canvas channels kept", (canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.Tangent) != 0
                && (canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) != 0, canvas.additionalShaderChannels.ToString());

            // The native core refuses documents above its input limit instead of running out of memory.
            if (SVGNativeCore.isSupported)
            {
                var huge = SVGNativeCore.Import(new byte[65 * 1024 * 1024]);
                Check("native input limit", huge.status == SVGNativeCore.Status.ParseError && huge.errorMessage.Contains("larger"), huge.status + ": " + huge.errorMessage);
            }

            // The texture overlay shaders didn't convert vertex colours in Linear color space.
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Color32 overlay = RenderQuad("SVG Importer/SolidColor/SolidColorTexOverlayAlphaBlended", new Color32(128, 128, 128, 255));
            Check("overlay shader in Linear", Mathf.Abs(overlay.r - 128) <= 3, "grey vertex colour renders as " + overlay);

            // Path bounds used x for the vertical extent.
            Rect pathBounds = new SVGPath(new[] { new Vector2(1f, 2f), new Vector2(5f, 9f) }).bounds;
            Check("path bounds", pathBounds == new Rect(1f, 2f, 4f, 7f), pathBounds.ToString());

            // Copying a mesh over 65,535 vertices kept the destination's 16-bit indices.
            var bigSource = new Mesh { indexFormat = IndexFormat.UInt32, vertices = new Vector3[70000] };
            var copy = new Mesh();
            SVGImporter.Utils.SVGMeshUtils.Fill(bigSource, copy);
            Check("mesh copy keeps 32-bit indices", copy.indexFormat == IndexFormat.UInt32 && copy.vertexCount == 70000, copy.indexFormat + ", " + copy.vertexCount + " vertices");

            // The renderer's component getters returned null until the renderer was enabled, and
            // the change event fired on every rebuild instead of when the asset changed.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var inactive = new GameObject("inactive", typeof(MeshFilter), typeof(MeshRenderer));
            inactive.SetActive(false);
            var inactiveRenderer = inactive.AddComponent<SVGRenderer>();
            Check("component getters on an inactive object", inactiveRenderer.meshFilter != null && inactiveRenderer.meshRenderer != null, "");
            var eventRenderer = new GameObject("events").AddComponent<SVGRenderer>();
            int changes = 0;
            eventRenderer.vectorGraphicsChanged += a => changes++;
            eventRenderer.vectorGraphics = Load("white");
            eventRenderer.UpdateRenderer();
            int afterAssign = changes;
            eventRenderer.UpdateRenderer();
            eventRenderer.UpdateRenderer();
            Check("change event only on asset changes", afterAssign >= 1 && changes == afterAssign, afterAssign + " after assigning, " + changes + " after rebuilding twice");

            // The sorter kept counting up from the previous sort.
            var sorter = new GameObject("sorter").AddComponent<SVGSorter>();
            sorter.layerIndex = 5;
            var first = new GameObject("first").AddComponent<SVGRenderer>();
            var second = new GameObject("second").AddComponent<SVGRenderer>();
            first.transform.SetParent(sorter.transform);
            second.transform.SetParent(sorter.transform);
            first.vectorGraphics = second.vectorGraphics = Load("white");
            sorter.Sort();
            sorter.Sort();
            Check("sorting twice gives the same order", first.sortingOrder == 5 && second.sortingOrder == 6, first.sortingOrder + ", " + second.sortingOrder);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Check("exception", false, e.ToString());
        }
        finally
        {
            PlayerSettings.colorSpace = colorSpace;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssetDatabase.DeleteAsset(Root);
        }

        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(Path.Combine(output, "bugfixes.txt"), results);
        EditorApplication.Exit(passed ? 0 : 1);
    }

    static SVGAsset Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<SVGAsset>(Root + "/" + name + ".svg");
    }

    static SVGAsset Import(string name, string svg, Action<SerializedObject> settings)
    {
        string path = Root + "/" + name + ".svg";
        File.WriteAllText(path, svg);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (settings != null)
        {
            var importer = new SerializedObject(AssetImporter.GetAtPath(path));
            settings(importer);
            importer.ApplyModifiedPropertiesWithoutUndo();
            ((AssetImporter)importer.targetObject).SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<SVGAsset>(path);
    }

    // Renders the asset filling a 300x100 view (3x1 units at the default scale) and returns the
    // pixel at (x, y) in SVG units, y down.
    // Opening a scene unloads unreferenced assets, so assets are loaded by name afterwards.
    static Color32 Render(string name, float x, float y, Action<GameObject> setup = null)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SVGAsset asset = Load(name);
        var go = new GameObject("svg");
        go.AddComponent<SVGRenderer>().vectorGraphics = asset;
        if (setup != null) setup(go);
        Color32[] pixels = Capture(0.5f);
        return pixels[(Height - 1 - (int)y) * Width + (int)x];
    }

    static Color32 RenderQuad(string shader, Color32 color)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var material = new Material(Shader.Find(shader));
        var mesh = new Mesh
        {
            vertices = new[] { new Vector3(-2, -1), new Vector3(2, -1), new Vector3(2, 1), new Vector3(-2, 1) },
            colors32 = Enumerable.Repeat(color, 4).ToArray(),
            uv = new Vector2[4],
            normals = new Vector3[4],
            triangles = new[] { 0, 2, 1, 0, 3, 2 },
        };
        var go = new GameObject("quad");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        Color32[] pixels = Capture(0.5f);
        return pixels[(Height / 2) * Width + Width / 2];
    }

    static Color32[] Capture(float orthographicSize)
    {
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        camera.targetTexture = target;
        camera.Render();
        camera.Render();
        var pixels = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        RenderTexture.active = null;
        camera.targetTexture = null;
        Color32[] result = pixels.GetPixels32();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(pixels);
        return result;
    }
}
