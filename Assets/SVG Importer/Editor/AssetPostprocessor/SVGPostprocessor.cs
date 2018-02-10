// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace SVGImporter 
{
    public class SVGPostprocessor : AssetPostprocessor
    {
        const string SVG_IMPORTER_POSTPROCESSOR_KEY = "SVG_IMPORTER_POSTPROCESSOR_KEY";
        static bool _active = false;
        public static bool active
        {
            get {
                if(!EditorPrefs.HasKey(SVG_IMPORTER_POSTPROCESSOR_KEY))
                {
                    EditorPrefs.SetBool(SVG_IMPORTER_POSTPROCESSOR_KEY, true);
                    _active = true;
                } else {
                    _active = EditorPrefs.GetBool(SVG_IMPORTER_POSTPROCESSOR_KEY);
                }

                return _active;
            }
            set {
                EditorPrefs.SetBool(SVG_IMPORTER_POSTPROCESSOR_KEY, value);
                _active = value;
            }
        }

        public static void Init()
        {
            if(!EditorPrefs.HasKey(SVG_IMPORTER_POSTPROCESSOR_KEY))
            {
                EditorPrefs.SetBool(SVG_IMPORTER_POSTPROCESSOR_KEY, true);
                _active = true;
            } else {
                _active = EditorPrefs.GetBool(SVG_IMPORTER_POSTPROCESSOR_KEY);
            }
        }

        public static void Start() { active = true; }
        public static void Stop() { active = false; }

        static string svgExtension = ".svg";        // Our custom svgExtension
        static string assetExtension = ".asset";        // svgExtension of newly created asset - it MUST be ".asset", nothing else is allowed...
        private const uint _version = 1;

        public override uint GetVersion()
        {
            return _version;
        }

        public override int GetPostprocessOrder()
        {
            return base.GetPostprocessOrder();
        }

        public static bool HasExtension(string asset, string extension)
        {
            return asset.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase);
        }
        
        public static string ConvertToInternalPath(string asset)
        {
            string left = asset.Substring(0, asset.Length - svgExtension.Length);
            return left + assetExtension;
        }
        
        // This is called always when importing something
        static void OnPostprocessAllAssets
            (
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths
            )
        {
            if(!active)
                return;

			// Hotfix: Joonas Nissinen 13.6.16 find all already imported assets and
			// remove them from the imported assets array
			string[] svgs = Array.FindAll (importedAssets, str => str.EndsWith (".svg"));
			string[] assets = Array.FindAll (importedAssets, str => str.EndsWith (".asset"));
			importedAssets = svgs.Where (str => !assets.Contains (str.Replace (".svg", ".asset"))).ToArray ();

			if (importedAssets.Length == 0)
				return;

            int importTotalAssets = 0;
            int reImportTotalAssets = 0;

            List<string> importSVGAssets = new List<string>();
            List<SVGAsset> reimportSVGAssets = new List<SVGAsset>();

            foreach(string asset in importedAssets)
            {
                if(HasExtension(asset, svgExtension))
                {
                    importSVGAssets.Add(asset);
                } else if(HasExtension(asset, assetExtension))
                {
                    SVGAsset svgAsset = AssetDatabase.LoadAssetAtPath(asset, typeof(SVGAsset)) as SVGAsset;
                    if(svgAsset != null)
                    {                        
                        reimportSVGAssets.Add(svgAsset);
                    }
                }
            }

            importTotalAssets = importSVGAssets.Count;
            reImportTotalAssets = reimportSVGAssets.Count;

			if(importTotalAssets + reImportTotalAssets > 0)
			{				
				AssetDatabase.StartAssetEditing();
			} else {
				EditorUtility.ClearProgressBar();
				importSVGAssets.Clear();
				importSVGAssets = null;
				reimportSVGAssets.Clear();
				reimportSVGAssets = null;
				return;
			}

            //Debug.Log("importedAssets: " + importedAssets.Length +"\n"+string.Join("\n", importSVGAssets.ToArray()));
//            Debug.Log("importedAssets: " + importedAssets.Length +"\n"+string.Join("\n", importedAssets));

            // Import SVG Assets
			int importSVGAssetsLength = importSVGAssets.Count;
			string importSVGAsset;
			int currentAssetIndex = 0;
			for(int i = 0; i < importSVGAssetsLength; i++)
            {
				importSVGAsset = importSVGAssets[i];                
                float importProgress = (float)currentAssetIndex / (float)importTotalAssets;
				if(EditorUtility.DisplayCancelableProgressBar("Importing SVG Assets", "Importing SVG Asset: "+importSVGAsset+"...", importProgress))
                {
                    EditorUtility.ClearProgressBar();
                    break;
                } else {
					ImportVGAsset(importSVGAsset);
                    currentAssetIndex++;
                }
            }

            importSVGAssets.Clear();
            importSVGAssets = null;
            /*
            // Reimport SVG Assets
            int currentAssetIndex = 0;
            foreach(SVGAsset svgAsset in reimportSVGAssets)
            {
                string asset = AssetDatabase.GetAssetPath(svgAsset);
                float importProgress = (float)currentAssetIndex / (float)importTotalAssets;
                if(EditorUtility.DisplayCancelableProgressBar("Reimporting SVG Assets", "Reimporting SVG Asset: "+asset+"...", importProgress))
                {
                    EditorUtility.ClearProgressBar();
                    break;
                } else {
                    ReimportSVGAsset(svgAsset, asset);
                    currentAssetIndex++;
                }
            }
            */
            EditorUtility.ClearProgressBar();
            reimportSVGAssets.Clear();
            reimportSVGAssets = null;

			AssetDatabase.SaveAssets();
			AssetDatabase.StopAssetEditing();
			AssetDatabase.Refresh();
			Canvas.ForceUpdateCanvases();
			SceneView.RepaintAll();
        }

        static void ReimportSVGAsset(SVGAsset svgAsset, string asset)
        {
            MethodInfo _editor_ApplyChanges = typeof(SVGAsset).GetMethod("_editor_ApplyChanges", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_ApplyChanges.Invoke(svgAsset, new object[]{true});
        }

        // Imports my asset from the file
        static void ImportVGAsset(string asset)
        {
            string directoryName = Path.GetDirectoryName(asset);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(asset);
            string outputFileName = directoryName+"/"+fileNameWithoutExtension+".asset";

            string svgText;
            using (StreamReader sr = new StreamReader(asset))
            {
                svgText = sr.ReadToEnd();
            }

            SVGAsset svgAsset = AssetDatabase.LoadAssetAtPath(outputFileName, typeof(SVGAsset)) as SVGAsset;
            if(svgAsset == null)
            {
                svgAsset = ScriptableObject.CreateInstance<SVGAsset>();
                InitDefaultValues(svgAsset);
                AssetDatabase.CreateAsset(svgAsset, outputFileName);
            }
            AssetDatabase.DeleteAsset(asset);
            
			FieldInfo _svgFile = typeof(SVGAsset).GetField("_svgFile", BindingFlags.NonPublic | BindingFlags.Instance);
			_svgFile.SetValue(svgAsset, svgText);

            MethodInfo _editor_ApplyChanges = typeof(SVGAsset).GetMethod("_editor_ApplyChanges", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_ApplyChanges.Invoke(svgAsset, new object[]{true});
        }

        protected static void InitDefaultValues(SVGAsset asset)
        {
            FieldInfo _editor_format = typeof(SVGAsset).GetField("_format", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_format.SetValue(asset, SVGImporterEditor.settings.defaultSVGFormat);

            FieldInfo _editor_useGradients = typeof(SVGAsset).GetField("_useGradients", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_useGradients.SetValue(asset, SVGImporterEditor.settings.defaultUseGradients);

            FieldInfo _editor_antialiasing = typeof(SVGAsset).GetField("_antialiasing", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_antialiasing.SetValue(asset, SVGImporterEditor.settings.defaultAntialiasing);

			FieldInfo _editor_meshCompression = typeof(SVGAsset).GetField("_meshCompression", BindingFlags.NonPublic | BindingFlags.Instance);
			_editor_meshCompression.SetValue(asset, SVGImporterEditor.settings.defaultMeshCompression);

            FieldInfo _editor_vpm = typeof(SVGAsset).GetField("_vpm", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_vpm.SetValue(asset, SVGImporterEditor.settings.defaultVerticesPerMeter);

            FieldInfo _editor_scale = typeof(SVGAsset).GetField("_scale", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_scale.SetValue(asset, SVGImporterEditor.settings.defaultScale);

            FieldInfo _editor_depthOffset = typeof(SVGAsset).GetField("_depthOffset", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_depthOffset.SetValue(asset, SVGImporterEditor.settings.defaultScale);

            FieldInfo _editor_compressDepth = typeof(SVGAsset).GetField("_compressDepth", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_compressDepth.SetValue(asset, SVGImporterEditor.settings.defaultCompressDepth);
            
            FieldInfo _editor_customPivotPoint = typeof(SVGAsset).GetField("_customPivotPoint", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_customPivotPoint.SetValue(asset, SVGImporterEditor.settings.defaultCustomPivotPoint);

            FieldInfo _editor_pivotPoint = typeof(SVGAsset).GetField("_pivotPoint", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_pivotPoint.SetValue(asset, SVGImporterEditor.settings.defaultPivotPoint);

            FieldInfo _editor_generateCollider = typeof(SVGAsset).GetField("_generateCollider", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_generateCollider.SetValue(asset, SVGImporterEditor.settings.defaultGenerateCollider);

			FieldInfo _editor_keepSVGFile = typeof(SVGAsset).GetField("_keepSVGFile", BindingFlags.NonPublic | BindingFlags.Instance);
			_editor_keepSVGFile.SetValue(asset, SVGImporterEditor.settings.defaultKeepSVGFile);

			FieldInfo _editor_useLayers = typeof(SVGAsset).GetField("_useLayers", BindingFlags.NonPublic | BindingFlags.Instance);
			_editor_useLayers.SetValue(asset, SVGImporterEditor.settings.defaultUseLayers);

            FieldInfo _editor_ignoreSVGCanvas = typeof(SVGAsset).GetField("_ignoreSVGCanvas", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_ignoreSVGCanvas.SetValue(asset, SVGImporterEditor.settings.defaultIgnoreSVGCanvas);

            FieldInfo _editor_optimizeMesh = typeof(SVGAsset).GetField("_optimizeMesh", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_optimizeMesh.SetValue(asset, SVGImporterEditor.settings.defaultOptimizeMesh);

            FieldInfo _editor_generateNormals = typeof(SVGAsset).GetField("_generateNormals", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_generateNormals.SetValue(asset, SVGImporterEditor.settings.defaultGenerateNormals);

            FieldInfo _editor_generateTangents = typeof(SVGAsset).GetField("_generateTangents", BindingFlags.NonPublic | BindingFlags.Instance);
            _editor_generateTangents.SetValue(asset, SVGImporterEditor.settings.defaultGenerateTangents);

        }

        const string assetIconPath = "Assets/Gizmos/SVGAsset icon.png";
        void OnPreprocessTexture() {
            if(assetPath != assetIconPath)
                return;
            
            TextureImporter textureImporter  = (TextureImporter) assetImporter;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.alphaIsTransparency = true;
            textureImporter.anisoLevel = 0;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.isReadable = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.spriteImportMode = SpriteImportMode.None;
            textureImporter.textureFormat = TextureImporterFormat.ARGB32;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
        }

        private static void ForcedImportFor(string newPath)
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                AssetDatabase.ImportAsset(newPath);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}