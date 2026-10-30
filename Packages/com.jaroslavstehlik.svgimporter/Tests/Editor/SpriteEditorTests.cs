// SVG files in Unity's Sprite Editor (2D Sprite package): the data provider shows the SVG and
// writes the pivot and border back to the import settings.
//
//   Unity -batchmode -projectPath . -executeMethod SpriteEditorTests.Run -out <dir>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SVGImporter;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class SpriteEditorTests
{
    const string Path = "Assets/SpriteEditorTests.svg";
    // A 200 x 100 rectangle, red in its top half and blue in its bottom half.
    const string Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"100\"><rect width=\"200\" height=\"50\" fill=\"red\"/>"
        + "<rect y=\"50\" width=\"200\" height=\"50\" fill=\"blue\"/></svg>";

    public static void Run()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/SpriteEditorTests";
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
            File.WriteAllText(Path, Svg);
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceSynchronousImport);

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(AssetDatabase.LoadMainAssetAtPath(Path));
            Check("the Sprite Editor finds SVG files", provider != null, provider == null ? "no provider" : provider.GetType().Name);
            provider.InitSpriteEditorDataProvider();

            var textures = provider.GetDataProvider<ITextureDataProvider>();
            Texture2D texture = textures.texture;
            // Texture rows go bottom up: red belongs at the top.
            Color top = texture != null ? texture.GetPixel(texture.width / 2, texture.height * 3 / 4) : Color.clear;
            Color bottom = texture != null ? texture.GetPixel(texture.width / 2, texture.height / 4) : Color.clear;
            Check("it shows the SVG the right way up", texture != null && texture.width == 1024 && texture.height == 512 && top.r > 0.9f && top.b < 0.1f && bottom.b > 0.9f && bottom.r < 0.1f,
                texture == null ? "no texture" : texture.width + " x " + texture.height + ", top " + top + ", bottom " + bottom);

#if UNITY_6000_6_OR_NEWER
            // Newer Sprite Editors lock what the provider doesn't allow.
            var capability = provider.GetDataProvider<ISpriteFrameEditCapability>();
            EditCapability allowed = capability != null ? capability.GetEditCapability() : default(EditCapability);
            Check("pivot and border can be edited", capability != null && allowed.HasCapability(EEditCapability.EditPivot) && allowed.HasCapability(EEditCapability.EditBorder),
                capability == null ? "no edit capability" : "pivot " + allowed.HasCapability(EEditCapability.EditPivot) + ", border " + allowed.HasCapability(EEditCapability.EditBorder));
#endif

            SpriteRect rect = provider.GetSpriteRects()[0];
            Check("the default pivot is the centre", rect.alignment == SpriteAlignment.Center && rect.pivot == new Vector2(0.5f, 0.5f), rect.alignment + " " + rect.pivot);

            // Edited in the Sprite Editor: a custom pivot near the bottom left, a 100-pixel left border.
            rect.alignment = SpriteAlignment.Custom;
            rect.pivot = new Vector2(0.25f, 0.1f);
            rect.border = new Vector4(100f, 0f, 0f, 0f);
            provider.SetSpriteRects(new[] { rect });
            provider.Apply();
            ((AssetImporter)provider.targetObject).SaveAndReimport();

            SVGAsset asset = AssetDatabase.LoadAssetAtPath<SVGAsset>(Path);
            Check("the pivot is stored with y down", asset.customPivotPoint && Vector2.Distance(asset.pivotPoint, new Vector2(0.25f, 0.9f)) < 1e-4f, asset.pivotPoint.ToString("F3"));
            Check("the border is stored as a fraction", Mathf.Abs(asset.border.x - 100f / 1024f) < 1e-4f, asset.border.ToString("F4"));
            // The mesh follows: the pivot is the origin, a quarter from the left and a tenth from the bottom.
            Bounds bounds = asset.sharedMesh.bounds;
            Check("the mesh is placed around the pivot", Mathf.Abs(bounds.min.x + 0.5f) < 0.01f && Mathf.Abs(bounds.min.y + 0.1f) < 0.01f,
                "bounds min " + bounds.min.ToString("F3"));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Check("exception", false, exception.ToString());
        }
        finally
        {
            AssetDatabase.DeleteAsset(Path);
        }

        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(System.IO.Path.Combine(output, "sprite-editor.txt"), results);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
