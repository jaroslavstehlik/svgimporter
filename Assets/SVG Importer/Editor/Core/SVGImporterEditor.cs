// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace SVGImporter
{
    public class SVGImporterEditor
    {
        protected const string assetName = "svgImporterSettings.asset";
        protected const string slash = "/";
        protected const string path = "Assets";
        protected const string gizmosPath = path+slash+"Gizmos";
        protected const string gizmosAssetIconPath = "SVGAsset icon.png";

        [MenuItem("Window/SVG Importer/Settings")]
        static public void ShowSettings () {
            Selection.activeObject = settings;
        }

        protected static SVGImporterSettings _settings;
        public static SVGImporterSettings settings
        {
            get {
                CreateSettings();
                return _settings;
            }
        }

        protected static void CreateSettings()
        {
            if(_settings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:SVGImporterSettings");
                if(guids == null || guids.Length == 0)
                {
                    ScriptableObject asset = ScriptableObject.CreateInstance<SVGImporterSettings>();                        
                    AssetDatabase.CreateAsset(asset, path + slash + assetName);
                    AssetDatabase.SaveAssets();

					_settings = (SVGImporterSettings)AssetDatabase.LoadAssetAtPath(path + assetName, typeof(SVGImporterSettings));
                    if(_settings != null)
                    {
                        EditorUtility.SetDirty(_settings);
                    }
                } else {
					_settings = (SVGImporterSettings)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), typeof(SVGImporterSettings));
                }
            }
        }
        
        public static void Init()
        {
            CreateSettings();
            InitIcons();
        }

        protected static void InitIcons()
        {
			if(!System.IO.Directory.Exists(gizmosPath))
            {
                AssetDatabase.CreateFolder(path, "Gizmos");
            }
            if(settings.defaultSVGIcon != null)
            {
				if(AssetDatabase.LoadAssetAtPath(gizmosAssetIconPath, typeof(Texture2D)) == null)
                {
                    File.WriteAllBytes(Application.dataPath + "/Gizmos/"+gizmosAssetIconPath, settings.defaultSVGIcon.EncodeToPNG());
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
