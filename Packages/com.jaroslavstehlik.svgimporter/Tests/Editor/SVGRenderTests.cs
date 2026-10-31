// Render tests for the development project.
//
// Imports a few SVG files with SVG Importer (SVGFileImporter), builds a scene
// that covers every rendering path (solid, gradient, antialiased, lit and UI),
// renders it with the Built-in Render Pipeline, URP and the URP 2D Renderer,
// saves a PNG per pipeline and checks pixel colors.
//
// Run from the command line:
//   Unity -batchmode -projectPath . -executeMethod SVGRenderTests.Run -svgTestOutput <dir>
// Exits with code 0 when every check passes.

#if SVGIMPORTER_URP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SVGImporter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SVGRenderTests
{
    const string Root = "Assets/SVGRenderTests";
    const int Width = 1280;
    const int Height = 720;
    const int Tolerance = 8;

    static readonly Color32 Gray = new Color32(128, 128, 128, 255);

    const string GraySvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\">" +
        "<rect width=\"200\" height=\"200\" fill=\"#808080\"/></svg>";

    const string GradientSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\">" +
        "<defs><linearGradient id=\"g\" x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"0%\">" +
        "<stop offset=\"0\" stop-color=\"#ff0000\"/><stop offset=\"1\" stop-color=\"#0000ff\"/>" +
        "</linearGradient></defs><rect width=\"200\" height=\"200\" fill=\"url(#g)\"/></svg>";

    const string ShapesSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"200\">" +
        "<defs><radialGradient id=\"r\"><stop offset=\"0\" stop-color=\"#ffee00\"/>" +
        "<stop offset=\"1\" stop-color=\"#da1c5c\"/></radialGradient></defs>" +
        "<circle cx=\"60\" cy=\"100\" r=\"55\" fill=\"url(#r)\"/>" +
        "<path d=\"M190 20 L210 75 L268 75 L221 110 L239 165 L190 131 L141 165 L159 110 L112 75 L170 75 Z\" fill=\"#27ccc0\"/>" +
        "<rect x=\"20\" y=\"170\" width=\"260\" height=\"20\" fill=\"#ffffff\" fill-opacity=\"0.5\"/></svg>";

    // Gradient coordinates in the different unit systems. Each one must render red on the left
    // and blue on the right of the rectangle, wherever the rectangle sits on the canvas.
    const string GradientBoundingBoxSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"200\">" +
        "<defs><linearGradient id=\"g\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\">" +
        "<stop offset=\"0\" stop-color=\"#ff0000\"/><stop offset=\"1\" stop-color=\"#0000ff\"/>" +
        "</linearGradient></defs><rect x=\"150\" y=\"0\" width=\"200\" height=\"200\" fill=\"url(#g)\"/></svg>";

    const string GradientUserSpaceSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"200\">" +
        "<defs><linearGradient id=\"g\" gradientUnits=\"userSpaceOnUse\" x1=\"150\" y1=\"0\" x2=\"350\" y2=\"0\">" +
        "<stop offset=\"0\" stop-color=\"#ff0000\"/><stop offset=\"1\" stop-color=\"#0000ff\"/>" +
        "</linearGradient></defs><rect x=\"150\" y=\"0\" width=\"200\" height=\"200\" fill=\"url(#g)\"/></svg>";

    // A radial gradient with the default r="50%" on a circle away from the canvas origin:
    // yellow in the center, pink at the edge.
    const string RadialOffsetSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"200\">" +
        "<defs><radialGradient id=\"r\"><stop offset=\"0\" stop-color=\"#ffff00\"/>" +
        "<stop offset=\"1\" stop-color=\"#ff00ff\"/></radialGradient></defs>" +
        "<circle cx=\"300\" cy=\"100\" r=\"80\" fill=\"url(#r)\"/></svg>";

    struct Variant
    {
        public string name, svg;
        public SVGAssetFormat format;
        public bool softEdges, normals;
        public Variant(string name, string svg, SVGAssetFormat format, bool softEdges = false, bool normals = false)
        {
            this.name = name; this.svg = svg; this.format = format;
            this.softEdges = softEdges; this.normals = normals;
        }
    }

    static readonly Variant[] Variants =
    {
        new Variant("gray_opaque", GraySvg, SVGAssetFormat.OpaqueAndTransparent),
        new Variant("gray_transparent", GraySvg, SVGAssetFormat.Transparent),
        new Variant("gray_antialiased", GraySvg, SVGAssetFormat.Transparent, softEdges: true),
        new Variant("gray_lit", GraySvg, SVGAssetFormat.OpaqueAndTransparent, normals: true),
        new Variant("gradient_transparent", GradientSvg, SVGAssetFormat.Transparent),
        new Variant("shapes_transparent", ShapesSvg, SVGAssetFormat.Transparent, softEdges: true),
        new Variant("gradient_bounding_box", GradientBoundingBoxSvg, SVGAssetFormat.Transparent),
        new Variant("gradient_user_space", GradientUserSpaceSvg, SVGAssetFormat.Transparent),
        new Variant("radial_offset", RadialOffsetSvg, SVGAssetFormat.Transparent),
        new Variant("gray_ui", GraySvg, SVGAssetFormat.Canvas),
        new Variant("gradient_ui", GradientSvg, SVGAssetFormat.Canvas),
    };

    public static void Run()
    {
        string output = Arg("-svgTestOutput") ?? "Temp/SVGRenderTests";
        Directory.CreateDirectory(output);
        // Optional: -svgTestColorSpace Linear|Gamma
        string colorSpace = Arg("-svgTestColorSpace");
        if (colorSpace != null)
            PlayerSettings.colorSpace = (ColorSpace)Enum.Parse(typeof(ColorSpace), colorSpace);
        // Compile shader variants when first drawn, rather than skip them while they compile.
        ShaderUtil.allowAsyncCompilation = false;
        var results = new List<string>();
        bool passed;
        try
        {
            var assets = ImportTestAssets(results);
            passed = true;
            foreach (string pipeline in new[] { "builtin", "urp", "urp2d" })
                passed &= RenderWithPipeline(pipeline, assets, output, results);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            results.Add("EXCEPTION " + e);
            passed = false;
        }
        finally
        {
            SetPipeline(null);
        }

        string engineName = "Native";
        results.Insert(0, "Engine: " + engineName + ", color space: " + PlayerSettings.colorSpace + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(Path.Combine(output, "results-" + engineName + "-" + PlayerSettings.colorSpace + ".txt"), results);
        foreach (string line in results) Debug.Log("[SVGRenderTests] " + line);
        EditorApplication.Exit(passed ? 0 : 1);
    }

    static Dictionary<string, SVGAsset> ImportTestAssets(List<string> results)
    {
        AssetDatabase.DeleteAsset(Root);
        Directory.CreateDirectory(Root);

        var assets = new Dictionary<string, SVGAsset>();
        foreach (var v in Variants)
        {
            string svgPath = Root + "/" + v.name + ".svg";
            File.WriteAllText(svgPath, v.svg);
            AssetDatabase.ImportAsset(svgPath, ImportAssetOptions.ForceSynchronousImport);

            // New .svg files must be switched to SVG Importer automatically, and stay in the project.
            var importer = AssetImporter.GetAtPath(svgPath) as SVGFileImporter;
            if (importer == null)
                throw new Exception("New SVG file was not assigned SVG Importer: " + svgPath
                    + " (importer: " + AssetImporter.GetAtPath(svgPath) + ")");

            var settings = new SerializedObject(importer);
            settings.FindProperty("_format").enumValueIndex = (int)v.format;
            settings.FindProperty("_antialiasing").boolValue = v.softEdges;
            settings.FindProperty("_generateNormals").boolValue = v.normals;
            settings.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();

            var asset = AssetDatabase.LoadAssetAtPath<SVGAsset>(svgPath);
            if (asset == null || asset.sharedMesh == null && v.format != SVGAssetFormat.Canvas)
                throw new Exception("SVG Importer did not import " + svgPath);
            assets[v.name] = asset;
        }

        // With "Import New SVG Files" off, new files keep Unity's importer.
        SVGImporterProjectSettings projectSettings = SVGImporterProjectSettings.instance;
        projectSettings.importNewSVGFiles = false;
        try
        {
            string optOutPath = Root + "/opt_out.svg";
            File.WriteAllText(optOutPath, GraySvg);
            AssetDatabase.ImportAsset(optOutPath, ImportAssetOptions.ForceSynchronousImport);
            AssetImporter optOutImporter = AssetImporter.GetAtPath(optOutPath);
            results.Add((optOutImporter is SVGFileImporter ? "FAIL" : "PASS")
                + " import: with Import New SVG Files off, a new file keeps " + optOutImporter.GetType().FullName);
        }
        finally
        {
            projectSettings.importNewSVGFiles = true;
        }

        bool sourcesKept = Variants.All(v => File.Exists(Root + "/" + v.name + ".svg"));
        bool noLegacyAssets = Variants.All(v => !File.Exists(Root + "/" + v.name + ".asset"));
        results.Add((sourcesKept && noLegacyAssets ? "PASS" : "FAIL") + " import: .svg files kept, no .asset files created");
        if (!sourcesKept || !noLegacyAssets)
            throw new Exception("SVG import left the project in the wrong state");
        return assets;
    }

    static bool RenderWithPipeline(string pipeline, Dictionary<string, SVGAsset> assets, string output, List<string> results)
    {
        SetPipeline(CreatePipelineAsset(pipeline));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // Opening a scene unloads unreferenced assets, so load them again.
        assets = Variants.ToDictionary(v => v.name, v => AssetDatabase.LoadAssetAtPath<SVGAsset>(Root + "/" + v.name + ".svg"));

        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Neutral, fixed ambient light, so lit results don't depend on the project's sky.
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f, 1f);

        var light = new GameObject("Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1f;

        var litMaterial = new Material(Shader.Find("SVG Importer/SolidColor/SolidColorDiffuseOpaque"));

        var renderers = new Dictionary<string, SVGRenderer>
        {
            { "gray_opaque", CreateRenderer(assets["gray_opaque"], new Vector3(-6f, 2.5f)) },
            { "gray_transparent", CreateRenderer(assets["gray_transparent"], new Vector3(-3f, 2.5f)) },
            { "gray_antialiased", CreateRenderer(assets["gray_antialiased"], new Vector3(0f, 2.5f)) },
            { "gray_lit", CreateRenderer(assets["gray_lit"], new Vector3(3f, 2.5f), litMaterial) },
            { "gradient_transparent", CreateRenderer(assets["gradient_transparent"], new Vector3(6f, 2.5f)) },
            { "shapes_transparent", CreateRenderer(assets["shapes_transparent"], new Vector3(-5f, -2.5f)) },
            { "gradient_bounding_box", CreateRenderer(assets["gradient_bounding_box"], new Vector3(-6f, 0f)) },
            { "gradient_user_space", CreateRenderer(assets["gradient_user_space"], new Vector3(-3f, 0f)) },
            { "radial_offset", CreateRenderer(assets["radial_offset"], new Vector3(0f, 0f)) },
            { "gray_unlit_2d", CreateRenderer(assets["gray_transparent"], new Vector3(3f, 0f)) },
        };
        renderers["gray_unlit_2d"].receive2DLights = false;
        renderers["gray_lit_transparent"] = CreateRenderer(assets["gray_transparent"], new Vector3(6f, 0f));
        renderers["gray_lit_transparent"].transparentMaterial = new Material(Shader.Find("SVG Importer/SolidColor/SolidColorDiffuseAlphaBlended"));

        var canvas = new GameObject("Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 5f;
        var images = new Dictionary<string, SVGImage>
        {
            { "gray_ui", CreateImage(canvas, assets["gray_ui"], new Vector2(800f, -540f)) },
            { "gradient_ui", CreateImage(canvas, assets["gradient_ui"], new Vector2(1050f, -540f)) },
        };

        var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();

        // Mark the gradient atlas dirty: rendering must rebuild it through the pipeline's
        // callbacks (Camera.onPreRender or RenderPipelineManager.beginContextRendering),
        // because EditorApplication.update does not run during this method.
        typeof(SVGAtlas).GetField("_atlasHasChanged", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(SVGAtlas.Instance, true);
        camera.Render();
        bool atlasDirtyAfter = SVGAtlas.Instance.atlasHasChanged;

        // Renders again, so the frame read back uses the rebuilt atlas, and saves it.
        Texture2D RenderFrame(string name)
        {
            camera.Render();
            var frame = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            RenderTexture.active = target;
            frame.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            frame.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(output, name + "-" + PlayerSettings.colorSpace + ".png"), frame.EncodeToPNG());
            return frame;
        }
        var pixels = RenderFrame(pipeline);

        bool ok = true;
        void Check(string name, bool condition, string detail)
        {
            ok &= condition;
            results.Add((condition ? "PASS " : "FAIL ") + pipeline + " " + name + ": " + detail);
        }

        Check("atlas rebuilt by render", !atlasDirtyAfter, "atlas still dirty after render: " + atlasDirtyAfter);

        foreach (string name in new[] { "gray_opaque", "gray_transparent", "gray_antialiased", "gray_unlit_2d" })
        {
            Color32 c = Sample(pixels, camera, renderers[name].GetComponent<Renderer>().bounds.center);
            Check(name, Near(c, Gray), "expected " + Gray + ", got " + c);
        }

        Color32 lit = Sample(pixels, camera, renderers["gray_lit"].GetComponent<Renderer>().bounds.center);
        Check("gray_lit", lit.r > 100 && Math.Abs(lit.r - lit.g) < Tolerance && Math.Abs(lit.r - lit.b) < Tolerance,
            "expected a lit gray (not black or pink), got " + lit);

        Bounds gradientBounds = renderers["gradient_transparent"].GetComponent<Renderer>().bounds;
        CheckGradient(Check, "gradient_transparent",
            Sample(pixels, camera, gradientBounds.center - new Vector3(gradientBounds.extents.x * 0.8f, 0f)),
            Sample(pixels, camera, gradientBounds.center),
            Sample(pixels, camera, gradientBounds.center + new Vector3(gradientBounds.extents.x * 0.8f, 0f)));

        foreach (string name in new[] { "gradient_bounding_box", "gradient_user_space" })
        {
            Bounds b = renderers[name].GetComponent<Renderer>().bounds;
            CheckGradient(Check, name,
                Sample(pixels, camera, b.center - new Vector3(b.extents.x * 0.8f, 0f)),
                Sample(pixels, camera, b.center),
                Sample(pixels, camera, b.center + new Vector3(b.extents.x * 0.8f, 0f)));
        }

        Bounds radial = renderers["radial_offset"].GetComponent<Renderer>().bounds;
        Color32 radialCenter = Sample(pixels, camera, radial.center);
        Color32 radialEdge = Sample(pixels, camera, radial.center + new Vector3(radial.extents.x * 0.95f, 0f));
        Check("radial_offset", radialCenter.r > 230 && radialCenter.g > 200 && radialCenter.b < 60
            && radialEdge.r > 230 && radialEdge.g < 60 && radialEdge.b > 180,
            "expected yellow center and magenta edge, got " + radialCenter + " / " + radialEdge);

        Color32 grayUi = SampleUI(pixels, camera, images["gray_ui"], 0f);
        Check("gray_ui", Near(grayUi, Gray), "expected " + Gray + ", got " + grayUi);
        CheckGradient(Check, "gradient_ui",
            SampleUI(pixels, camera, images["gradient_ui"], -0.4f),
            SampleUI(pixels, camera, images["gradient_ui"], 0f),
            SampleUI(pixels, camera, images["gradient_ui"], 0.4f));

        // The 2D Renderer lights SVG Renderers with 2D lights, as it lights sprites: an orange
        // global light keeps red and darkens green, and blue more.
        if (pipeline == "urp2d")
        {
            CreateGlobalLight2D(new Color(1f, 0.5f, 0.25f));
            Texture2D litPixels = RenderFrame(pipeline + "-light2d");
            var points = new List<KeyValuePair<string, Vector3>>();
            foreach (string name in new[] { "gray_opaque", "gray_transparent", "gray_antialiased", "gray_lit", "gray_lit_transparent", "gray_unlit_2d" })
                points.Add(new KeyValuePair<string, Vector3>(name, renderers[name].GetComponent<Renderer>().bounds.center));
            points.Add(new KeyValuePair<string, Vector3>("gradient_transparent", gradientBounds.center));
            points.Add(new KeyValuePair<string, Vector3>("radial_offset", radial.center));

            foreach (var point in points)
            {
                Color32 unlit = Sample(pixels, camera, point.Value);
                Color32 litColor = Sample(litPixels, camera, point.Value);
                if (point.Key == "gray_unlit_2d")
                {
                    Check("2D lights: receive2DLights off", Near(litColor, unlit), "expected " + unlit + " unchanged, got " + litColor);
                    continue;
                }
                Check("2D lights: " + point.Key, Math.Abs(litColor.r - unlit.r) <= Tolerance
                    && (unlit.g < 40 || litColor.g < unlit.g - 15) && (unlit.b < 40 || litColor.b < unlit.b - 15),
                    "expected " + unlit + " tinted orange, got " + litColor);
            }
            Color32 uiUnlit = SampleUI(pixels, camera, images["gray_ui"], 0f);
            Color32 uiLit = SampleUI(litPixels, camera, images["gray_ui"], 0f);
            Check("2D lights: Canvas unlit", Near(uiLit, uiUnlit), "expected " + uiUnlit + " unchanged, got " + uiLit);
        }

        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        return ok;
    }

    static void CheckGradient(Action<string, bool, string> check, string name, Color32 left, Color32 middle, Color32 right)
    {
        check(name, left.r > 180 && left.b < 80 && right.b > 180 && right.r < 80,
            "expected red on the left and blue on the right, got " + left + " / " + right);
        // SVG gradients interpolate in sRGB, like browsers do: the midpoint of red to blue is about (128, 0, 128).
        check(name + " midpoint", Math.Abs(middle.r - 128) <= 20 && Math.Abs(middle.b - 128) <= 20 && middle.g <= 20,
            "expected about (128, 0, 128), got " + middle);
    }

    // A global Light2D, found by name: URP moved it to its own assembly after Unity 6.0.
    static void CreateGlobalLight2D(Color color)
    {
        Type type = TypeCache.GetTypesDerivedFrom<Behaviour>()
            .First(t => t.FullName == "UnityEngine.Rendering.Universal.Light2D");
        var light = (Component)new GameObject("Light 2D").AddComponent(type);
        PropertyInfo lightType = type.GetProperty("lightType");
        lightType.SetValue(light, Enum.Parse(lightType.PropertyType, "Global"));
        type.GetProperty("color").SetValue(light, color);
        type.GetProperty("intensity").SetValue(light, 1f);
    }

    static SVGRenderer CreateRenderer(SVGAsset asset, Vector3 position, Material opaqueMaterial = null)
    {
        var go = new GameObject(asset.name);
        go.transform.position = position;
        var renderer = go.AddComponent<SVGRenderer>();
        if (opaqueMaterial != null) renderer.opaqueMaterial = opaqueMaterial;
        renderer.vectorGraphics = asset;
        return renderer;
    }

    static SVGImage CreateImage(Canvas canvas, SVGAsset asset, Vector2 position)
    {
        var go = new GameObject(asset.name, typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(160f, 160f);
        rect.anchoredPosition = position;
        var image = go.AddComponent<SVGImage>();
        image.vectorGraphics = asset;
        return image;
    }

    static Color32 Sample(Texture2D pixels, Camera camera, Vector3 world)
    {
        Vector3 screen = camera.WorldToScreenPoint(world);
        return pixels.GetPixel(Mathf.RoundToInt(screen.x), Mathf.RoundToInt(screen.y));
    }

    // offset is in fractions of the image width, from its center.
    static Color32 SampleUI(Texture2D pixels, Camera camera, SVGImage image, float offset)
    {
        var rect = image.rectTransform;
        Vector3 world = rect.TransformPoint(rect.rect.center + new Vector2(rect.rect.width * offset, 0f));
        return Sample(pixels, camera, world);
    }

    static bool Near(Color32 a, Color32 b)
    {
        return Math.Abs(a.r - b.r) <= Tolerance && Math.Abs(a.g - b.g) <= Tolerance && Math.Abs(a.b - b.b) <= Tolerance;
    }

    static RenderPipelineAsset CreatePipelineAsset(string pipeline)
    {
        if (pipeline == "builtin") return null;

        string path = Root + "/" + pipeline + ".asset";
        string rendererPath = Root + "/" + pipeline + "_Renderer.asset";
        ScriptableRendererData rendererData;
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        if (pipeline == "urp2d")
        {
            var menus = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.Rendering.Universal.Renderer2DMenus"))
                .First(t => t != null);
            rendererData = (ScriptableRendererData)menus.GetMethod("CreateRendererAsset", flags)
                .Invoke(null, new object[] { rendererPath, RendererType._2DRenderer, false, "Renderer" });
        }
        else
        {
            rendererData = (ScriptableRendererData)typeof(UniversalRenderPipelineAsset).GetMethod("CreateRendererAsset", flags)
                .Invoke(null, new object[] { rendererPath, RendererType.UniversalRenderer, false, "Renderer" });
        }
        var asset = UniversalRenderPipelineAsset.Create(rendererData);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void SetPipeline(RenderPipelineAsset asset)
    {
        GraphicsSettings.defaultRenderPipeline = asset;
        QualitySettings.renderPipeline = asset;
    }

    static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
#endif
