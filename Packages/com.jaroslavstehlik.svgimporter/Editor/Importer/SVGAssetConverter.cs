// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// Converts SVG Assets from SVG Importer 1.x (.asset files that embed the SVG source)
    /// into .svg files imported by SVGFileImporter.
    ///
    /// The .svg file keeps the asset's GUID, so references to it survive. Imported objects get
    /// different local file IDs than objects in an .asset file, so references in text-serialized
    /// scenes, prefabs and assets are rewritten to the new IDs.
    /// </summary>
    public static class SVGAssetConverter
    {
        public struct Result
        {
            public int converted;
            public List<string> skipped;
            public int referencesUpdated;
            public int filesUpdated;
        }

        struct Conversion
        {
            public string svgPath;
            public string guid;
            public Dictionary<string, object> settings;
            // Local file IDs in the .asset file, by the identifier SVGFileImporter uses.
            public Dictionary<string, long> oldIds;
        }

        const string k_MenuPath = "Window/SVG Importer/Convert SVG Assets to SVG Files...";

        [MenuItem(k_MenuPath)]
        static void ConvertProjectMenu()
        {
            string[] paths = FindLegacySVGAssets();
            if(paths.Length == 0)
            {
                EditorUtility.DisplayDialog("Convert SVG Assets", "There are no SVG Assets from SVG Importer 1.x in this project.", "OK");
                return;
            }
            ConvertWithDialogs(paths);
        }

        /// <summary>
        /// Converts the SVG Assets at the paths as the menu command does: it asks first, saves and
        /// closes the open scenes, and reports the result.
        /// </summary>
        internal static void ConvertWithDialogs(string[] paths)
        {
            if(EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                EditorUtility.DisplayDialog("Convert SVG Assets",
                    "References can only be updated in text-serialized assets. Set Project Settings > Editor > Asset Serialization > Mode to Force Text, then run the conversion again.", "OK");
                return;
            }

            if(!EditorUtility.DisplayDialog("Convert SVG Assets",
                "Convert " + paths.Length + " SVG Asset(s) to .svg files?\n\n" +
                "Each .asset file is replaced by an .svg file with the same import settings, and references in scenes, prefabs and assets are updated.\n\n" +
                "Commit or back up your project first: this cannot be undone.", "Convert", "Cancel"))
            {
                return;
            }

            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Close the open scenes so they do not hold on to the SVG Assets being replaced.
            List<string> openScenes = GetOpenScenes();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Result result = Convert(paths);
            OpenScenes(openScenes);
            string message = "Converted " + result.converted + " SVG Asset(s).\nUpdated " + result.referencesUpdated + " reference(s) in " + result.filesUpdated + " file(s).";
            if(result.skipped.Count > 0)
            {
                message += "\n\nSkipped, because they do not contain their SVG source (Keep SVG File was off and the .meta file lost it):\n" + string.Join("\n", result.skipped);
            }
            EditorUtility.DisplayDialog("Convert SVG Assets", message, "OK");
        }

        /// <summary>All SVG Assets stored as .asset files under Assets.</summary>
        public static string[] FindLegacySVGAssets()
        {
            return AssetDatabase.FindAssets("t:SVGAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToArray();
        }

        public static Result Convert(IList<string> assetPaths)
        {
            var result = new Result { skipped = new List<string>() };
            var conversions = new List<Conversion>();

            // 1. Read everything we need while the .asset files still exist.
            foreach(string assetPath in assetPaths)
            {
                SVGAsset asset = AssetDatabase.LoadAssetAtPath<SVGAsset>(assetPath);
                string svgText = asset != null ? asset.svgFile : null;
                if(string.IsNullOrEmpty(svgText))
                {
                    result.skipped.Add(assetPath);
                    continue;
                }

                var conversion = new Conversion {
                    guid = AssetDatabase.AssetPathToGUID(assetPath),
                    settings = SVGFileImporter.ReadSettings(asset),
                    oldIds = GetLocalIds(assetPath),
                    svgPath = GetFreePath(Path.ChangeExtension(assetPath, ".svg")),
                };
                conversions.Add(conversion);

                File.WriteAllText(conversion.svgPath, svgText);
            }

            // 2. Replace each .asset by its .svg file under the same GUID. Unload the old objects
            // first: Unity would otherwise try to reload them from the new file as native assets.
            foreach(Conversion conversion in conversions)
            {
                foreach(Object obj in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(conversion.guid)))
                {
                    if(obj != null) Resources.UnloadAsset(obj);
                }
            }
            foreach(Conversion conversion in conversions)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(conversion.guid);
                File.Delete(assetPath);
                File.Delete(assetPath + ".meta");
                // The .svg file takes over the GUID. Until SVG Importer is assigned, it is a plain file.
                File.WriteAllText(conversion.svgPath + ".meta",
                    "fileFormatVersion: 2\nguid: " + conversion.guid + "\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 3. Import with SVG Importer and the original settings.
            var idMaps = new Dictionary<string, Dictionary<long, long>>();
            foreach(Conversion conversion in conversions)
            {
                if(!SVGPostprocessor.UseSVGImporter(conversion.svgPath))
                    continue;
                var importer = (SVGFileImporter)AssetImporter.GetAtPath(conversion.svgPath);
                importer.WriteSettings(conversion.settings);
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                result.converted++;

                Dictionary<string, long> newIds = GetLocalIds(conversion.svgPath);
                var idMap = new Dictionary<long, long>();
                foreach(var oldId in conversion.oldIds)
                {
                    long newId;
                    if(newIds.TryGetValue(oldId.Key, out newId)) idMap[oldId.Value] = newId;
                }
                idMaps[conversion.guid] = idMap;
            }

            // 4. Point references at the imported objects.
            if(idMaps.Count > 0)
            {
                UpdateReferences(idMaps, ref result);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            return result;
        }

        /// <summary>Local file IDs of the asset's objects, keyed like SVGFileImporter's identifiers.</summary>
        static Dictionary<string, long> GetLocalIds(string path)
        {
            var ids = new Dictionary<string, long>();
            foreach(Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                string key = obj is SVGAsset ? "main" : obj is Mesh ? "mesh" : obj is SVGDocumentAsset ? "document" : null;
                string guid;
                long localId;
                if(key != null && !ids.ContainsKey(key) && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out localId))
                {
                    ids[key] = localId;
                }
            }
            return ids;
        }

        static string GetFreePath(string path)
        {
            if(!File.Exists(path)) return path;
            string directory = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            for(int i = 1; ; i++)
            {
                string candidate = Path.Combine(directory, name + " " + i + ".svg").Replace('\\', '/');
                if(!File.Exists(candidate)) return candidate;
            }
        }

        // Unity wraps long references over two lines, so any whitespace may separate the parts.
        static readonly Regex s_Reference = new Regex(@"\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]{32}),\s*type:\s*(\d+)\}", RegexOptions.Compiled);

        static void UpdateReferences(Dictionary<string, Dictionary<long, long>> idMaps, ref Result result)
        {
            int referencesUpdated = 0;
            foreach(string file in Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories))
            {
                if(file.EndsWith(".meta") || !IsTextAsset(file))
                    continue;

                string text = File.ReadAllText(file);
                if(!idMaps.Keys.Any(text.Contains))
                    continue;

                int count = 0;
                string updated = s_Reference.Replace(text, match => {
                    Dictionary<long, long> idMap;
                    long newId;
                    if(idMaps.TryGetValue(match.Groups[2].Value, out idMap) && idMap.TryGetValue(long.Parse(match.Groups[1].Value), out newId))
                    {
                        count++;
                        // type 3: an object produced by an importer, rather than stored in the file (type 2).
                        return "{fileID: " + newId + ", guid: " + match.Groups[2].Value + ", type: 3}";
                    }
                    return match.Value;
                });

                if(count > 0)
                {
                    File.WriteAllText(file, updated);
                    referencesUpdated += count;
                    result.filesUpdated++;
                }
            }
            result.referencesUpdated = referencesUpdated;
        }

        static bool IsTextAsset(string file)
        {
            using(var stream = File.OpenRead(file))
            {
                var header = new byte[5];
                return stream.Read(header, 0, 5) == 5 && Encoding.ASCII.GetString(header) == "%YAML";
            }
        }

        static List<string> GetOpenScenes()
        {
            var openScenes = new List<string>();
            for(int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                string path = EditorSceneManager.GetSceneAt(i).path;
                if(!string.IsNullOrEmpty(path)) openScenes.Add(path);
            }
            return openScenes;
        }

        static void OpenScenes(List<string> scenes)
        {
            for(int i = 0; i < scenes.Count; i++)
            {
                EditorSceneManager.OpenScene(scenes[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
            }
        }
    }
}
