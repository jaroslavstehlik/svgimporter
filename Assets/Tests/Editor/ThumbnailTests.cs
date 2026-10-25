// Project window thumbnails of SVG files with gradients. In 2D mode, Unity 6 renders them in an
// import worker process (PreviewImporter), where the gradient atlas has to be built and bound by
// the preview itself; they used to come out white there.
//
// Assets/Tests/ThumbnailTests/Fresh.svg (the same SVG, assigned to SVG Importer by its .meta) also
// tests a project's first import when the project is opened with an empty Library: SVG files can
// import before the package's shaders, and used to be saved without shaders and draw nothing.
//
//   Unity -batchmode -projectPath . -executeMethod ThumbnailTests.Run -out <dir>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SVGImporter;
using UnityEditor;
using UnityEngine;

public static class ThumbnailTests
{
    const string Path = "Assets/ThumbnailTests.svg";
    const string FreshPath = "Assets/Tests/ThumbnailTests/Fresh.svg";
    // A radial gradient from red in the middle to blue at the edge.
    public const string Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\">"
        + "<radialGradient id=\"g\"><stop offset=\"0\" stop-color=\"red\"/><stop offset=\"1\" stop-color=\"blue\"/></radialGradient>"
        + "<circle cx=\"50\" cy=\"50\" r=\"50\" fill=\"url(#g)\"/></svg>";

    static string s_Output;
    static EditorBehaviorMode s_Mode;
    static int s_Frames;

    public static void Run()
    {
        s_Output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/ThumbnailTests";
        Directory.CreateDirectory(s_Output);
        s_Mode = EditorSettings.defaultBehaviorMode;
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        File.WriteAllText(Path, Svg);
        AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
        // Thumbnails are rendered asynchronously: wait for it in the Editor's update loop.
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        var paths = new List<string> { Path };
        if (File.Exists(FreshPath)) paths.Add(FreshPath);
        var thumbnails = paths.Select(p => AssetPreview.GetAssetPreview(AssetDatabase.LoadMainAssetAtPath(p))).ToList();
        if (thumbnails.Contains(null) && ++s_Frames < 3000) return;
        EditorApplication.update -= Poll;

        var results = new List<string>();
        bool passed = true;
        for (int i = 0; i < paths.Count; i++)
        {
            string name = paths[i] == FreshPath ? "imported with the project" : "imported later";
            Texture2D thumbnail = thumbnails[i];
            if (thumbnail == null)
            {
                passed = false;
                results.Add("FAIL " + name + ": no thumbnail after " + s_Frames + " frames");
                continue;
            }
            var copy = new Texture2D(thumbnail.width, thumbnail.height, TextureFormat.RGBA32, false);
            copy.SetPixels(thumbnail.GetPixels());
            copy.Apply();
            File.WriteAllBytes(System.IO.Path.Combine(s_Output, System.IO.Path.GetFileNameWithoutExtension(paths[i]) + ".png"), copy.EncodeToPNG());
            Color centre = copy.GetPixel(copy.width / 2, copy.height / 2);
            Color edge = copy.GetPixel(copy.width / 2, copy.height / 2 + copy.height * 3 / 10);
            // A broken thumbnail is empty, or the first stop's colour everywhere: red at the edge too.
            bool gradient = centre.r > 0.6f && centre.b < 0.4f && edge.b > edge.r + 0.2f;
            passed &= gradient;
            results.Add((gradient ? "PASS" : "FAIL") + " " + name + ": the thumbnail shows its gradient: centre " + centre + ", towards the edge " + edge);
        }

        EditorSettings.defaultBehaviorMode = s_Mode;
        AssetDatabase.DeleteAsset(Path);
        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(System.IO.Path.Combine(s_Output, "thumbnail.txt"), results);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
