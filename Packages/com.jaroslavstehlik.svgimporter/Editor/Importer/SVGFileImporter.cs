// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [ScriptedImporter(9, null, new[] { "svg" })]
    public class SVGFileImporter : ScriptedImporter
    {
        // The field names match SVGAsset's serialized fields, so that the same GUI draws both and
        // the 1.x converter can copy settings by name.
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
            // Reimport when the engine changes (Project Settings > SVG Importer).
            ctx.DependsOnCustomDependency(SVGImportEngineDependency.name);

            SVGImportOptions options = importOptions;
            string svgText = File.ReadAllText(ctx.assetPath);
            SVGImportResult result = SVGImport.Import(svgText, options);
            string name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            var document = SVGDocumentAsset.CreateInstance(options.keepSVGFile ? svgText : null, result.errors.ToArray());
            document.name = name + " Document";
            document.hideFlags = HideFlags.HideInHierarchy;
            ctx.AddObjectToAsset("document", document);

            if (result.mesh != null)
            {
                result.mesh.name = name;
                result.mesh.hideFlags = HideFlags.HideInHierarchy;
                MeshUtility.SetMeshCompression(result.mesh, MeshCompression(options.meshCompression));
                ctx.AddObjectToAsset("mesh", result.mesh);
            }

            SVGAsset asset = ScriptableObject.CreateInstance<SVGAsset>();
            asset.name = name;
            asset.Apply(result, options, document);
            ReportDiagnostics(ctx, result, asset);
            ctx.AddObjectToAsset("main", asset);
            ctx.SetMainObject(asset);
        }

        /// <summary>The import settings as options for SVGImport.</summary>
        internal SVGImportOptions importOptions
        {
            get {
                return new SVGImportOptions {
                    format = _format, useGradients = _useGradients, antialiasing = _antialiasing,
                    verticesPerMeter = _vpm, scale = _scale, depthOffset = _depthOffset, compressDepth = _compressDepth,
                    customPivotPoint = _customPivotPoint, pivotPoint = _pivotPoint, border = _border, sliceMesh = _sliceMesh,
                    ignoreSVGCanvas = _ignoreSVGCanvas, generateCollider = _generateCollider, useLayers = _useLayers,
                    generateNormals = _generateNormals, generateTangents = _generateTangents,
                    meshCompression = _meshCompression, optimizeMesh = _optimizeMesh, keepSVGFile = _keepSVGFile,
                };
            }
        }

        // Logged through the import context, so that they show again when the import comes from the cache.
        static void ReportDiagnostics(AssetImportContext ctx, SVGImportResult result, SVGAsset asset)
        {
            foreach (string message in result.messages)
            {
                if (result.succeeded) ctx.LogImportWarning("SVG Importer: " + message, asset);
                else ctx.LogImportError("SVG Importer: " + message, asset);
            }
            var unsupported = result.errors.Where(e => e != SVGError.Syntax && e != SVGError.CorruptedFile).Distinct().ToArray();
            if (unsupported.Length > 0)
                ctx.LogImportWarning("SVG Importer: not supported in " + Path.GetFileName(ctx.assetPath) + ": " + string.Join(", ", unsupported), asset);
        }

        static ModelImporterMeshCompression MeshCompression(SVGMeshCompression compression)
        {
            switch (compression)
            {
                case SVGMeshCompression.Low: return ModelImporterMeshCompression.Low;
                case SVGMeshCompression.Medium: return ModelImporterMeshCompression.Medium;
                case SVGMeshCompression.High: return ModelImporterMeshCompression.High;
            }
            return ModelImporterMeshCompression.Off;
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
        internal void ApplyDefaults(SVGImportDefaults defaults)
        {
            _format = defaults.format;
            _useGradients = defaults.useGradients;
            _antialiasing = defaults.antialiasing;
            _meshCompression = defaults.meshCompression;
            _optimizeMesh = defaults.optimizeMesh;
            _scale = defaults.scale;
            _vpm = defaults.verticesPerMeter;
            _depthOffset = defaults.depthOffset;
            _compressDepth = defaults.compressDepth;
            _customPivotPoint = defaults.customPivotPoint;
            _pivotPoint = defaults.pivotPoint;
            _generateCollider = defaults.generateCollider;
            _generateNormals = defaults.generateNormals;
            _generateTangents = defaults.generateTangents;
            _keepSVGFile = defaults.keepSVGFile;
            _useLayers = defaults.useLayers;
            _ignoreSVGCanvas = defaults.ignoreSVGCanvas;
        }
    }
}
