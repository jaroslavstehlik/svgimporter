// Gradient atlas lifecycle test: gradients must keep rendering correctly through the Editor events
// that recreate or reload state. A scene with gradient SVGs (an SVG Renderer, one with Use Layers,
// and an SVG Image) is rendered once, then again after each event, and must look the same.
//
//   Unity -batchmode -projectPath . -executeMethod AtlasLifecycleTests.Run -out <dir>
//
// The test spans script reloads and play mode changes, so it runs as a sequence of steps kept in
// SessionState. Results: <dir>/atlas-lifecycle.txt and a PNG per step.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SVGImporter;
using SVGImporter.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AtlasLifecycleTests
{
    const string Root = "Assets/AtlasLifecycleTest";
    const string ScenePath = Root + "/AtlasLifecycle.unity";
    const string OtherScenePath = Root + "/Other.unity";
    const string StepKey = "AtlasLifecycleTests.step";
    const string OutputKey = "AtlasLifecycleTests.output";
    const string RecordedKey = "AtlasLifecycleTests.recorded";
    const int Width = 640, Height = 360;

    static readonly string[] Steps =
    {
        "baseline", "script reload", "enter play mode", "exit play mode",
        "enter play mode without domain reload", "exit play mode without domain reload",
        "reimport", "atlas full", "scene switch", "edit",
    };

    // The first SVG after editing: its gradient becomes green to yellow.
    const string EditedSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><linearGradient id=\"g\"><stop offset=\"0\" stop-color=\"#00ff00\"/><stop offset=\"1\" stop-color=\"#ffff00\"/></linearGradient><rect width=\"100\" height=\"100\" fill=\"url(#g)\"/></svg>";

    // Two-stop and many-stop gradients, linear and radial, in distinct colours.
    static readonly string[] Svgs =
    {
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><linearGradient id=\"g\"><stop offset=\"0\" stop-color=\"#ff0000\"/><stop offset=\"1\" stop-color=\"#0000ff\"/></linearGradient><rect width=\"100\" height=\"100\" fill=\"url(#g)\"/></svg>",
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><radialGradient id=\"g\"><stop offset=\"0\" stop-color=\"#ffff00\"/><stop offset=\"0.5\" stop-color=\"#00ff00\"/><stop offset=\"1\" stop-color=\"#004000\"/></radialGradient><circle cx=\"50\" cy=\"50\" r=\"50\" fill=\"url(#g)\"/></svg>",
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><linearGradient id=\"g\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#ff00ff\"/><stop offset=\"0.5\" stop-color=\"#ffffff\"/><stop offset=\"1\" stop-color=\"#00ffff\"/></linearGradient><rect width=\"100\" height=\"100\" fill=\"url(#g)\"/></svg>",
    };

    static AtlasLifecycleTests()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (SessionState.GetInt(StepKey, -1) >= 0)
            EditorApplication.delayCall += Continue;
    }

    public static void Run()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/AtlasLifecycleTests";
        Directory.CreateDirectory(output);
        SessionState.SetString(OutputKey, Path.GetFullPath(output));
        File.WriteAllText(ResultsPath, "Unity " + Application.unityVersion + "\n");
        CreateScene();
        SessionState.SetInt(StepKey, 0);
        SessionState.SetInt(RecordedKey, -1);
        Continue();
    }

    static string ResultsPath => Path.Combine(SessionState.GetString(OutputKey, "Temp"), "atlas-lifecycle.txt");

    // Runs the current step: renders and compares, then starts the next event.
    static void Continue()
    {
        int step = SessionState.GetInt(StepKey, -1);
        if (step < 0 || step >= Steps.Length) return;
        try
        {
            if (EditorApplication.isPlaying != (Steps[step].StartsWith("enter play mode")))
                return; // Waiting for a play mode change.
            // A reload during a play mode change resumes the test twice.
            if (SessionState.GetInt(RecordedKey, -1) >= step) return;
            SessionState.SetInt(RecordedKey, step);
            Record(step);
            if (Steps[step] == "enter play mode") ImportDuringPlayMode();
            Next(step + 1);
        }
        catch (Exception e)
        {
            File.AppendAllText(ResultsPath, "EXCEPTION in " + Steps[step] + ": " + e + "\n");
            Finish();
        }
    }

    static void Next(int step)
    {
        SessionState.SetInt(StepKey, step);
        if (step >= Steps.Length) { Finish(); return; }
        switch (Steps[step])
        {
            case "script reload":
                EditorUtility.RequestScriptReload();
                break;
            case "enter play mode":
                EditorSettings.enterPlayModeOptionsEnabled = false;
                EditorApplication.EnterPlaymode();
                break;
            case "enter play mode without domain reload":
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.EnterPlaymode();
                break;
            case "exit play mode":
            case "exit play mode without domain reload":
                EditorApplication.ExitPlaymode();
                break;
            case "reimport":
                foreach (string path in Directory.GetFiles(Root, "*.svg"))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                EditorApplication.delayCall += Continue;
                break;
            case "atlas full":
                // Other assets loading 600 more gradients than the atlas held at first.
                for (int i = 0; i < 600; i++)
                {
                    var color = new Color32((byte)(i * 7), (byte)(i * 13), (byte)(i * 29), 255);
                    SVGAtlas.Instance.AddGradient(new SVGGradient(
                        new[] { new SVGGradientColorKey(color, 0f), new SVGGradientColorKey(Color.black, 1f) },
                        new[] { new SVGGradientAlphaKey(1f, 0f), new SVGGradientAlphaKey(1f, 1f) }));
                }
                EditorApplication.delayCall += Continue;
                break;
            case "edit":
                File.WriteAllText(Root + "/gradient0.svg", EditedSvg);
                AssetDatabase.ImportAsset(Root + "/gradient0.svg", ImportAssetOptions.ForceSynchronousImport);
                EditorApplication.delayCall += Continue;
                break;
            case "scene switch":
                EditorSceneManager.OpenScene(OtherScenePath, OpenSceneMode.Single);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                EditorApplication.delayCall += Continue;
                break;
            default:
                EditorApplication.delayCall += Continue;
                break;
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (SessionState.GetInt(StepKey, -1) < 0) return;
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += Continue;
    }

    static void Finish()
    {
        SessionState.EraseInt(StepKey);
        EditorSettings.enterPlayModeOptionsEnabled = false;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        bool failed = File.ReadAllText(ResultsPath).Contains("FAIL") || File.ReadAllText(ResultsPath).Contains("EXCEPTION");
        File.AppendAllText(ResultsPath, failed ? "RESULT: FAIL\n" : "RESULT: PASS\n");
        EditorApplication.Exit(failed ? 1 : 0);
    }

    // An SVG imported while the game runs (edited on disk, auto refresh) used to be saved empty.
    static void ImportDuringPlayMode()
    {
        string path = Root + "/played.svg";
        File.WriteAllText(path, Svgs[0]);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var asset = AssetDatabase.LoadAssetAtPath<SVGAsset>(path);
        int vertices = asset != null && asset.sharedMesh != null ? asset.sharedMesh.vertexCount : 0;
        File.AppendAllText(ResultsPath, string.Format("{0} import during play mode: {1} vertices\n", vertices > 0 ? "PASS" : "FAIL", vertices));
    }

    static void CreateScene()
    {
        AssetDatabase.DeleteAsset(Root);
        Directory.CreateDirectory(Root);
        for (int i = 0; i < Svgs.Length; i++)
            File.WriteAllText(Root + "/gradient" + i + ".svg", Svgs[i]);
        // The third SVG again, with Use Layers: the renderer builds its own mesh from the layers.
        // Its gradient's index in the asset differs from its atlas slot.
        File.WriteAllText(Root + "/layers.svg", Svgs[2]);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var layersImporter = new SerializedObject(AssetImporter.GetAtPath(Root + "/layers.svg"));
        layersImporter.FindProperty("_useLayers").boolValue = true;
        layersImporter.ApplyModifiedPropertiesWithoutUndo();
        ((AssetImporter)layersImporter.targetObject).SaveAndReimport();

        EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), OtherScenePath);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 2f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.gray;

        for (int i = 0; i < Svgs.Length; i++)
            AddRenderer(Root + "/gradient" + i + ".svg", new Vector3(-3f + i * 1.5f, 0.8f, 0f));
        AddRenderer(Root + "/layers.svg", new Vector3(1.5f, 0.8f, 0f));

        var canvas = new GameObject("Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 5f;
        var image = new GameObject("Image").AddComponent<SVGImage>();
        image.transform.SetParent(canvas.transform, false);
        image.vectorGraphics = AssetDatabase.LoadAssetAtPath<SVGAsset>(Root + "/gradient1.svg");
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -100f);
        rect.sizeDelta = new Vector2(100f, 100f);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static void AddRenderer(string path, Vector3 position)
    {
        var go = new GameObject(Path.GetFileNameWithoutExtension(path));
        go.transform.position = position;
        go.AddComponent<SVGRenderer>().vectorGraphics = AssetDatabase.LoadAssetAtPath<SVGAsset>(path);
    }

    static void Record(int step)
    {
#if UNITY_6000_5_OR_NEWER
        var camera = UnityEngine.Object.FindObjectsByType<Camera>().First(c => c.name == "Camera");
#else
        var camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c => c.name == "Camera");
#endif
        var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        camera.Render();
        camera.targetTexture = null;
        var pixels = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        pixels.Apply();
        RenderTexture.active = null;
        string output = SessionState.GetString(OutputKey, "Temp");
        File.WriteAllBytes(Path.Combine(output, step + " " + Steps[step] + ".png"), pixels.EncodeToPNG());
        Color32[] current = pixels.GetPixels32();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(pixels);

        string baselinePath = Path.Combine(output, "baseline.bytes");
        if (step == 0)
        {
            File.WriteAllBytes(baselinePath, current.SelectMany(c => new[] { c.r, c.g, c.b, c.a }).ToArray());
            // The layers renderer draws the same SVG as gradient2: it must match it.
            File.AppendAllText(ResultsPath, string.Format("{0} {1}: layers renderer matches the plain renderer: {2} of {3} pixels differ\n",
                LayersMatch(current) ? "PASS" : "FAIL", Steps[step], LayersDifference(current), PatchPixels));
            return;
        }
        if (Steps[step] == "edit")
        {
            // The edited SVG shows its new colours (green on the left, yellow on the right), and
            // nothing else changed.
            int cx = (int)(Width / 2f - 3f * 90f), cy = (int)(Height / 2f + 0.8f * 90f);
            Color32 left = current[cy * Width + cx - 40], right = current[cy * Width + cx + 40];
            bool edited = left.g > 200 && left.r < 80 && right.r > 200 && right.g > 200 && right.b < 80;
            File.AppendAllText(ResultsPath, string.Format("{0} {1}: the edited SVG shows its new gradient: left {2}, right {3}\n", edited ? "PASS" : "FAIL", Steps[step], left, right));
            return;
        }
        byte[] baseline = File.ReadAllBytes(baselinePath);
        int differing = 0, worst = 0, worstAt = 0;
        for (int i = 0; i < current.Length; i++)
        {
            int d = Math.Abs(current[i].r - baseline[i * 4]) + Math.Abs(current[i].g - baseline[i * 4 + 1]) + Math.Abs(current[i].b - baseline[i * 4 + 2]);
            if (d > 6) differing++;
            if (d > worst) { worst = d; worstAt = i; }
        }
        string where = worst == 0 ? "" : string.Format(" (largest difference {0} at pixel {1}: {2} instead of {3}, {4}, {5})", worst, worstAt, current[worstAt],
            baseline[worstAt * 4], baseline[worstAt * 4 + 1], baseline[worstAt * 4 + 2]);
        File.AppendAllText(ResultsPath, string.Format("{0} {1}: {2} pixels differ from the baseline{3}\n", differing == 0 ? "PASS" : "FAIL", Steps[step], differing, where));
    }

    // The renderers are 1 unit (100 px at 0.01) wide; at orthographic size 2 on 360 px, 90 px per unit.
    const int PatchPixels = 60 * 60;
    static int LayersDifference(Color32[] c)
    {
        int differing = 0;
        foreach (var (x, y) in Patch(0f))
        {
            Color32 p = c[y * Width + x], q = c[y * Width + x + (int)(1.5f * 90f)];
            if (Math.Abs(p.r - q.r) + Math.Abs(p.g - q.g) + Math.Abs(p.b - q.b) > 6) differing++;
        }
        return differing;
    }

    static bool LayersMatch(Color32[] c) => LayersDifference(c) == 0;

    static IEnumerable<(int, int)> Patch(float worldX)
    {
        int cx = (int)(Width / 2f + worldX * 90f), cy = (int)(Height / 2f + 0.8f * 90f);
        for (int y = cy - 30; y < cy + 30; y++)
            for (int x = cx - 30; x < cx + 30; x++)
                yield return (x, y);
    }
}
