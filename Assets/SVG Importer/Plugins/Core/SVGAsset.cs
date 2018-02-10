// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
                if(vertices[i].x < minX)
                {
                    minX = vertices[i].x;
                } else if(vertices[i].x > maxX)
                {
                    maxX = vertices[i].x;
                }
                
                if(vertices[i].y < minY)
                {
                    minY = vertices[i].y;
                } else if(vertices[i].y > maxY)
                {
                    maxY = vertices[i].y;
                }
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
                if(min.x < minX)
                {
                    minX = min.x;
                } else if(max.x > maxX)
                {
                    maxX = max.x;
                }
                
                if(min.y < minY)
                {
                    minY = min.y;
                } else if(max.y > maxY)
                {
                    maxY = max.y;
                }
                
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

        public static SVGShape MergeLayersToShape(IList<SVGLayer> svgLayers)
        {
            SVGShape output = new SVGShape();

            int totalLayers = svgLayers.Count;
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
                int totalShapes = svgLayers[i].shapes.Length;
                for(int j = 0; j < totalShapes; j++)
                {
                    if(svgLayers[i].shapes[j].vertices != null) totalVertices += svgLayers[i].shapes[j].vertices.Length;
                    if(svgLayers[i].shapes[j].triangles != null) totalTriangles += svgLayers[i].shapes[j].triangles.Length;
                    if(svgLayers[i].shapes[j].colors != null) totalColors += svgLayers[i].shapes[j].colors.Length;
                    if(svgLayers[i].shapes[j].angles != null) totalAngles += svgLayers[i].shapes[j].angles.Length;
                }
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
                int totalShapes = svgLayers[i].shapes.Length;
                for(int j = 0; j < totalShapes; j++)
                {
                    Vector2 min = svgLayers[i].shapes[j].bounds.min;
                    Vector2 max = svgLayers[i].shapes[j].bounds.max;
                    if(min.x < minX)
                    {
                        minX = min.x;
                    } else if(max.x > maxX)
                    {
                        maxX = max.x;
                    }

                    if(min.y < minY)
                    {
                        minY = min.y;
                    } else if(max.y > maxY)
                    {
                        maxY = max.y;
                    }

                    if(svgLayers[i].shapes[j].vertices != null)
                    {
                        verticesLength = svgLayers[i].shapes[j].vertices.Length;
                        for(int k = 0; k < verticesLength; k++)
                        {
                            output.vertices[totalVertices + k] = svgLayers[i].shapes[j].vertices[k];
                        }
                    } else {
                        verticesLength = 0;
                    }

                    if(svgLayers[i].shapes[j].triangles != null)
                    {
                        trianglesLength = svgLayers[i].shapes[j].triangles.Length;
                        for(int k = 0; k < trianglesLength; k++)
                        {
                            output.triangles[totalTriangles + k] = totalVertices + svgLayers[i].shapes[j].triangles[k];
                        }
                    } else {
                        trianglesLength = 0;
                    }

                    if(svgLayers[i].shapes[j].colors != null)
                    {
                        colorsLength = svgLayers[i].shapes[j].colors.Length;
                        for(int k = 0; k < colorsLength; k++)
                        {
                            output.colors[totalColors + k] = svgLayers[i].shapes[j].colors[k];
                        }
                    } else {
                        colorsLength = 0;
                    }

                    if(svgLayers[i].shapes[j].angles != null)
                    {
                        anglesLength = svgLayers[i].shapes[j].angles.Length;
                        for(int k = 0; k < anglesLength; k++)
                        {
                            output.angles[totalAngles + k] = svgLayers[i].shapes[j].angles[k];
                        }
                    } else {
                        anglesLength = 0;
                    }

                    totalVertices += verticesLength;
                    totalTriangles += trianglesLength;
                    totalColors += colorsLength;
                    totalAngles += anglesLength;
                }
            }

            if(minX != float.MaxValue || maxX != float.MinValue || minY != float.MaxValue || maxY != float.MinValue)
            {
                output.bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }

            return output;
        }

        public static SVGShape[] MergeLayersToShapes(IList<SVGLayer> svgLayers)
        {
            SVGShape[] output;

            if(svgLayers == null) return null;

            int totalLayers = svgLayers.Count;
            int totalShapes = 0;

            for(int i = 0; i < totalLayers; i++)
            {
                int shapesLength = svgLayers[i].shapes.Length;
                totalShapes += shapesLength;
            }

            if(totalShapes == 0) return null;

            output = new SVGShape[totalShapes];

            int currentShape = 0;
            for(int i = 0; i < totalLayers; i++)
            {
                int shapesLength = svgLayers[i].shapes.Length;
                for(int j = 0; j < shapesLength; j++)
                {
                    output[currentShape++] = svgLayers[i].shapes[j];
                }
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
        protected long _lastTimeModified;

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
                    clonedMesh.name += " Instance "+clonedMesh.GetInstanceID();                    
                }
                return clonedMesh;
            }
        }

        public void AddReference(ISVGReference reference)
        {
            if(hasGradients)
            {
                MonoBehaviour mb = reference as MonoBehaviour;                
                if(SVGAtlas.beingDestroyed) return;
                for(int i = 0; i < _sharedGradients.Length; i++)
                {
                    if(_sharedGradients[i] == null) continue;
                    CCGradient gradient = SVGAtlas.Instance.GetGradient(_sharedGradients[i]);
                    if(gradient != null)
                    {
                        gradient.AddReference(reference);
                    } else {
                        gradient = SVGAtlas.Instance.AddGradient(_sharedGradients[i].Clone());
                        gradient.AddReference(reference);
                    }
                }
            }
        }

        public void RemoveReference(ISVGReference reference)
        {
            if(hasGradients)
            {
                MonoBehaviour mb = reference as MonoBehaviour;                
                int totalReferences = 0;
                if(SVGAtlas.beingDestroyed) return;
                for(int i = 0; i < _sharedGradients.Length; i++)
                {
                    if(_sharedGradients[i] == null) continue;
                    CCGradient gradient = SVGAtlas.Instance.GetGradient(_sharedGradients[i]);
                    if(gradient != null)
                    {
                        gradient.RemoveReference(reference);
                        if(gradient.referenceCount == 0)
                        {
                            SVGAtlas.Instance.RemoveGradient(gradient);
                        }
                        totalReferences += gradient.CountReferences(reference);
                    }
                }

                if(totalReferences == 0)
                {
                    if(_runtimeMesh != null)
                    {
                        _runtimeMesh.Clear();
                        _runtimeMesh = null;
                    }

                    if(_runtimeMaterials != null)
                    {
                        _runtimeMaterials = null;
                    }
                }
            }
        }

        protected Mesh _runtimeMesh;
        protected Mesh runtimeMesh
        {
            get {
                if(!hasGradients)
                {
                    return _sharedMesh;
                } else {
                    if(_runtimeMesh == null && _sharedMesh != null)
                    {
//                        Debug.Log("Mesh has changed: "+name);
                        Dictionary<int, int> gradientCache = new Dictionary<int, int>();
                        CCGradient[] gradients = new CCGradient[_sharedGradients.Length];
                        for(int i = 0; i < _sharedGradients.Length; i++)
                        {
                            if(_sharedGradients[i] == null)
                                continue;

                            CCGradient gradient = SVGAtlas.Instance.GetGradient(_sharedGradients[i]);
                            if(gradient != null)
                            {
                                gradients[i] = gradient;
                            } else {
                                gradients[i] = SVGAtlas.Instance.AddGradient(_sharedGradients[i].Clone());
                            }

                            gradientCache.Add(_sharedGradients[i].index, gradients[i].index);
                        }

                        _runtimeMesh = SVGMeshUtils.Clone(_sharedMesh);
                        _runtimeMesh.hideFlags = HideFlags.DontSave;
                        if(_runtimeMesh.uv2 != null && _runtimeMesh.uv2.Length > 0)
                        {
                            Vector2[] uv2 = _runtimeMesh.uv2;
                            int key;
                            for(int i = 0; i < uv2.Length; i++)
                            {
                                key = Mathf.FloorToInt(Mathf.Abs(uv2[i].x));
                                try {
                                    uv2[i].x = gradientCache[key];
                                } catch {}
                                /*
                                catch ( System.Exception exception)
                                {
                                    Debug.Log("i: "+i+", "+exception.Message+"\nKey: "+key);
                                }
                                */
                            }
                            _runtimeMesh.uv2 = uv2;
                        }
                    }    
                }
                return _runtimeMesh;
            }
        }

        protected Mesh _runtimeLegacyUIMesh;
        protected Mesh runtimeLegacyUIMesh
        {
            get {
                if(_runtimeLegacyUIMesh == null)
                    _runtimeLegacyUIMesh = CreateLegacyUIMesh(sharedMesh);
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
                    if(_sharedGradients[0].hash == CCGradient.DEFAULT_GRADIENT_HASH) return false;
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

        /// <summary>Load SVG at runtime. (Slow Method).
        /// <para>svgText represents the SVG string content</para>
        /// <para>settings holds all the SVG settings</para>
        /// </summary>
        public static SVGAsset Load(string svgText, SVGImporterSettings settings = null)
        {
#if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                Debug.LogWarning("SVG Asset Load works only in playmode!");
                return null;
            }
#endif
            if(string.IsNullOrEmpty(svgText)) return null;

            if(settings == null)
            {
                SVGAssetImport.format = SVGAssetFormat.Transparent;
                SVGAssetImport.pivotPoint = new Vector2(0.5f, 0.5f);
                SVGAssetImport.meshScale = 0.01f;
                SVGAssetImport.border = new Vector4(0f, 0f, 0f, 0f);
                SVGAssetImport.sliceMesh = false;
                SVGAssetImport.minDepthOffset = 0.01f;
                SVGAssetImport.compressDepth = true;
                SVGAssetImport.ignoreSVGCanvas = true;
                SVGAssetImport.useGradients = SVGUseGradients.Always;            
            } else {
                SVGAssetImport.format = settings.defaultSVGFormat;
                SVGAssetImport.pivotPoint = settings.defaultPivotPoint;
                SVGAssetImport.meshScale = settings.defaultScale;
                SVGAssetImport.border = new Vector4(0f, 0f, 0f, 0f);
                SVGAssetImport.sliceMesh = false;
                SVGAssetImport.minDepthOffset = settings.defaultDepthOffset;
                SVGAssetImport.compressDepth = settings.defaultCompressDepth;
                SVGAssetImport.ignoreSVGCanvas = settings.defaultIgnoreSVGCanvas;
                SVGAssetImport.useGradients = settings.defaultUseGradients;
                SVGAssetImport.antialiasing = settings.defaultAntialiasing;
            }

            SVGGraphics graphics = new SVGGraphics(1000f, SVGAssetImport.antialiasing);
            SVGDocument svgDocument = null;

            SVGAssetImport.Clear();
            SVGAssetImport.atlasData = new SVGAtlasData();
            SVGAssetImport.atlasData.Init(SVGAtlas.defaultAtlasTextureWidth * SVGAtlas.defaultAtlasTextureHeight);
            SVGAssetImport.atlasData.AddGradient(SVGAtlasData.GetDefaultGradient());
            SVGParser.Init();
            SVGGraphics.Init();
            
            SVGElement rootSVGElement = null;
            List<SVGError> errors = new List<SVGError>();

            svgDocument = new SVGDocument(svgText, graphics);
            rootSVGElement = svgDocument.rootElement;

            if(rootSVGElement == null)
            {
                Debug.LogError("SVG Document is corrupted!");
                return null;
            }

            SVGAsset asset = ScriptableObject.CreateInstance<SVGAsset>();

            asset._antialiasing = SVGAssetImport.antialiasing;
            asset._border = SVGAssetImport.border;
            asset._compressDepth = SVGAssetImport.compressDepth;
            asset._depthOffset = SVGAssetImport.minDepthOffset;
            asset._ignoreSVGCanvas = SVGAssetImport.ignoreSVGCanvas;
            asset._meshCompression = SVGMeshCompression.Off;
            asset._scale = SVGAssetImport.meshScale;
            asset._format = SVGAssetImport.format;
            asset._useGradients = SVGAssetImport.useGradients;
            asset._pivotPoint = SVGAssetImport.pivotPoint;
            asset._vpm = SVGAssetImport.vpm;
            asset._sharedGradients = null;

            if(settings != null)
            {
                asset._generateCollider = settings.defaultGenerateCollider;
                asset._generateNormals = settings.defaultGenerateNormals;
                asset._generateTangents = settings.defaultGenerateTangents;
                asset._sliceMesh = false;
                asset._optimizeMesh = settings.defaultOptimizeMesh;
                asset._keepSVGFile = settings.defaultKeepSVGFile;
            } else {
                asset._generateCollider = false;
                asset._generateNormals = false;
                asset._generateTangents = false;
                asset._sliceMesh = false;
                asset._optimizeMesh = true;
                asset._keepSVGFile = false;
            }

            try {
                rootSVGElement.Render();

                Rect viewport = rootSVGElement.paintable.viewport;
                viewport.x *= SVGAssetImport.meshScale;
                viewport.y *= SVGAssetImport.meshScale;
                viewport.size *= SVGAssetImport.meshScale;

                Vector2 offset;
                SVGGraphics.CorrectSVGLayers(SVGGraphics.layers, viewport, asset, out offset);

                // Handle gradients
                bool hasGradients = (asset.useGradients == SVGUseGradients.Always);

                // Create actual Mesh
                Shader[] outputShaders;
                Mesh mesh = new Mesh();
                SVGMesh.CombineMeshes(SVGGraphics.layers.ToArray(), mesh, out outputShaders, asset._useGradients, asset._format, asset._compressDepth, asset._antialiasing);
                if(mesh == null) return null;

                if(asset._useGradients == SVGUseGradients.Always)
                {
                    if(outputShaders != null)
                    {
                        for(int i = 0; i < outputShaders.Length; i++)
                        {
                            if(outputShaders[i] == null) continue;
                            if(outputShaders[i].name == SVGShader.SolidColorOpaque.name)
                            {
                                outputShaders[i] = SVGShader.GradientColorOpaque;
                            } else if(outputShaders[i].name == SVGShader.SolidColorAlphaBlended.name)
                            {
                                outputShaders[i] = SVGShader.GradientColorAlphaBlended;
                            } else if(outputShaders[i].name == SVGShader.SolidColorAlphaBlendedAntialiased.name)
                            {
                                outputShaders[i] = SVGShader.GradientColorAlphaBlendedAntialiased;
                            }
                        }
                    }
                    hasGradients = true;
                } else {
                    if(outputShaders != null)
                    {
                        for(int i = 0; i < outputShaders.Length; i++)
                        {
                            if(outputShaders[i] == null) continue;
                            if(outputShaders[i].name == SVGShader.GradientColorOpaque.name ||
                               outputShaders[i].name == SVGShader.GradientColorAlphaBlended.name ||
                               outputShaders[i].name == SVGShader.GradientColorAlphaBlendedAntialiased.name)
                            {
                                hasGradients = true;
                                break;
                            }
                        }
                    }
                }

                if(!asset.useLayers)
                {
                    asset._sharedMesh = mesh;
                }

                if(outputShaders != null && outputShaders.Length > 0)
                {
                    asset._sharedShaders = new string[outputShaders.Length];
                    if(hasGradients)
                    {
                        for(int i = 0; i < outputShaders.Length; i++)
                        {
                            asset._sharedShaders[i] = outputShaders[i].name;
                        }
                    } else {
                        for(int i = 0; i < outputShaders.Length; i++)
                        {
                            if(outputShaders[i].name == SVGShader.GradientColorAlphaBlended.name)
                            {
                                outputShaders[i] = SVGShader.SolidColorAlphaBlended;
                            } else if(outputShaders[i].name == SVGShader.GradientColorAlphaBlendedAntialiased.name)
                            {
                                outputShaders[i] = SVGShader.SolidColorAlphaBlendedAntialiased;                                
                            } else if(outputShaders[i].name == SVGShader.GradientColorOpaque.name)
                            {
                                outputShaders[i] = SVGShader.SolidColorOpaque;                                
                            }
                            asset._sharedShaders[i] = outputShaders[i].name;
                        }
                    }
                }

                // Handle Canvas Rectangle
                asset._canvasRectangle = new Rect(viewport.x, viewport.y, viewport.size.x, viewport.size.y);
                
                if(asset.generateCollider)
                {
                    // Create polygon contour
                    if(SVGGraphics.paths != null && SVGGraphics.paths.Count > 0)
                    {
                        List<List<Vector2>> polygons = new List<List<Vector2>>();
                        for(int i = 0; i < SVGGraphics.paths.Count; i++)
                        {
                            Vector2[] points = SVGGraphics.paths[i].points;
                            for(int j = 0; j < points.Length; j++)
                            {
                                points[j].x = points[j].x * SVGAssetImport.meshScale - offset.x;
                                points[j].y = (points[j].y * SVGAssetImport.meshScale + offset.y) * -1f;
                            }
                            
                            polygons.Add(new List<Vector2>(points));
                        }
                        
                        polygons = SVGGeom.MergePolygon(polygons);
                        
                        SVGPath[] paths = new SVGPath[polygons.Count];
                        for(int i = 0; i < polygons.Count; i++)
                        {
                            paths[i] = new SVGPath(polygons[i].ToArray());
                        }

                        if(paths != null && paths.Length > 0)
                        {
                            asset._colliderShape = paths;
                        }
                    }
                }

                if(hasGradients)
                {
                    if(SVGAssetImport.atlasData.gradientCache != null && SVGAssetImport.atlasData.gradientCache.Count > 0)
                    {
                        int gradientsCount = SVGAssetImport.atlasData.gradientCache.Count;
                        CCGradient[] gradients = new CCGradient[gradientsCount];
                        int i = 0;
                        foreach(KeyValuePair<string, CCGradient> entry in SVGAssetImport.atlasData.gradientCache)
                        {
                            gradients[i++] = entry.Value;
                        }
                        asset._sharedGradients = gradients;
                    }
                }
            } catch(System.Exception exception) {
                Debug.LogWarning("Asset Failed to import\n"+exception.Message);
                errors.Add(SVGError.CorruptedFile);
            }

            asset._documentAsset = SVGDocumentAsset.CreateInstance(svgText, errors.ToArray());
            if(svgDocument != null) svgDocument.Clear();
            SVGAssetImport.Clear();
            return asset;
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

            _editor_LoadSVG();        

            // Create Document Asset
            if(_documentAsset == null)
            {
                _documentAsset = AddObjectToAsset<SVGDocumentAsset>(ScriptableObject.CreateInstance<SVGDocumentAsset>(), this, HideFlags.HideInHierarchy);
            }

            var svgAssetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            var svgAssetImporter = UnityEditor.AssetImporter.GetAtPath(svgAssetPath);

            if(!string.IsNullOrEmpty(svgFile))
            {
                svgAssetImporter.userData = svgFile;
            }

            if(keepSVGFile)
            {
                _documentAsset.svgFile = svgAssetImporter.userData;
            } else {
                _documentAsset.svgFile = null;
            }

            if(SVGAssetImport.errors != null && SVGAssetImport.errors.Count > 0)
            {
                _documentAsset.errors = SVGAssetImport.errors.ToArray();

				bool critical = false;
				string errors = "";
				int errorsLength = _documentAsset.errors.Length;
				for(int i = 0; i < errorsLength; i++)
				{
					if(i < errorsLength - 1)
					{
						errors += _documentAsset.errors[i].ToString() +", ";
					} else {
						errors += _documentAsset.errors[i].ToString() +".";
					}

					if(_documentAsset.errors[i] == SVGError.CorruptedFile || 
					   _documentAsset.errors[i] == SVGError.Syntax)
					{
						critical = true;
					}
				}

				if(critical)
				{
					Debug.LogError ("SVGAsset: "+this.name+"\nerrors: "+errors+"\npath: "+UnityEditor.AssetDatabase.GetAssetPath(this)+"\n", this);
				} else {
					Debug.LogWarning ("SVGAsset: "+this.name+"\nerrors: "+errors+"\npath: "+UnityEditor.AssetDatabase.GetAssetPath(this)+"\n", this);
				}
            }

            UnityEditor.EditorUtility.SetDirty(_documentAsset);

            _svgFile = null;

            if(SVGAssetImport.errors != null)
            {
                SVGAssetImport.errors.Clear();
                SVGAssetImport.errors = null;
            }

            if(_sharedMesh != null && _sharedMesh.vertexCount > 0)
            {
                _sharedMesh.name = this.name;

                int vertexCount = _sharedMesh.vertexCount;
                UnityEditor.MeshUtility.SetMeshCompression(_sharedMesh, GetModelImporterMeshCompression(_meshCompression));
                if(_optimizeMesh) ;
                if(_generateNormals)
                {
                    if(!_antialiasing)
                    {
                        Vector3[] normals = new Vector3[vertexCount];
                        for(int i = 0; i < vertexCount; i++)
                        {
                            normals[i] = -Vector3.forward;
                        }
                        _sharedMesh.normals = normals;
                    }
                    if(_generateTangents)
                    {
                        Vector4[] tangents = new Vector4[vertexCount];
                        for(int i = 0; i < vertexCount; i++)
                        {
                            tangents[i] = new Vector4(-1f, 0f, 0f, -1f);
                        }
                        _sharedMesh.tangents = tangents;
                    }
                }
            }

            _lastTimeModified = System.DateTime.UtcNow.Ticks;
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

        internal void _editor_SetGradients(CCGradient[] gradients)
        {
            if(gradients == null || gradients.Length == 0)
            {
                _sharedGradients = null;
                return;
            }
            
            _sharedGradients = new CCGradient[gradients.Length];
            for(int i = 0; i < gradients.Length; i++)
            {
                _sharedGradients[i] = gradients[i].Clone();
            }
        }

        internal void _editor_SetColliderShape(SVGPath[] shape)
        {
            _colliderShape = shape;
        }

        internal void _editor_SetCanvasRectangle(Rect rectangle)
        {
            _canvasRectangle = rectangle;
        }

        internal void _editor_SetLayers(SVGLayer[] layers)
        {
            _layers = layers;
        }

        internal void _editor_LoadSVG()
        {        
            SVGAssetImport assetImport;
            if (svgFile != null)
            {
                SVGAssetImport.format = _format;
                SVGAssetImport.meshScale = _scale;
                SVGAssetImport.border = _border;
                SVGAssetImport.sliceMesh = _sliceMesh;
                SVGAssetImport.minDepthOffset = _depthOffset;
                SVGAssetImport.compressDepth = _compressDepth;
                SVGAssetImport.ignoreSVGCanvas = _ignoreSVGCanvas;
                SVGAssetImport.useGradients = _useGradients;
                SVGAssetImport.antialiasing = _antialiasing;
                assetImport = new SVGAssetImport(svgFile, _vpm);
                assetImport.StartProcess(this);
            }
        }

        internal T AddObjectToAsset<T>(T obj, SVGAsset asset, HideFlags hideFlags) where T : UnityEngine.Object
        {
            if(obj == null)
                return null;
            
            obj.hideFlags = hideFlags;
            UnityEditor.AssetDatabase.AddObjectToAsset(obj, asset);
            return obj;
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
