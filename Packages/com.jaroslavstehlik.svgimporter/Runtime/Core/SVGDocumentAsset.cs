

using UnityEngine;
using UnityEngine.Serialization;

using System.Collections;

namespace SVGImporter 
{
    public enum SVGError
    {
        None = 0,
        Syntax = 1,
        CorruptedFile = 2,
        // 3 and 4 were clip paths and symbols, which are now imported.
        Image = 5,
        Mask = 6,
        Unknown = 7,
        // Reported by the native import engine.
        Filter = 8,
        Pattern = 9,
        BlendMode = 10,
        GroupOpacity = 11,
        TextSkipped = 12,
    }

    public sealed class SVGDocumentAsset : ScriptableObject {

        [FormerlySerializedAs("errors")]
        [SerializeField]
        private SVGError[] _errors;
        public SVGError[] errors
        {
            get {
                return _errors;
            }
            set {
                _errors = value;
            }
        }

        [FormerlySerializedAs("svgFile")]
        [SerializeField]
        private string _svgFile;
        public string svgFile
        {
            get {
#if UNITY_EDITOR
                if(string.IsNullOrEmpty(_svgFile))
                {
                    var svgAssetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
                    // Imported from an .svg file: the file itself is the source.
                    if(svgAssetPath.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(svgAssetPath))
                        return System.IO.File.ReadAllText(svgAssetPath);
                    var svgAssetImporter = UnityEditor.AssetImporter.GetAtPath(svgAssetPath);
                    return svgAssetImporter != null ? svgAssetImporter.userData : null;
                }
#endif
                return _svgFile;
            }
            set {
                _svgFile = value;
            }
        }

        [FormerlySerializedAs("title")]
        [SerializeField]
        private string _title;
        public string title
        {
            get {
                return _title;
            }
            set {
                _title = value;
            }
        }

        [FormerlySerializedAs("description")]
        [SerializeField]
        private string _description;
        public string description
        {
            get {
                return _description;
            }
            set {
                _description = value;
            }
        }

        internal static SVGDocumentAsset CreateInstance(string svgFile, SVGError[] errors = null, string title = null, string description = null)
        {
            SVGDocumentAsset svgDocumentAsset = ScriptableObject.CreateInstance<SVGDocumentAsset>();
            svgDocumentAsset._description = description;
            svgDocumentAsset._title = title;
            svgDocumentAsset._svgFile = svgFile;
            svgDocumentAsset._errors = errors;
            return svgDocumentAsset;
        }
    }
}