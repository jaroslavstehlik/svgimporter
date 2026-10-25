

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{
    using Rendering;
    using Geometry;
    using Utils;
    using Data;
    using Document;

    public enum SVGShapeType
    {
        NONE,
        FILL,
        STROKE,
        ANTIALIASING
    }

    [System.Serializable]
    public struct SVGLayer
    {
        public string name;
        public SVGShape[] shapes;

        public SVGLayer Clone()
        {
            SVGLayer output = this;
            if(this.shapes != null)
            {
                int totalShapes = this.shapes.Length;
                output.shapes = new SVGShape[totalShapes];
                for(int i = 0; i < totalShapes; i++)
                {
                    output.shapes[i] = this.shapes[i];
                    if(this.shapes[i].vertices != null) output.shapes[i].vertices = this.shapes[i].vertices.Clone() as Vector2[];
                    if(this.shapes[i].triangles != null) output.shapes[i].triangles = this.shapes[i].triangles.Clone() as int[];
                    if(this.shapes[i].colors != null) output.shapes[i].colors = this.shapes[i].colors.Clone() as Color32[];
                    if(this.shapes[i].angles != null) output.shapes[i].angles = this.shapes[i].angles.Clone() as Vector2[];
                    if(this.shapes[i].fill != null) output.shapes[i].fill = this.shapes[i].fill.Clone();
                }
            }
            return output;
        }
    }

    [System.Serializable]
    public struct SVGShape
    {
        public SVGShapeType type;
        public Vector2[] vertices;
        public int[] triangles;
        public Color32[] colors;
        public Vector2[] angles;
        public float depth;
        public Rect bounds;
        public SVGFill fill;

        public SVGShape(Vector2[] vertices, int[] triangles, Color32[] colors, Vector2[] angles, int depth, Rect bounds, SVGFill fill)
        {
            this.type = SVGShapeType.NONE;
            this.vertices = vertices;
            this.triangles = triangles;
            this.colors = colors;
            this.angles = angles;
            this.depth = depth;
            this.bounds = bounds;
            this.fill = fill;
        }

        public int vertexCount
        {
            get {
                if(vertices == null) return 0;
                return vertices.Length;
            }
        }

        public void RecalculateBounds()
        {            
            int length = vertexCount;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for(int i = 0; i < length; i++)
            {
                minX = Mathf.Min(minX, vertices[i].x);
                maxX = Mathf.Max(maxX, vertices[i].x);
                minY = Mathf.Min(minY, vertices[i].y);
                maxY = Mathf.Max(maxY, vertices[i].y);
            }

            if(minX != float.MaxValue || maxX != float.MinValue || minY != float.MaxValue || maxY != float.MinValue)
            {
                this.bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
        }

        public SVGShape Clone()
        {
            SVGShape output = new SVGShape();
            output.angles = this.angles;
            output.bounds = this.bounds;
            return output;
        }

        public static SVGShape MergeShapes(IList<SVGShape> svgShapes)
        {
            SVGShape output = new SVGShape();
            
            int totalLayers = svgShapes.Count;
            int totalVertices = 0;
            int totalTriangles = 0;
            int totalColors = 0;
            int totalAngles = 0;
            int verticesLength;
            int trianglesLength;
            int colorsLength;
            int anglesLength;
            
            for(int i = 0; i < totalLayers; i++)
            {
                if(svgShapes[i].vertices != null) totalVertices += svgShapes[i].vertices.Length;
                if(svgShapes[i].triangles != null) totalTriangles += svgShapes[i].triangles.Length;
                if(svgShapes[i].colors != null) totalColors += svgShapes[i].colors.Length;
                if(svgShapes[i].angles != null) totalAngles += svgShapes[i].angles.Length;
            }
            
            if(totalVertices > 0) output.vertices = new Vector2[totalVertices];
            if(totalTriangles > 0) output.triangles = new int[totalTriangles];
            if(totalColors > 0) output.colors = new Color32[totalColors];
            if(totalAngles > 0) output.angles = new Vector2[totalAngles];
            
            totalVertices = 0;
            totalTriangles = 0;
            totalColors = 0;
            totalAngles = 0;
            
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            
            for(int i = 0; i < totalLayers; i++)
            {
                Vector2 min = svgShapes[i].bounds.min;
                Vector2 max = svgShapes[i].bounds.max;
                minX = Mathf.Min(minX, min.x);
                maxX = Mathf.Max(maxX, max.x);
                minY = Mathf.Min(minY, min.y);
                maxY = Mathf.Max(maxY, max.y);
                
                if(svgShapes[i].vertices != null)
                {
                    verticesLength = svgShapes[i].vertices.Length;
                    for(int k = 0; k < verticesLength; k++)
                    {
                        output.vertices[totalVertices + k] = svgShapes[i].vertices[k];
                    }
                } else {
                    verticesLength = 0;
                }
                
                if(svgShapes[i].triangles != null)
                {
                    trianglesLength = svgShapes[i].triangles.Length;
                    for(int k = 0; k < trianglesLength; k++)
                    {
                        output.triangles[totalTriangles + k] = totalVertices + svgShapes[i].triangles[k];
                    }
                } else {
                    trianglesLength = 0;
                }
                
                if(svgShapes[i].colors != null)
                {
                    colorsLength = svgShapes[i].colors.Length;
                    for(int k = 0; k < colorsLength; k++)
                    {
                        output.colors[totalColors + k] = svgShapes[i].colors[k];
                    }
                } else {
                    colorsLength = 0;
                }
                
                if(svgShapes[i].angles != null)
                {
                    anglesLength = svgShapes[i].angles.Length;
                    for(int k = 0; k < anglesLength; k++)
                    {
                        output.angles[totalAngles + k] = svgShapes[i].angles[k];
                    }
                } else {
                    anglesLength = 0;
                }
                
                totalVertices += verticesLength;
                totalTriangles += trianglesLength;
                totalColors += colorsLength;
                totalAngles += anglesLength;
            }
            
            if(minX != float.MaxValue || maxX != float.MinValue || minY != float.MaxValue || maxY != float.MinValue)
            {
                output.bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            
            return output;
        }
    }

    public enum SVGUseGradients
    {
        Always,
        Auto,
        Never
    }

    public enum SVGMeshCompression
    {
        Off,
        Low,
        Medium,
        High
    }

    public enum SVGAssetFormat
    {
        Opaque = 0,
        Transparent = 1,
        uGUI = 2
    }

    public class SVGAsset : ScriptableObject
    {
        [FormerlySerializedAs("lastTimeModified")]
        [SerializeField]
        protected long _lastTimeModified; // The import hash (SVGImportResult.importHash); the old name is kept for serialization.

        [FormerlySerializedAs("documentAsset")]
        [SerializeField]
        protected SVGDocumentAsset _documentAsset;

        [FormerlySerializedAs("sharedMesh")]
        [SerializeField]
        protected Mesh _sharedMesh;

        /// <summary>
        /// Returns the shared mesh of the SVG Asset. (Read Only)
        /// </summary>
        public Mesh sharedMesh
        {
            get {
                return runtimeMesh;
            }
        }

        public bool isOpaque
        {
            get {
                if(_format == SVGAssetFormat.Transparent || _format == SVGAssetFormat.uGUI) return false;
                if(_sharedShaders == null || _sharedShaders.Length == 0) return true;
                for(int i = 0; i < _sharedShaders.Length; i++)
                {
                    if(string.IsNullOrEmpty(_sharedShaders[i])) continue;
                    if(_sharedShaders[i].ToLower().Contains("opaque")) return true;
                }

                return false;
            }
        }
        
        /// <summary>
        /// Returns the instanced mesh of the SVG Asset. (Read Only)
        /// </summary>
        public Mesh mesh
        {
            get {
                Mesh sharedMeshReference = sharedMesh;
                if(sharedMeshReference == null)
                    return null;
                Mesh clonedMesh = SVGMeshUtils.Clone(sharedMeshReference);
                if(clonedMesh != null)
                {
                    clonedMesh.name += " Instance";                    
                }
                return clonedMesh;
            }
        }

        /// <summary>
        /// Kept for compatibility: gradients are registered with the atlas when a mesh using them
        /// is built (see runtimeMesh), and stay registered while the atlas exists.
        /// </summary>
        public void AddReference(ISVGReference reference)
        {
        }

        /// <summary>Kept for compatibility; see AddReference.</summary>
        public void RemoveReference(ISVGReference reference)
        {
        }

        // The atlas slot of each of the asset's gradients, by the gradient's index in the asset.
        [System.NonSerialized] Dictionary<int, int> _atlasSlots;
        [System.NonSerialized] int _atlasSlotsGeneration;
        [System.NonSerialized] long _atlasSlotsImport;

        /// <summary>
        /// Registers the asset's gradients with the current atlas and returns their slots, by
        /// gradient index in the asset (the index stored in the imported mesh and layers).
        /// </summary>
        internal Dictionary<int, int> atlasSlots
        {
            get {
                SVGAtlas atlas = SVGAtlas.Instance;
                // Unity can reimport an asset into the same object, so caches also check the
                // import time.
                if (_atlasSlots != null && _atlasSlotsGeneration == SVGAtlas.generation && _atlasSlotsImport == _lastTimeModified)
                    return _atlasSlots;
                // Adding a gradient can replace a full atlas; then start over with the new one.
                do {
                    _atlasSlotsGeneration = SVGAtlas.generation;
                    _atlasSlotsImport = _lastTimeModified;
                    _atlasSlots = new Dictionary<int, int>();
                    if (_sharedGradients == null) break;
                    foreach (CCGradient local in _sharedGradients)
                    {
                        if (local == null) continue;
                        CCGradient registered = SVGAtlas.Instance.AddGradient(local.Clone());
                        if (registered != null) _atlasSlots[local.index] = registered.index;
                    }
                } while (_atlasSlotsGeneration != SVGAtlas.generation);
                return _atlasSlots;
            }
        }

        protected Mesh _runtimeMesh;
        [System.NonSerialized] int _runtimeMeshGeneration;
        [System.NonSerialized] Mesh _runtimeMeshSource;
        [System.NonSerialized] long _runtimeMeshImport;

        /// <summary>
        /// The mesh to draw: the imported mesh, with its gradient indices replaced by atlas slots
        /// if it has gradients. Rebuilt when the atlas or the imported mesh changes.
        /// </summary>
        protected Mesh runtimeMesh
        {
            get {
                if(!hasGradients || _sharedMesh == null)
                    return _sharedMesh;

                Dictionary<int, int> slots = atlasSlots;
                if (_runtimeMesh != null && _runtimeMeshGeneration == SVGAtlas.generation && _runtimeMeshSource == _sharedMesh && _runtimeMeshImport == _lastTimeModified)
                    return _runtimeMesh;

                _runtimeMesh = SVGMeshUtils.Clone(_sharedMesh);
                _runtimeMesh.hideFlags = HideFlags.DontSave;
                SVGAtlas.Own(_runtimeMesh);
                _runtimeMeshGeneration = SVGAtlas.generation;
                _runtimeMeshSource = _sharedMesh;
                _runtimeMeshImport = _lastTimeModified;
                Vector2[] uv2 = _runtimeMesh.uv2;
                if(uv2 != null && uv2.Length > 0)
                {
                    int slot;
                    for(int i = 0; i < uv2.Length; i++)
                    {
                        if (slots.TryGetValue(Mathf.FloorToInt(Mathf.Abs(uv2[i].x)), out slot))
                            uv2[i].x = slot;
                    }
                    _runtimeMesh.uv2 = uv2;
                }
                return _runtimeMesh;
            }
        }

        protected Mesh _runtimeLegacyUIMesh;
        [System.NonSerialized] Mesh _runtimeLegacyUIMeshSource;
        [System.NonSerialized] long _runtimeLegacyUIMeshImport;
        protected Mesh runtimeLegacyUIMesh
        {
            get {
                Mesh source = sharedMesh;
                if(_runtimeLegacyUIMesh == null || _runtimeLegacyUIMeshSource != source || _runtimeLegacyUIMeshImport != _lastTimeModified)
                {
                    _runtimeLegacyUIMesh = CreateLegacyUIMesh(source);
                    _runtimeLegacyUIMeshSource = source;
                    _runtimeLegacyUIMeshImport = _lastTimeModified;
                    if (_runtimeLegacyUIMesh != null && hasGradients) SVGAtlas.Own(_runtimeLegacyUIMesh);
                }
                return _runtimeLegacyUIMesh;
            }
        }

        /// <summary>
        /// Returns the shared UI Mesh of the SVG Asset. (Read Only)
        /// </summary>
        public Mesh sharedLegacyUIMesh
        {
            get {
                return runtimeLegacyUIMesh;
            }
        }

        /// <summary>
        /// Returns the shared UI Material of the SVG Asset. (Read Only)
        /// </summary>
        public Material sharedUIMaterial
        {
            get {
                if(_antialiasing)
                {
                    return SVGAtlas.Instance.uiAntialiased;
                } else {
                    return SVGAtlas.Instance.ui;
                }
            }
        }

        /// <summary>
        /// Returns the instanced UI Material of the SVG Asset. (Read Only)
        /// </summary>
        public Material uiMaterial
        {
            get {
                if(_antialiasing)
                {
                    return CloneMaterial(SVGAtlas.Instance.uiAntialiased);
                } else {
                    return CloneMaterial(SVGAtlas.Instance.ui);
                }
            }
        }

        /// <summary>
        /// Returns the shared materials of the SVG Asset. (Read Only)
        /// </summary>
        public Material[] sharedMaterials
        {
            get {
                return runtimeMaterials;
            }
        }

        /// <summary>
        /// Returns the instanced materials of the SVG Asset. (Read Only)
        /// </summary>
        public Material[] materials
        {
            get {
                if(sharedMaterials == null)
                    return null;
                
                int sharedMaterialsLength = sharedMaterials.Length;
                Material[] materials = new Material[sharedMaterialsLength];
                for(int i = 0; i < sharedMaterialsLength; i++)
                {
                    materials[i] = CloneMaterial(sharedMaterials[i]);                    
                }
                return materials;
            }
        }

        protected Material[] _runtimeMaterials;
        public Material[] runtimeMaterials
        {
            get {
                bool regenerateRuntimeMaterials = false;
                if(_runtimeMaterials != null && _runtimeMaterials.Length != 0)
                {
                    for(int i = 0; i < _runtimeMaterials.Length; i++)
                    {
                        if(_runtimeMaterials[i] != null) continue;
                        regenerateRuntimeMaterials = true;
                        break;
                    }
                } else {
                    regenerateRuntimeMaterials = true;
                }

                if(regenerateRuntimeMaterials)
                {
                    if(_sharedShaders != null && _sharedShaders.Length > 0)
                    {
                        _runtimeMaterials = new Material[_sharedShaders.Length];
                        string shaderName;
                        for(int i = 0; i < _sharedShaders.Length; i++)
                        {
                            if(_sharedShaders[i] == null)
                                continue;

                            shaderName = _sharedShaders[i];
                            if(shaderName == SVGShader.SolidColorOpaque.name)
                            {
                                _runtimeMaterials[i] = SVGAtlas.Instance.opaqueSolid;
                            } else if(shaderName == SVGShader.SolidColorAlphaBlended.name)
                            {
                                _runtimeMaterials[i] = SVGAtlas.Instance.transparentSolid;
                            } else if(shaderName == SVGShader.SolidColorAlphaBlendedAntialiased.name)
                            {
                                _runtimeMaterials[i] = SVGAtlas.Instance.transparentSolidAntialiased;
                            } else if(shaderName == SVGShader.GradientColorOpaque.name)
                            {
                                _runtimeMaterials[i] = SVGAtlas.Instance.opaqueGradient;
                            } else if(shaderName == SVGShader.GradientColorAlphaBlended.name)
                            {
                                _runtimeMaterials[i] = SVGAtlas.Instance.transparentGradient;
                            } else if(shaderName == SVGShader.GradientColorAlphaBlendedAntialiased.name)
                            {
                                _runtimeMaterials[i] = SVGAtlas.Instance.transparentGradientAntialiased;
                            }
                        }
                    } else {
                        _runtimeMaterials = new Material[0];
                    }
                }
                return _runtimeMaterials;
            }
        }

        [FormerlySerializedAs("antialiasing")]
        [SerializeField]
        protected bool _antialiasing = false;
        /// <summary>
        /// Use antialiasing (Read Only)
        /// </summary>
        public bool antialiasing
        {
            get {
                return _antialiasing;
            }
        }

        [FormerlySerializedAs("generateCollider")]
        [SerializeField]
        protected bool _generateCollider = false;        
        /// <summary>
        /// Returns if the asset has generated collider shape. (Read Only)
        /// </summary>
        public bool generateCollider
        {
            get {
                return _generateCollider;
            }
        }

        [FormerlySerializedAs("keepSVGFile")]
        [SerializeField]
        protected bool _keepSVGFile = true;
        /// <summary>
        /// Keep the SVG file in the final build (Read Only)
        /// </summary>
        public bool keepSVGFile
        {
            get {
                return _keepSVGFile;
            }
        }

        [FormerlySerializedAs("ignoreSVGCanvas")]
        [SerializeField]
        protected bool _ignoreSVGCanvas = true;
        /// <summary>
        /// Trim the document canvas to object bounding box (Read Only)
        /// </summary>
        public bool ignoreSVGCanvas
        {
            get {
                return _ignoreSVGCanvas;
            }
        }

        [FormerlySerializedAs("colliderShape")]
        [SerializeField]
        protected SVGPath[] _colliderShape;
        /// <summary>
        /// Returns the collider shape. (Read Only)
        /// </summary>
        public SVGPath[] colliderShape
        {
            get {
                return _colliderShape;
            }
        }

        [FormerlySerializedAs("format")]
        [SerializeField]
        protected SVGAssetFormat _format = SVGAssetFormat.Transparent;
        /// <summary>
        /// Returns the rendering format of the SVG Asset. (Read Only)
        /// </summary>
        public SVGAssetFormat format
        {
            get {
                return _format;
            }
        }
        
        [FormerlySerializedAs("useGradients")]
        [SerializeField]
        protected SVGUseGradients _useGradients = SVGUseGradients.Always;
        /// <summary>
        /// Returns if the mesh was compressed. (Read Only)
        /// </summary>
        public SVGUseGradients useGradients
        {
            get {
                return _useGradients;
            }
        }

        [FormerlySerializedAs("meshCompression")]
        [SerializeField]
        protected SVGMeshCompression _meshCompression = SVGMeshCompression.Off;
        /// <summary>
        /// Returns if the mesh was compressed. (Read Only)
        /// </summary>
        public SVGMeshCompression meshCompression
        {
            get {
                return _meshCompression;
            }
        }

        [FormerlySerializedAs("optimizeMesh")]
        [SerializeField]
        protected bool _optimizeMesh = true;        
        /// <summary>
        /// Returns if the mesh is optimised for GPU. (Read Only)
        /// </summary>
        public bool optimizeMesh
        {
            get {
                return _optimizeMesh;
            }
        }

        [FormerlySerializedAs("generateNormals")]
        [SerializeField]
        protected bool _generateNormals = false;
        /// <summary>
        /// Returns if the mesh contains normals. (Read Only)
        /// </summary>
        public bool generateNormals
        {
            get {
                return _generateNormals;
            }
        }

        [FormerlySerializedAs("generateTangents")]
        [SerializeField]
        protected bool _generateTangents = false;        
        /// <summary>
        /// Returns if the mesh contains tangents. (Read Only)
        /// </summary>
        public bool generateTangents
        {
            get {
                return _generateTangents;
            }
        }

        [FormerlySerializedAs("scale")]
        [SerializeField]
        protected float _scale = 0.01f;
        /// <summary>
        /// Returns the scale of the mesh relative to the SVG Asset. (Read Only)
        /// </summary>
        public float scale {
            get {
                return _scale;
            }
        }

        [FormerlySerializedAs("vpm")]
        [SerializeField]
        protected float _vpm = 1000f;
        /// <summary>
        /// Returns the number of vertices in the SVG Asset that correspond to one unit in world space. (Read Only)
        /// </summary>
        public float vpm
        {
            get {
                return _vpm;
            }
        }

        [FormerlySerializedAs("depthOffset")]
        [SerializeField]
        protected float _depthOffset = 0.01f;
        /// <summary>
        /// Returns the minimal z-offset in WorldSpace for Opaque Rendering. (Read Only)
        /// </summary>
        public float depthOffset
        {
            get {
                return _depthOffset;
            }
        }

        [FormerlySerializedAs("compressDepth")]
        [SerializeField]
        protected bool _compressDepth = true;
        /// <summary>
        /// Returns the compress overlapping objects to reduce z-offset requirements. (Read Only)
        /// </summary>
        public bool compressDepth
        {
            get {
                return _compressDepth;
            }
        }

        [FormerlySerializedAs("pivotPoint")]
        [SerializeField]
        protected Vector2 _pivotPoint = new Vector2(0.5f, 0.5f);
        /// <summary>
        /// Returns the location of the SVG Asset center point in the original Rect, specified in percents. (Read Only)
        /// </summary>
        public Vector2 pivotPoint
        {
            get {
                return _pivotPoint;
            }
        }

        [FormerlySerializedAs("customPivotPoint")]
        [SerializeField]
        protected bool _customPivotPoint = false;
        /// <summary>
        /// Returns the use of predefined pivot point or custom pivot point. (Read Only)
        /// </summary>
        public bool customPivotPoint
        {
            get {
                return _customPivotPoint;
            }
        }

		[FormerlySerializedAs("border")]
		[SerializeField]
		protected Vector4 _border = new Vector4(0f, 0f, 0f, 0f);		
		/// <summary>
		/// Returns the 9-slice border. (Read Only)
        /// LEFT, BOTTOM, RIGHT, TOP
		/// </summary>
		public Vector4 border
		{
			get {
				return _border;
			}
		}

        [FormerlySerializedAs("sliceMesh")]
        [SerializeField]
        protected bool _sliceMesh = false;        
        /// <summary>
        /// Returns if the mesh is sliced. (Read Only)
        /// </summary>
        public bool sliceMesh
        {
            get {
                return _sliceMesh;
            }
        }

        protected string _svgFile;
        /// <summary>
        /// Returns the original SVG text content available only in the Editor. (Read Only)
        /// </summary>
        public string svgFile
        {
            get {
                if(!string.IsNullOrEmpty(_svgFile))
                {
                    return _svgFile;
                } else {
                    if(_documentAsset != null)
                    {
                        return _documentAsset.svgFile;
                    } else {
                        return null;
                    }
                }
            }
        }

        [FormerlySerializedAs("sharedGradients")]
        [SerializeField]
        protected CCGradient[] _sharedGradients;
        /// <summary>
        /// Returns all the used gradients in the SVG Asset. (Read Only)
        /// </summary>
        public CCGradient[] sharedGradients {
            get {
                return _sharedGradients;
            }
        }

        [FormerlySerializedAs("sharedShaders")]
        [SerializeField]
        protected string[] _sharedShaders;
        /// <summary>
        /// Returns all the used shader names in the SVG Asset. (Read Only)
        /// </summary>
        public string[] sharedShaders {
            get {
                return _sharedShaders;
            }
        }

        /// <summary>
        /// Returns the bounding volume of the mesh of the SVG Asset. (Read Only)
        /// </summary>
        public Bounds bounds
        {
            get {
                if(_sharedMesh == null)
                    return new Bounds();

                return _sharedMesh.bounds;
            }
        }

        [FormerlySerializedAs("canvasRectangle")]
        [SerializeField]
        protected Rect _canvasRectangle;
        /// <summary>
        /// Returns the Original Canvas rectangle of the SVG Asset. (Read Only)
        /// </summary>
        public Rect canvasRectangle
        {
            get {
                return _canvasRectangle;
            }
        }

        [FormerlySerializedAs("useLayers")]
        [SerializeField]
        protected bool _useLayers = false;
        /// <summary>
        /// Store individual SVG Layers for further modification. (Read Only)
        /// </summary>
        public bool useLayers
        {
            get {
                return _useLayers;
            }
        }

        [FormerlySerializedAs("layers")]
        [SerializeField]
        protected SVGLayer[] _layers;
        /// <summary>
        /// Returns individual SVG layers. (Read Only)
        /// </summary>
        public SVGLayer[] layers
        {
            get {
                return _layers;
            }
        }

        public SVGLayer[] layersClone
        {
            get {
                if(_layers == null) return null;
                int _layersLength = _layers.Length;
                SVGLayer[] output = new SVGLayer[_layersLength];
                for(int i = 0; i < _layersLength; i++)
                {
                    output[i] = _layers[i].Clone();
                }
                // Meshes built from the layers draw with the atlas, so their gradients refer to
                // atlas slots, as in runtimeMesh.
                if (hasGradients)
                {
                    Dictionary<int, int> slots = atlasSlots;
                    int slot;
                    foreach (SVGLayer layer in output)
                    {
                        if (layer.shapes == null) continue;
                        foreach (SVGShape shape in layer.shapes)
                        {
                            if (shape.fill != null && shape.fill.gradientColors != null && slots.TryGetValue(shape.fill.gradientColors.index, out slot))
                                shape.fill.gradientColors.index = slot;
                        }
                    }
                }
                return output;
            }
        }

        /// <summary>
        /// Returns if the SVG Asset contains any gradients. (Read Only)
        /// </summary>
        public bool hasGradients
        {
            get {
                if(_sharedGradients == null || _sharedGradients.Length == 0) return false;
                if(_sharedGradients.Length == 1)
                {
                    if(SVGAtlasData.IsDefaultGradient(_sharedGradients[0])) return false;
                }
                return true;
            }
        }

        protected Material CloneMaterial(Material original)
        {
            if(original == null)
                return null;

            Material material = new Material(original.shader);
            material.CopyPropertiesFromMaterial(original);
            return material;
        }

        /// <summary>
        /// Returns the number of vertices in the mesh of the SVG Asset. (Read Only)
        /// </summary>
        public int uiVertexCount
        {
            get {
                if(_sharedMesh == null || _sharedMesh.triangles == null)
                    return 0;

                int trianglesCount = _sharedMesh.triangles.Length;
                return trianglesCount + (trianglesCount / 3);
            }
        }

        protected static Mesh CreateLegacyUIMesh(Mesh inputMesh)
        {
            if(inputMesh == null) return null;

            Mesh outputMesh = new Mesh();

            Vector3[] vertices = inputMesh.vertices;
            Color32[] colors = inputMesh.colors32;
            Vector2[] uv = inputMesh.uv;
            Vector2[] uv2 = inputMesh.uv2;
            Vector3[] normals = inputMesh.normals;
            Vector4[] tangents = inputMesh.tangents;

            int[] triangles = inputMesh.triangles;
            int trianglesCount = triangles.Length;
            int verticesCount = trianglesCount + (trianglesCount / 3);
            Vector3[] outputVertices = new Vector3[verticesCount];
            Color32[] outputColors = new Color32[verticesCount];
            Vector2[] outputUV0;
            Vector2[] outputUV1;
            Vector3[] outputNormals;
            Vector4[] outputTangents;

            int currentQuad = 0;
            int currentTriangle = 0;

            for(int i = 0; i < trianglesCount; i += 3)
            {
                currentTriangle = triangles[i];
                outputVertices[currentQuad] = vertices[currentTriangle];
                outputColors[currentQuad] = colors[currentTriangle];
                currentQuad++;
                
                currentTriangle = triangles[i + 1];
                outputVertices[currentQuad] = vertices[currentTriangle];
                outputColors[currentQuad] = colors[currentTriangle];
                currentQuad++;
                
                currentTriangle = triangles[i + 2];
                outputVertices[currentQuad] = vertices[currentTriangle];
                outputColors[currentQuad] = colors[currentTriangle];
                currentQuad++;
            }

            outputMesh.vertices = outputVertices;
            outputMesh.colors32 = outputColors;

            if(uv != null && uv.Length > 0 && uv2 != null && uv2.Length > 0)
            {
                currentQuad = 0;
                currentTriangle = 0;

                outputUV0 = new Vector2[verticesCount];
                outputUV1 = new Vector2[verticesCount];

                for(int i = 0; i < trianglesCount; i += 3)
                {
                    currentTriangle = triangles[i];
                    outputUV0[currentQuad] = uv[currentTriangle];
                    outputUV1[currentQuad] = uv2[currentTriangle];
                    currentQuad++;
                    
                    currentTriangle = triangles[i + 1];
                    outputUV0[currentQuad] = uv[currentTriangle];
                    outputUV1[currentQuad] = uv2[currentTriangle];
                    currentQuad++;
                    
                    currentTriangle = triangles[i + 2];
                    outputUV0[currentQuad] = uv[currentTriangle];
                    outputUV1[currentQuad] = uv2[currentTriangle];
                    currentQuad++;
                }

                outputMesh.uv = outputUV0;
                outputMesh.uv2 = outputUV1;
            }

            if(normals != null && normals.Length > 0)
            {
                currentQuad = 0;
                currentTriangle = 0;

                outputNormals = new Vector3[verticesCount];

                for(int i = 0; i < trianglesCount; i += 3)
                {
                    currentTriangle = triangles[i];
                    outputNormals[currentQuad] = normals[currentTriangle];
                    currentQuad++;
                    
                    currentTriangle = triangles[i + 1];
                    outputNormals[currentQuad] = normals[currentTriangle];
                    currentQuad++;
                    
                    currentTriangle = triangles[i + 2];
                    outputNormals[currentQuad] = normals[currentTriangle];
                    currentQuad++;
                }

                outputMesh.normals = outputNormals;
            }

            if(tangents != null && tangents.Length > 0)
            {
                currentQuad = 0;
                currentTriangle = 0;

                outputTangents = new Vector4[verticesCount];

                for(int i = 0; i < trianglesCount; i += 3)
                {
                    currentTriangle = triangles[i];
                    outputTangents[currentQuad] = tangents[currentTriangle];
                    currentQuad++;
                    
                    currentTriangle = triangles[i + 1];                        
                    outputTangents[currentQuad] = tangents[currentTriangle];
                    currentQuad++;
                    
                    currentTriangle = triangles[i + 2];
                    outputTangents[currentQuad] = tangents[currentTriangle];
                    currentQuad++;
                }

                outputMesh.tangents = outputTangents;
            }
            
            return outputMesh;
        }

        /// <summary>
        /// Imports SVG text at runtime (also in the Editor), with the defaults of settings, or of
        /// SVGImportOptions.Default without them. The engine is the project's (Project Settings >
        /// SVG Importer). Returns null if the document couldn't be imported.
        /// </summary>
        public static SVGAsset Load(string svgText, SVGImporterSettings settings = null)
        {
            SVGImportOptions options = settings != null ? SVGImportOptions.FromSettings(settings) : SVGImportOptions.Default;
            options.engine = SVGImporterSettings.Get.importEngine;
            return Load(svgText, options);
        }

        /// <summary>Imports SVG text with these options. Returns null if the document couldn't be imported.</summary>
        public static SVGAsset Load(string svgText, SVGImportOptions options)
        {
            if(string.IsNullOrEmpty(svgText)) return null;
            SVGImportResult result = SVGImport.Import(svgText, options);
            foreach(string message in result.messages)
            {
                if(result.succeeded) Debug.LogWarning("SVG Importer: " + message);
                else Debug.LogError("SVG Importer: " + message);
            }
            if(!result.succeeded) return null;

            SVGAsset asset = ScriptableObject.CreateInstance<SVGAsset>();
            asset.Apply(result, options, SVGDocumentAsset.CreateInstance(options.keepSVGFile ? svgText : null, result.errors.ToArray()));
            asset._ownsMesh = true;
            return asset;
        }

        // Assets made by Load own their mesh; imported assets' meshes belong to the asset file.
        [System.NonSerialized] bool _ownsMesh;

        protected void OnDestroy()
        {
            if(_ownsMesh && _sharedMesh != null)
            {
                if(Application.isPlaying) Destroy(_sharedMesh); else DestroyImmediate(_sharedMesh);
            }
        }

        /// <summary>The settings the asset was imported with.</summary>
        public SVGImportOptions importOptions
        {
            get {
                return new SVGImportOptions {
                    engine = _importEngine,
                    format = _format, useGradients = _useGradients, antialiasing = _antialiasing,
                    verticesPerMeter = _vpm, scale = _scale, depthOffset = _depthOffset, compressDepth = _compressDepth,
                    customPivotPoint = _customPivotPoint, pivotPoint = _pivotPoint, border = _border, sliceMesh = _sliceMesh,
                    ignoreSVGCanvas = _ignoreSVGCanvas, generateCollider = _generateCollider, useLayers = _useLayers,
                    generateNormals = _generateNormals, generateTangents = _generateTangents,
                    meshCompression = _meshCompression, optimizeMesh = _optimizeMesh, keepSVGFile = _keepSVGFile,
                };
            }
        }

        [SerializeField]
        protected SVGImportEngine _importEngine = SVGImportEngine.Legacy;
        /// <summary>Identifies the import: equal for the same SVG text and settings.</summary>
        public long importHash
        {
            get { return _lastTimeModified; }
        }

        /// <summary>The engine that imported the asset.</summary>
        public SVGImportEngine importEngine
        {
            get { return _importEngine; }
        }

        /// <summary>Makes this asset hold an import's result.</summary>
        internal void Apply(SVGImportResult result, SVGImportOptions options, SVGDocumentAsset document)
        {
            _importEngine = result.engine;
            _format = options.format;
            _useGradients = options.useGradients;
            _antialiasing = options.antialiasing;
            _meshCompression = options.meshCompression;
            _optimizeMesh = options.optimizeMesh;
            _scale = options.scale;
            _vpm = options.verticesPerMeter;
            _depthOffset = options.depthOffset;
            _compressDepth = options.compressDepth;
            _customPivotPoint = options.customPivotPoint;
            _pivotPoint = options.pivotPoint;
            _border = options.border;
            _sliceMesh = options.sliceMesh;
            _generateCollider = options.generateCollider;
            _generateNormals = options.generateNormals;
            _generateTangents = options.generateTangents;
            _keepSVGFile = options.keepSVGFile;
            _useLayers = options.useLayers;
            _ignoreSVGCanvas = options.ignoreSVGCanvas;

            _sharedMesh = result.mesh;
            _sharedShaders = result.shaders;
            _sharedGradients = result.gradients == null ? null : System.Array.ConvertAll(result.gradients, g => g.Clone());
            _layers = result.layers;
            _colliderShape = result.colliderShape;
            _canvasRectangle = result.canvasRectangle;
            _documentAsset = document;
            // Caches derived from the asset (runtimeMesh, atlasSlots) are keyed on it.
            _lastTimeModified = result.importHash;
            _runtimeMaterials = null;
        }

    #if UNITY_EDITOR

        internal SVGDocumentAsset _editor_documentAsset
        {
            get {
                return _documentAsset;
            }
        }

        internal void _editor_ApplyChanges(bool importMultipleFiles = false)
        {
            if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling)
                return;

            if(_documentAsset != null)
            {
                _documentAsset.errors = null;
            }

            if(_sharedShaders != null)
                _sharedShaders = null;

            if(_sharedMesh != null)
            {
                Object.DestroyImmediate(_sharedMesh, true);
                _sharedMesh = null;
            }

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if(assets != null && assets.Length > 0)
            {
                for(int i = 0; i < assets.Length; i++)
                {
                    if(assets[i] == null)
                        continue;
                    if(assets[i] == this)
                        continue;

                    if(assets[i] is SVGDocumentAsset)
                        continue;

                    DestroyImmediate(assets[i], true);
                }
            }

            // 1.x SVG Assets are imported with the legacy engine.
            SVGImportOptions options = importOptions;
            options.engine = SVGImportEngine.Legacy;
            SVGImportResult result = SVGImport.Import(svgFile, options);
            if(result.mesh != null)
            {
                result.mesh.name = this.name;
                UnityEditor.MeshUtility.SetMeshCompression(result.mesh, GetModelImporterMeshCompression(_meshCompression));
                AddObjectToAsset<Mesh>(result.mesh, this, HideFlags.HideInHierarchy);
            }
            SVGDocumentAsset document = _documentAsset;
            Apply(result, options, document);
            foreach(string message in result.messages)
            {
                if(result.succeeded) Debug.LogWarning("SVG Importer: " + this.name + ": " + message, this);
                else Debug.LogError("SVG Importer: " + this.name + ": " + message, this);
            }

            // Create Document Asset
            if(_documentAsset == null)
            {
                _documentAsset = AddObjectToAsset<SVGDocumentAsset>(ScriptableObject.CreateInstance<SVGDocumentAsset>(), this, HideFlags.HideInHierarchy);
            }

            var svgAssetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            var svgAssetImporter = UnityEditor.AssetImporter.GetAtPath(svgAssetPath);

            string svgSource = svgFile;
            if(svgAssetImporter != null && !string.IsNullOrEmpty(svgSource))
            {
                svgAssetImporter.userData = svgSource;
            }

            if(keepSVGFile || svgAssetImporter == null)
            {
                // Without an importer there is nowhere else to keep the source.
                _documentAsset.svgFile = svgSource;
            } else {
                _documentAsset.svgFile = null;
            }

            _documentAsset.errors = result.errors.ToArray();
            UnityEditor.EditorUtility.SetDirty(_documentAsset);

            _svgFile = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        internal UnityEditor.ModelImporterMeshCompression GetModelImporterMeshCompression(SVGMeshCompression meshCompression)
        {
            switch(meshCompression)
            {
                case SVGMeshCompression.Low:
                return UnityEditor.ModelImporterMeshCompression.Low;
                case SVGMeshCompression.Medium:
                    return UnityEditor.ModelImporterMeshCompression.Medium;
                case SVGMeshCompression.High:
                    return UnityEditor.ModelImporterMeshCompression.High;
            }

            return UnityEditor.ModelImporterMeshCompression.Off;
        }

        internal T AddObjectToAsset<T>(T obj, SVGAsset asset, HideFlags hideFlags) where T : UnityEngine.Object
        {
            if(obj == null)
                return null;
            
            obj.hideFlags = hideFlags;
            UnityEditor.AssetDatabase.AddObjectToAsset(obj, asset);
            return obj;
        }

        internal SVGAssetFormat _editor_format
        {
            set { _format = value; }
        }

        internal Mesh _editor_sharedMesh
        {
            get {
                return _sharedMesh;
            }
        }

        internal string _editor_Info
        {
            get {
                string output = "No info available";

                if(_useLayers)
                {
                    if(_layers == null || _layers.Length == 0) return output;
                    int totalVertices = 0;
                    int totalTriangles = 0;

                    for(int i = 0; i < _layers.Length; i++)
                    {
                        if(_layers[i].shapes == null) continue;
                        for(int j = 0; j < _layers[i].shapes.Length; j++)
                        {
                            if(_layers[i].shapes[j].vertices == null) continue;
                            totalVertices += _layers[i].shapes[j].vertices.Length;
                            totalTriangles += _layers[i].shapes[j].triangles.Length;
                        }
                    }
                    
                    return string.Format("{0} Vertices, {1} Triangles", totalVertices, totalTriangles);
                } else {
                    if(_sharedMesh == null)
                    {
                        return output;
                    }
                    
                    int totalVertices = _sharedMesh.vertexCount;
                    int totalTriangles = _sharedMesh.triangles.Length / 3;
                    
                    return string.Format("{0} Vertices, {1} Triangles", totalVertices, totalTriangles);
                }
            }
        }

        internal SVGError[] _editor_errors
        {
            get {
                if(_documentAsset == null)
                    return null;

                return _documentAsset.errors;
            }
        }
    #endif
    }
}
