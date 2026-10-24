// Prototype harness for feathered (Gaussian-blurred) shapes. See DESIGN.md, section 16, item 4.
//
// For each case it writes an SVG using feGaussianBlur (render it in a browser for reference) and
// renders the same case in Unity with the "Feathered Shape" shader, 1 unit = 1 pixel.
//
//   Unity -batchmode -projectPath . -executeMethod FeatheredShapePrototype.Run -out <dir> [-colorSpace Gamma|Linear]

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeatheredShapePrototype
{
    const int Width = 400;
    const int Height = 300;
    // Sigma used for crisp shapes in Unity: Gaussian antialiasing of about one pixel.
    const float CrispSigma = 0.4f;

    struct Shape
    {
        public float cx, cy, width, height, radius, sigma;
        public Color color;
        public Shape(float cx, float cy, float width, float height, float radius, float sigma, Color color)
        {
            this.cx = cx; this.cy = cy; this.width = width; this.height = height;
            this.radius = radius; this.sigma = sigma; this.color = color;
        }
    }

    static readonly Color Black = Color.black;
    static readonly Color Shadow = new Color(0f, 0f, 0f, 0.4f);
    static readonly Color Card = new Color32(0x4a, 0x90, 0xd9, 0xff);

    // SVG coordinates: origin top left, y down. sigma 0 means a crisp shape.
    static readonly Dictionary<string, Shape[]> Cases = new Dictionary<string, Shape[]>
    {
        { "rect", new[] { new Shape(200, 150, 200, 150, 0, 12, Black) } },
        { "rounded", new[] { new Shape(200, 150, 200, 150, 24, 12, Black) } },
        { "thin", new[] { new Shape(200, 150, 20, 150, 0, 12, Black) } },
        { "small_blur", new[] { new Shape(200, 150, 200, 150, 24, 2, Black) } },
        { "drop_shadow", new[] { new Shape(200, 158, 200, 120, 16, 10, Shadow), new Shape(200, 150, 200, 120, 16, 0, Card) } },
    };

    public static void Run()
    {
        string output = Arg("-out") ?? "Temp/FeatheredShapes";
        Directory.CreateDirectory(output);
        ColorSpace originalColorSpace = PlayerSettings.colorSpace;
        string colorSpace = Arg("-colorSpace");
        if (colorSpace != null)
            PlayerSettings.colorSpace = (ColorSpace)Enum.Parse(typeof(ColorSpace), colorSpace);

        try
        {
            var shader = Shader.Find("SVG Importer/Prototypes/Feathered Shape");
            foreach (var pair in Cases)
            {
                File.WriteAllText(Path.Combine(output, pair.Key + ".svg"), ToSvg(pair.Value));
                Render(pair.Value, shader, Path.Combine(output, pair.Key + "-unity-" + PlayerSettings.colorSpace + ".png"));
            }
        }
        finally
        {
            PlayerSettings.colorSpace = originalColorSpace;
        }
        EditorApplication.Exit(0);
    }

    static string ToSvg(Shape[] shapes)
    {
        var inv = CultureInfo.InvariantCulture;
        var svg = new System.Text.StringBuilder();
        svg.AppendFormat(inv, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{0}\" height=\"{1}\">\n", Width, Height);
        svg.AppendFormat(inv, "<rect width=\"{0}\" height=\"{1}\" fill=\"#fff\"/>\n", Width, Height);
        for (int i = 0; i < shapes.Length; i++)
        {
            Shape s = shapes[i];
            string filter = "";
            if (s.sigma > 0f)
            {
                // A filter region covering the whole canvas, so the blur is never clipped.
                svg.AppendFormat(inv, "<filter id=\"b{0}\" filterUnits=\"userSpaceOnUse\" x=\"0\" y=\"0\" width=\"{1}\" height=\"{2}\"><feGaussianBlur stdDeviation=\"{3}\"/></filter>\n",
                    i, Width, Height, s.sigma);
                filter = string.Format(inv, " filter=\"url(#b{0})\"", i);
            }
            svg.AppendFormat(inv, "<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{4}\" fill=\"#{5}\" fill-opacity=\"{6}\"{7}/>\n",
                s.cx - s.width / 2f, s.cy - s.height / 2f, s.width, s.height, s.radius,
                ColorUtility.ToHtmlStringRGB(s.color), s.color.a, filter);
        }
        svg.Append("</svg>\n");
        return svg.ToString();
    }

    static void Render(Shape[] shapes, Shader shader, string path)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Height / 2f;
        camera.transform.position = new Vector3(Width / 2f, Height / 2f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;

        for (int i = 0; i < shapes.Length; i++)
        {
            Shape s = shapes[i];
            float sigma = s.sigma > 0f ? s.sigma : CrispSigma;
            Vector2 half = new Vector2(s.width / 2f, s.height / 2f);
            // The quad covers everything the blur reaches: 4 sigma plus a pixel of margin.
            Vector2 extent = half + Vector2.one * (4f * sigma + 1f);

            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-extent.x, -extent.y), new Vector3(extent.x, -extent.y),
                new Vector3(extent.x, extent.y), new Vector3(-extent.x, extent.y),
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };

            var material = new Material(shader);
            material.SetColor("_Color", s.color);
            material.SetVector("_HalfSize", half);
            material.SetFloat("_Radius", s.radius);
            material.SetFloat("_Sigma", sigma);
            material.renderQueue = 3000 + i;

            var go = new GameObject("Shape " + i);
            // SVG y points down, Unity y points up.
            go.transform.position = new Vector3(s.cx, Height - s.cy, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        camera.targetTexture = target;
        camera.Render();
        var pixels = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        RenderTexture.active = null;
        camera.targetTexture = null;
        // Blending also writes alpha below 1 at soft edges. Save opaque pixels, like a screenshot,
        // so image viewers don't premultiply the colours by that alpha.
        Color32[] colors = pixels.GetPixels32();
        for (int p = 0; p < colors.Length; p++) colors[p].a = 255;
        pixels.SetPixels32(colors);
        File.WriteAllBytes(path, pixels.EncodeToPNG());
    }

    static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
