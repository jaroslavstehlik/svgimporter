// Antialiasing (DESIGN.md, 7.2): renders white shapes on black, where a pixel's value is its
// coverage, at a scale of one SVG unit per pixel, and checks that edges are one pixel soft, centred
// on the outline, without spikes, under any transform.
//
//   Unity -batchmode -projectPath . -executeMethod AntialiasingTests.Run -out <dir>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SVGImporter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AntialiasingTests
{
    const int Size = 256;

    // A 100 x 100 square whose edges fall a quarter of a pixel into their pixels.
    const string Square = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\">"
        + "<rect x=\"40.25\" y=\"40.25\" width=\"100\" height=\"100\" fill=\"white\"/></svg>";
    const string HalfSquare = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\">"
        + "<rect x=\"40.25\" y=\"40.25\" width=\"100\" height=\"100\" fill=\"white\" fill-opacity=\"0.5\"/></svg>";
    // A spike with a 10 degree tip, and a square with a square hole.
    static readonly Vector2[] Spike = { new Vector2(85f, 180f), new Vector2(115f, 180f), new Vector2(100f, 8.5f) };
    const string SpikeSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\">"
        + "<path d=\"M85 180 L115 180 L100 8.5 Z\" fill=\"white\"/></svg>";
    const string Frame = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\">"
        + "<path d=\"M40.25 40.25 h100 v100 h-100 Z M70.25 70.25 v40 h40 v-40 Z\" fill=\"white\"/></svg>";

    public static void Run()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/AntialiasingTests";
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

                float[] square = Render(Square, Quaternion.identity, Vector3.one, output, "square");
                float total = square.Sum();
                Check("a square keeps its size", Mathf.Abs(total - 10000f) < 30f, "coverage " + total.ToString("F0") + " of 10000 pixels");

                // Across the left edge, at pixel x = 68.25: pixels 67, 68 and 69 are 0, 0.75 and 1 covered.
                int row = 130;
                float[] profile = { square[row * Size + 67], square[row * Size + 68], square[row * Size + 69] };
                Check("edge profile is a one-pixel box filter", profile[0] < 0.03f && Mathf.Abs(profile[1] - 0.75f) < 0.05f && profile[2] > 0.97f,
                    string.Join(", ", profile.Select(p => p.ToString("F3"))));

                float[] half = Render(HalfSquare, Quaternion.identity, Vector3.one, output, "half");
                float brightest = half.Max();
                Check("no double blending where fill and fringe meet", brightest < 0.5f + 3f / 255f && Mathf.Abs(half[row * Size + 69] - 0.5f) < 3f / 255f,
                    "brightest " + brightest.ToString("F3") + ", next to the edge " + half[row * Size + 69].ToString("F3"));

                float[] spike = Render(SpikeSvg, Quaternion.identity, Vector3.one, output, "spike");
                float farthest = 0f;
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                        if (spike[y * Size + x] > 2f / 255f)
                            farthest = Mathf.Max(farthest, DistanceOutside(Spike, new Vector2(x + 0.5f - 28f, 228f - (y + 0.5f))));
                Check("no spikes at sharp corners", farthest <= 0.75f, "lit pixels reach " + farthest.ToString("F2") + " pixels outside the shape");

                float[] frame = Render(Frame, Quaternion.identity, Vector3.one, output, "frame");
                float frameTotal = frame.Sum();
                Check("holes keep their size", Mathf.Abs(frameTotal - 8400f) < 40f, "coverage " + frameTotal.ToString("F0") + " of 8400 pixels");

                // Rotated and stretched: the area scales with the determinant, and edges stay one pixel soft.
                float[] stretched = Render(Square, Quaternion.Euler(0f, 0f, 30f), new Vector3(1.4f, 0.7f, 1f), output, "transformed");
                float stretchedTotal = stretched.Sum();
                Check("rotated and stretched squares keep their size", Mathf.Abs(stretchedTotal - 9800f) < 50f,
                    "coverage " + stretchedTotal.ToString("F0") + " of 9800 pixels");
                int soft = stretched.Count(v => v > 0.02f && v < 0.98f);
                Check("rotated and stretched edges stay one pixel soft", soft < 4.6f * 100f * (1.4f + 0.7f), soft + " partly covered pixels");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Check("exception", false, exception.ToString());
        }

        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(Path.Combine(output, "antialiasing.txt"), results);
        EditorApplication.Exit(passed ? 0 : 1);
    }

    // Renders the SVG, white on black at one SVG unit per pixel, centred, and returns each pixel's
    // value (its coverage), bottom row first. The SVG point (x, y) lands at pixel (28 + x, 228 - y).
    static float[] Render(string svg, Quaternion rotation, Vector3 scale, string output, string name)
    {
        SVGImportOptions options = SVGImportOptions.Default;
        options.softEdges = true;
        options.ignoreSVGCanvas = false;
        SVGAsset asset = SVGAsset.Load(svg, options);
        if (asset == null) throw new Exception("couldn't import " + name);

        var go = new GameObject(name);
        go.transform.rotation = rotation;
        go.transform.localScale = scale;
        var renderer = go.AddComponent<SVGRenderer>();
        renderer.vectorGraphics = asset;

        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Size / 200f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        // Geometry antialiasing alone: the project's MSAA would smooth the edges too.
        camera.allowMSAA = false;
        var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        camera.targetTexture = target;
        camera.Render();

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(output, name.Replace(": ", "-") + ".png"), texture.EncodeToPNG());
        float[] values = texture.GetPixels().Select(c => c.r).ToArray();

        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(asset);
        return values;
    }

    // How far a point lies outside a convex polygon (0 inside).
    static float DistanceOutside(Vector2[] polygon, Vector2 point)
    {
        bool inside = true;
        float nearest = float.MaxValue;
        float orientation = Mathf.Sign((polygon[1].x - polygon[0].x) * (polygon[2].y - polygon[0].y) - (polygon[1].y - polygon[0].y) * (polygon[2].x - polygon[0].x));
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
            float side = ((b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x)) * orientation;
            if (side < 0f) inside = false;
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
            nearest = Mathf.Min(nearest, Vector2.Distance(point, a + ab * t));
        }
        return inside ? 0f : nearest;
    }
}
