// Gradient identity and evaluation: gradients are compared by what they draw, exactly, so
// different gradients never share an atlas slot, and Evaluate matches the shaders.
//
//   Unity -batchmode -projectPath . -executeMethod GradientTests.Run -out <dir>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SVGImporter;
using SVGImporter.Rendering;
using UnityEditor;
using UnityEngine;

public static class GradientTests
{
    public static void Run()
    {
        string output = Environment.GetCommandLineArgs().SkipWhile(a => a != "-out").Skip(1).FirstOrDefault() ?? "Temp/GradientTests";
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
            // The same colours with a stop 0.0004 apart: the old string hash rounded offsets (and
            // alphas) to 1/999, so these two shared one atlas slot.
            CCGradient a = Gradient(new[] { (0f, Color.red), (0.5f, Color.green), (1f, Color.blue) });
            CCGradient b = Gradient(new[] { (0f, Color.red), (0.5004f, Color.green), (1f, Color.blue) });
            Check("nearly equal gradients differ", !a.GradientEquals(b), "stop at 0.5 vs 0.5004");
            var data = new SVGAtlasData();
            data.Init(16);
            CCGradient ra = data.AddGradient(a.Clone()), rb = data.AddGradient(b.Clone());
            Check("nearly equal gradients get their own slots", ra.index != rb.index, "slots " + ra.index + " and " + rb.index);
            Check("an equal gradient finds its slot", data.AddGradient(a.Clone()).index == ra.index && data.GetGradient(b.Clone()) == rb, "");

            // The same keys in a different order describe the same gradient.
            CCGradient shuffled = new CCGradient(a.colorKeys.Reverse().ToArray(), a.alphaKeys.Reverse().ToArray());
            Check("key order doesn't matter", a.GradientEquals(shuffled), "");

            // The spread method and focal point are part of what a gradient draws.
            CCGradient reflected = a.Clone(), focused = a.Clone(), padded = a.Clone();
            reflected.spreadMethod = SVGSpreadMethod.Reflect;
            focused.focalOffset = new Vector2(0.25f, 0f);
            padded.spreadMethod = SVGSpreadMethod.Pad;
            Check("spread and focal point tell gradients apart", !a.GradientEquals(reflected) && !a.GradientEquals(focused)
                && data.AddGradient(reflected).index != ra.index && data.AddGradient(focused).index != ra.index, "");
            Check("an unset spread method is pad", a.GradientEquals(padded), "");

            // A hard stop at 0.5: red before it, blue from it on, as the shaders draw it.
            CCGradient hard = Gradient(new[] { (0f, Color.red), (0.5f, Color.red), (0.5f, Color.blue), (1f, Color.blue) });
            List<CCGradientStop> stops = hard.GetStops();
            Check("hard stops keep their order", stops.Count == 4 && stops[1].color == Color.red && stops[2].color == Color.blue,
                string.Join(", ", stops.Select(s => s.offset + " " + s.color)));
            Color32 before = hard.Evaluate(0.4999f), at = hard.Evaluate(0.5f);
            Check("Evaluate at a hard stop", before.r == 255 && before.b == 0 && at.b == 255 && at.r == 0, "0.4999: " + before + ", 0.5: " + at);

            Color32 middle = Gradient(new[] { (0f, Color.black), (1f, Color.white) }).Evaluate(0.5f);
            Check("Evaluate interpolates", Mathf.Abs(middle.r - 128) <= 1, middle.ToString());

            // Colour and alpha keys at different offsets merge into stops with both.
            var mixed = new CCGradient(
                new[] { new CCGradientColorKey(Color.black, 0f), new CCGradientColorKey(Color.white, 1f) },
                new[] { new CCGradientAlphaKey(1f, 0f), new CCGradientAlphaKey(0f, 0.5f), new CCGradientAlphaKey(1f, 1f) });
            List<CCGradientStop> merged = mixed.GetStops();
            Check("colour and alpha keys merge", merged.Count == 3 && Mathf.Abs(merged[1].color.r - 0.5f) < 1e-4f && merged[1].color.a == 0f,
                string.Join(", ", merged.Select(s => s.offset + " " + s.color)));

            // Many distinct gradients: all get their own slots.
            var many = new SVGAtlasData();
            many.Init(1024);
            var slots = new HashSet<int>();
            for (int i = 0; i < 1000; i++)
                slots.Add(many.AddGradient(Gradient(new[] { (0f, Color.black), (i / 1000f, Color.white), (1f, Color.black) })).index);
            Check("1000 distinct gradients, 1000 slots", slots.Count == 1000, slots.Count + " slots");

            // An SVG with two nearly equal gradients keeps both.
            const string path = "Assets/GradientTests.svg";
            File.WriteAllText(path, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"50\">"
                + "<linearGradient id=\"a\"><stop offset=\"0\" stop-color=\"red\"/><stop offset=\"0.5\" stop-color=\"lime\"/><stop offset=\"1\" stop-color=\"blue\"/></linearGradient>"
                + "<linearGradient id=\"b\"><stop offset=\"0\" stop-color=\"red\"/><stop offset=\"0.5004\" stop-color=\"lime\"/><stop offset=\"1\" stop-color=\"blue\"/></linearGradient>"
                + "<rect width=\"50\" height=\"50\" fill=\"url(#a)\"/><rect x=\"50\" width=\"50\" height=\"50\" fill=\"url(#b)\"/></svg>");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            SVGAsset asset = AssetDatabase.LoadAssetAtPath<SVGAsset>(path);
            int imported = asset.sharedGradients.Count(g => !SVGAtlasData.IsDefaultGradient(g));
            Check("an SVG keeps nearly equal gradients apart", imported == 2, imported + " gradients");
            AssetDatabase.DeleteAsset(path);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Check("exception", false, e.ToString());
        }

        results.Insert(0, "Unity " + Application.unityVersion + ", result: " + (passed ? "PASS" : "FAIL"));
        File.WriteAllLines(Path.Combine(output, "gradients.txt"), results);
        EditorApplication.Exit(passed ? 0 : 1);
    }

    static CCGradient Gradient((float offset, Color color)[] stops)
    {
        return new CCGradient(
            stops.Select(s => new CCGradientColorKey(s.color, s.offset)).ToArray(),
            stops.Select(s => new CCGradientAlphaKey(s.color.a, s.offset)).ToArray());
    }
}
