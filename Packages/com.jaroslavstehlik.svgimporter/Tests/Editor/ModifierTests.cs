// SVG modifiers, rendered: blur keeps shapes solid and glows around them, motion blur smears
// along its direction only, and the colour modifier tints. One SVG unit is one pixel; the SVG
// point (x, y) lands at pixel (x, 300 - y), bottom row first.
//
//   Unity -batchmode -projectPath . -executeMethod ModifierTests.Run -out <dir>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SVGImporter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ModifierTests
{
    const int Size = 300;
    // A thin rounded stick, as in the Motion Blur sample, and a square.
    const string Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"300\">"
        + "<rect x=\"40\" y=\"50\" width=\"20\" height=\"200\" rx=\"10\" fill=\"#675A53\"/>"
        + "<rect x=\"140\" y=\"100\" width=\"100\" height=\"100\" fill=\"#45B86F\"/></svg>";
    static readonly Color Background = new Color(0.2f, 0.2f, 0.2f, 1f);

    public static void Run()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/ModifierTests";
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
            Shader.SetGlobalFloat("SVG_ANTIALIASING_WIDTH", 2f);
            {
                Color[] plain = Render(null, output, "plain");
                Color[] blurred = Render(renderer => {
                    var blur = renderer.gameObject.AddComponent<SVGBlurModifier>();
                    blur.radius = 3f;
                }, output, "blur");
                Color[] smeared = Render(renderer => {
                    var blur = renderer.gameObject.AddComponent<SVGBlurModifier>();
                    blur.radius = 3f;
                    blur.motionBlur = true;
                    blur.manualMotionBlur = true;
                    blur.direction = 0f;
                }, output, "motion");
                Color[] tinted = Render(renderer => {
                    renderer.gameObject.AddComponent<SVGColorModifier>().color = new Color(0.5f, 1f, 1f, 1f);
                }, output, "color");

                // Blur: the inside of the stick and the square stays as it was, up to a few pixels from the edge.
                float core = new[] { At(blurred, 50, 150), At(blurred, 44, 150), At(blurred, 190, 150), At(blurred, 144, 150) }
                    .Zip(new[] { At(plain, 50, 150), At(plain, 44, 150), At(plain, 190, 150), At(plain, 144, 150) }, Difference).Max();
                Check("blur keeps shapes solid", core < 4f / 255f, "largest change inside " + core.ToString("F3"));

                // ... and glows outward: pixels outside the edge that were background now aren't, fading with distance.
                float[] glow = Enumerable.Range(1, 8).Select(d => Difference(At(blurred, 140 - d, 150), Background)).ToArray();
                bool fades = glow[0] > 0.05f && glow.Zip(glow.Skip(1), (a, b) => b <= a + 1f / 255f).All(x => x);
                Check("blur glows outward and fades", fades && Difference(At(plain, 137, 150), Background) < 1f / 255f,
                    string.Join(", ", glow.Select(g => g.ToString("F2"))));

                // ... and only mixes the shape's colour with the background: no ring brighter or darker
                // than both, as when the fringe's inner edge was pushed into the shape.
                float stray = 0f;
                foreach (var span in new[] { (from: 25, to: 75, x: 50), (from: 125, to: 160, x: 190) })
                {
                    Color fill = At(plain, span.x, 150);
                    for (int x = span.from; x < span.to; x++)
                        stray = Mathf.Max(stray, Outside(At(blurred, x, 150), fill, Background));
                }
                Check("blur only mixes shape and background", stray < 3f / 255f, "largest stray " + stray.ToString("F3"));

                // The glow ramps into the shape without a step at its edge.
                float step = Enumerable.Range(25, 30).Max(x => Difference(At(blurred, x, 150), At(blurred, x + 1, 150)));
                Check("blur ramps into the shape", step < 12f / 255f, "largest step between neighbours " + step.ToString("F3"));

                // A blur wider than the stick still leaves its middle solid.
                Color[] wide = Render(renderer => renderer.gameObject.AddComponent<SVGBlurModifier>().radius = 6f, output, "wide blur");
                float stick = Difference(At(wide, 50, 150), At(plain, 50, 150));
                Check("a wide blur keeps thin shapes solid", stick < 4f / 255f, "change in the middle of the stick " + stick.ToString("F3"));

                // Motion blur along x: the square smears left and right, not up and down.
                float sideways = Difference(At(smeared, 135, 150), Background);
                float vertical = Difference(At(smeared, 190, 96), Background);
                Check("motion blur smears along its direction", sideways > 0.05f && vertical < 0.02f,
                    "4 px left " + sideways.ToString("F2") + ", 4 px above " + vertical.ToString("F2"));

                // The colour modifier multiplies the fill: half the red, the rest unchanged.
                Color before = At(plain, 190, 150), after = At(tinted, 190, 150);
                Check("colour modifier tints", Mathf.Abs(after.r - before.r * 0.5f) < 3f / 255f && Mathf.Abs(after.g - before.g) < 2f / 255f,
                    before + " -> " + after);
            }

            // Modifiers rebuild the mesh every frame: each rebuild starts from the asset's layers,
            // so the effect doesn't add up, and reuses its buffers. A grid of 2,500 circles makes
            // the per-vertex arrays large enough to measure.
            {
                var grid = new System.Text.StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"500\" height=\"500\">");
                for (int y = 0; y < 50; y++)
                    for (int x = 0; x < 50; x++)
                        grid.Append("<circle cx=\"" + (x * 10 + 5) + "\" cy=\"" + (y * 10 + 5) + "\" r=\"4\" fill=\"#45B86F\"/>");
                grid.Append("</svg>");
                SVGImportOptions options = SVGImportOptions.Default;
                options.softEdges = true;
                options.useLayers = true;
                SVGAsset asset = SVGAsset.Load(grid.ToString(), options);
                var go = new GameObject("animated");
                var renderer = go.AddComponent<SVGRenderer>();
                renderer.vectorGraphics = asset;
                go.AddComponent<SVGTransformModifier>().properties.position = new Vector3(1f, 0f, 0f);
                go.AddComponent<SVGColorModifier>().color = new Color(0.5f, 1f, 1f, 1f);
                var rebuild = (Action)Delegate.CreateDelegate(typeof(Action), renderer,
                    typeof(SVGRenderer).GetMethod("RebuildModifiedMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
                Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
                rebuild();
                Vector3 once = mesh.bounds.center;
                Color32 onceColor = mesh.colors32[0];
                rebuild();
                rebuild();
                Check("modifier rebuilds don't add up", Vector3.Distance(mesh.bounds.center, once) < 1e-4f && mesh.colors32[0].Equals(onceColor) && go.GetComponent<MeshFilter>().sharedMesh == mesh,
                    "centre " + once.ToString("F3") + " -> " + mesh.bounds.center.ToString("F3") + ", colour " + onceColor + " -> " + mesh.colors32[0]);
                long allocated = Allocated(() => { for (int i = 0; i < 10; i++) rebuild(); });
                // For comparison, the full rebuild modifiers used to run every frame.
                long full = Allocated(() => { for (int i = 0; i < 10; i++) renderer.UpdateRenderer(); });
                Check("modifier rebuilds reuse their buffers", allocated < 64 * 1024 && full > 1 << 20,
                    allocated + " bytes in 10 rebuilds of " + mesh.vertexCount + " vertices (full rebuilds: " + full + ")");
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Check("exception", false, exception.ToString());
        }

        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(Path.Combine(output, "modifiers.txt"), results);
        EditorApplication.Exit(passed ? 0 : 1);
    }

    // The managed memory an action allocates: the heap's growth, measured again if a garbage
    // collection ran meanwhile. Unity's Mono doesn't count allocations per thread, and small
    // allocations may not show until the next collection.
    static long Allocated(Action action)
    {
        action();
        for (int attempt = 0; ; attempt++)
        {
            GC.Collect();
            int collections = GC.CollectionCount(0);
            long before = GC.GetTotalMemory(false);
            action();
            long grown = GC.GetTotalMemory(false) - before;
            if (GC.CollectionCount(0) == collections || attempt == 4) return grown;
        }
    }

    static Color At(Color[] pixels, int x, int y) { return pixels[(Size - y) * Size + x]; }

    // How far a colour lies outside the range between two others, in its worst channel.
    static float Outside(Color c, Color a, Color b)
    {
        float worst = 0f;
        for (int i = 0; i < 3; i++)
            worst = Mathf.Max(worst, Mathf.Max(Mathf.Min(a[i], b[i]) - c[i], c[i] - Mathf.Max(a[i], b[i])));
        return worst;
    }

    static float Difference(Color a, Color b) { return Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)); }

    // Renders the test SVG, imported with layers (modifiers need them) and soft edges, in Gamma
    // values as read back from an sRGB target.
    static Color[] Render(Action<SVGRenderer> setup, string output, string name)
    {
        SVGImportOptions options = SVGImportOptions.Default;
        options.softEdges = true;
        options.useLayers = true;
        options.ignoreSVGCanvas = false;
        SVGAsset asset = SVGAsset.Load(Svg, options);
        if (asset == null) throw new Exception("couldn't import " + name);

        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Size / 200f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
        camera.allowMSAA = false;
        var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        camera.targetTexture = target;

        var go = new GameObject(name);
        var renderer = go.AddComponent<SVGRenderer>();
        renderer.vectorGraphics = asset;
        if (setup != null)
        {
            setup(renderer);
            foreach (var blur in go.GetComponents<SVGBlurModifier>()) blur.camera = camera;
            renderer.UpdateRenderer();
        }
        // Each render rebuilds the mesh with the modifiers; the last one is read back.
        camera.Render();
        camera.Render();
        camera.Render();

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(output, name.Replace(": ", "-") + ".png"), texture.EncodeToPNG());
        Color[] pixels = texture.GetPixels();

        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(asset);
        return pixels;
    }
}
