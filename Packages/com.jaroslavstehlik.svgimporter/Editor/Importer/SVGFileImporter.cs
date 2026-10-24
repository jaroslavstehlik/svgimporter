// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// Imports an .svg file as an SVGAsset, the way a texture is imported as a Sprite:
    /// the .svg file stays in the project and its import settings live in its .meta file.
    ///
    /// Registered as an override importer for .svg, because Unity's own Vector Graphics
    /// importer also claims the extension. SVGPostprocessor assigns it to new files.
    /// </summary>
    [ScriptedImporter(2, null, new[] { "svg" })]
    public class SVGFileImporter : ScriptedImporter
    {
        // The field names match SVGAsset's serialized fields, so the settings are copied by
        // name (see ApplySettings) and drawn by the same GUI as legacy SVG Assets.
        [SerializeField] internal SVGAssetFormat _format = SVGAssetFormat.Transparent;
        [SerializeField] internal SVGUseGradients _useGradients = SVGUseGradients.Auto;
        [SerializeField] internal bool _antialiasing = true;
        [SerializeField] internal SVGMeshCompression _meshCompression = SVGMeshCompression.Off;
        [SerializeField] internal bool _optimizeMesh = true;
        [SerializeField] internal float _scale = 0.01f;
        [SerializeField] internal float _vpm = 1000f;
        [SerializeField] internal float _depthOffset = 0.01f;
        [SerializeField] internal bool _compressDepth = true;
        [SerializeField] internal bool _customPivotPoint = false;
        [SerializeField] internal Vector2 _pivotPoint = new Vector2(0.5f, 0.5f);
        [SerializeField] internal Vector4 _border = Vector4.zero;
        [SerializeField] internal bool _sliceMesh = false;
        [SerializeField] internal bool _generateCollider = false;
        [SerializeField] internal bool _generateNormals = false;
        [SerializeField] internal bool _generateTangents = false;
        [SerializeField] internal bool _keepSVGFile = false;
        [SerializeField] internal bool _useLayers = false;
        [SerializeField] internal bool _ignoreSVGCanvas = true;

        static readonly FieldInfo[] s_SettingFields = typeof(SVGFileImporter).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        const BindingFlags k_AssetFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Reimport when the Import Engine setting changes.
            ctx.DependsOnCustomDependency(SVGImportEngineDependency.name);

            SVGAsset asset = ScriptableObject.CreateInstance<SVGAsset>();
            asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ApplySettings(this, asset);

            asset._editor_Import(File.ReadAllText(ctx.assetPath), (identifier, obj) => ctx.AddObjectToAsset(identifier, obj));

            SVGError[] errors = asset._editor_errors;
            if(errors != null)
            {
                foreach(SVGError error in errors)
                {
                    if(error == SVGError.CorruptedFile || error == SVGError.Syntax)
                        ctx.LogImportError("SVG Importer: " + error, asset);
                }
            }

            ctx.AddObjectToAsset("main", asset);
            ctx.SetMainObject(asset);
        }

        /// <summary>Copies every setting field to the field with the same name on the target.</summary>
        internal static void ApplySettings(SVGFileImporter importer, SVGAsset asset)
        {
            foreach(FieldInfo field in s_SettingFields)
            {
                if(!field.Name.StartsWith("_")) continue;
                FieldInfo assetField = typeof(SVGAsset).GetField(field.Name, k_AssetFieldFlags);
                if(assetField != null) assetField.SetValue(asset, field.GetValue(importer));
            }
        }

        /// <summary>Reads the import settings of a legacy SVG Asset, to convert it to an .svg file.</summary>
        internal static Dictionary<string, object> ReadSettings(SVGAsset asset)
        {
            var settings = new Dictionary<string, object>();
            foreach(FieldInfo field in s_SettingFields)
            {
                if(!field.Name.StartsWith("_")) continue;
                FieldInfo assetField = typeof(SVGAsset).GetField(field.Name, k_AssetFieldFlags);
                if(assetField != null) settings[field.Name] = assetField.GetValue(asset);
            }
            return settings;
        }

        internal void WriteSettings(Dictionary<string, object> settings)
        {
            foreach(FieldInfo field in s_SettingFields)
            {
                object value;
                if(settings.TryGetValue(field.Name, out value)) field.SetValue(this, value);
            }
        }

        /// <summary>Applies the project's default import settings to a newly assigned importer.</summary>
        internal void ApplyDefaults(SVGImporterSettings settings)
        {
            _format = settings.defaultSVGFormat;
            _useGradients = settings.defaultUseGradients;
            _antialiasing = settings.defaultAntialiasing;
            _meshCompression = settings.defaultMeshCompression;
            _optimizeMesh = settings.defaultOptimizeMesh;
            _scale = settings.defaultScale;
            _vpm = settings.defaultVerticesPerMeter;
            _depthOffset = settings.defaultDepthOffset;
            _compressDepth = settings.defaultCompressDepth;
            _customPivotPoint = settings.defaultCustomPivotPoint;
            _pivotPoint = settings.defaultPivotPoint;
            _generateCollider = settings.defaultGenerateCollider;
            _generateNormals = settings.defaultGenerateNormals;
            _generateTangents = settings.defaultGenerateTangents;
            _keepSVGFile = settings.defaultKeepSVGFile;
            _useLayers = settings.defaultUseLayers;
            _ignoreSVGCanvas = settings.defaultIgnoreSVGCanvas;
        }
    }
}
