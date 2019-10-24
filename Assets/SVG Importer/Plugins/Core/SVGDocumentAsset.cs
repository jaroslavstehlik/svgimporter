// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
        ClipPath = 3,
        Symbol = 4,
        Image = 5,
        Mask = 6,
        Unknown = 7
    }

    public class SVGDocumentAsset : ScriptableObject {

        [FormerlySerializedAs("errors")]
        [SerializeField]
        protected SVGError[] _errors;
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
        protected string _svgFile;
        public string svgFile
        {
            get {
#if UNITY_EDITOR
                if(string.IsNullOrEmpty(_svgFile))
                {
                    var svgAssetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
                    var svgAssetImporter = UnityEditor.AssetImporter.GetAtPath(svgAssetPath);
                    return svgAssetImporter.userData;
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
        protected string _title;
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
        protected string _description;
        public string description
        {
            get {
                return _description;
            }
            set {
                _description = value;
            }
        }

        public static SVGDocumentAsset CreateInstance(string svgFile, SVGError[] errors = null, string title = null, string description = null)
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