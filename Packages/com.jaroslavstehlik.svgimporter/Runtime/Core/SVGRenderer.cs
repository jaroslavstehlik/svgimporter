

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

using System.Collections;
using System.Collections.Generic;

namespace SVGImporter
{
    using Utils;
    using Rendering;
    using Geometry;
    
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/SVG Renderer", 20)]
    public class SVGRenderer : UIBehaviour, ISVGShape, ISVGRenderer, ISVGReference
    {
        public enum Type
        {
            Simple,
            Sliced
        }

        /// <summary>
        /// delegate which indicates that the source SVG graphics changed.
        /// </summary>
        public System.Action<SVGAsset> onVectorGraphicsChanged;

        protected System.Action<SVGLayer[], SVGAsset, bool> _OnPrepareForRendering;
        /// <summary>
        /// delegate which indicates that the mesh has changed.
        /// You can use it as your custom mesh postprocessor.
        /// </summary>
        public virtual System.Action<SVGLayer[], SVGAsset, bool> OnPrepareForRendering
        {
            get {
                return _OnPrepareForRendering;
            }
            set {
                _OnPrepareForRendering = value;
            }
        }

        protected Type _lastType;
        /// <summary>
        /// Render type of the Image
        /// </summary>
        [FormerlySerializedAs("type")]
        [SerializeField] private Type _type = Type.Simple;
        public Type type { get { return _type; } set { _type = value; } }

        // Tracking of modified assets
        [FormerlySerializedAs("lastTimeModified")]
        [SerializeField]
        protected long _lastTimeModified;

        // Tracking of modified assets
        protected Rect _rectTransformRect;
        protected Rect _lastRectTransformRect;

        // Tracking of mesh change
        protected int _lastFrameChanged;
        public int lastFrameChanged
        {
            get {
                return _lastFrameChanged;
            }
        }

        // Serialized SVG Asset
        [FormerlySerializedAs("vectorGraphics")]
        [SerializeField]
        protected SVGAsset _vectorGraphics;
        protected SVGAsset _lastVectorGraphics;

        /// <summary>
        /// The SVG Asset to render.
        /// </summary>
        public SVGAsset vectorGraphics
        {
            get {
                return _vectorGraphics;
            }
            set {
                _vectorGraphics = value;
                if(!meshRenderer.isPartOfStaticBatch)
                    PrepareForRendering(true);
            }
        }

        // Serialized rendering color
        [FormerlySerializedAs("color")]
        [SerializeField]
        protected Color _color = Color.white;
        protected Color _lastColor = Color.white;
        protected Color32[] _cachedColors;
        protected Vector3[] _cachedVertices;

        /// <summary>
        /// Rendering color for the SVG Asset..
        /// </summary>
        public Color color
        {
            get {
                return _color;
            }
            set {
                _color = value;
            }
        }
        
        [FormerlySerializedAs("opaqueMaterial")]
        [SerializeField]
        protected Material _opaqueMaterial;
        protected Material _lastOpaqueMaterial;
        /// <summary>
        /// The opaque override material
        /// </summary>
        public Material opaqueMaterial
        {
            get {
                return _opaqueMaterial;
            }
            set {
                if(_opaqueMaterial != value)
                {
                    _opaqueMaterial = value;
                    UpdateMaterials();
                }
            }
        }

        [FormerlySerializedAs("transparentMaterial")]
        [SerializeField]
        protected Material _transparentMaterial;
        protected Material _lastTransparentMaterial;
        /// <summary>
        /// The opaque override material
        /// </summary>
        public Material transparentMaterial
        {
            get {
                return _transparentMaterial;
            }
            set {
                if(_transparentMaterial != value)
                {
                    _transparentMaterial = value;
                    UpdateMaterials();
                }
            }
        }

        protected MeshFilter _meshFilter;
        public MeshFilter meshFilter {
            get
            {
                if (_meshFilter == null) GetComponent<MeshRenderer>();
                return _meshFilter;
            }
        }

        protected MeshRenderer _meshRenderer;
        public MeshRenderer meshRenderer {
            get {
                if (_meshRenderer == null) GetComponent<MeshRenderer>();
                return _meshRenderer;
            }
        }

        public RectTransform rectTransform
        {
            get {
                return transform as RectTransform;
            }
        }

        // Cached SVG Layers
        protected SVGLayer[] _layers;
        // Shared mesh for better gpu instancing
        protected Mesh _sharedMesh;
        // Instanced mesh for custom coloring
        protected Mesh _mesh;

        // Unity default transparent sorting ID
        [FormerlySerializedAs("sortingLayerID")]
        [SerializeField]
        protected int _sortingLayerID = 0;
        protected int _lastSortingLayerID = 0;
        /// <summary>
        /// Unique ID of the Renderer's sorting layer.
        /// </summary>
        public int sortingLayerID
        {
            get {
                return meshRenderer.sortingLayerID;
            }
            set {
#if UNITY_EDITOR
                _lastSortingLayerID = value;
#endif
                if(!SortingLayer.IsValid(value))
                {
                    Debug.LogWarning(this.name + ": This renderer has an invalid layer-id, resetting to default.");
                    _sortingLayerID = SortingLayer.NameToID("Default");
                } else
                {
                    _sortingLayerID = value;
                }
                meshRenderer.sortingLayerID = _sortingLayerID;
                _sortingLayerName = meshRenderer.sortingLayerName;
            }
        }

        // Unity default transparent sorting layer name
        [FormerlySerializedAs("sortingLayerName")]
        [SerializeField]
        protected string _sortingLayerName;
        /// <summary>
        /// Name of the Renderer's sorting layer.
        /// </summary>
        public string sortingLayerName
        {
            get {
                return meshRenderer.sortingLayerName;
            }
            set {
                meshRenderer.sortingLayerName = _sortingLayerName = value;
                _lastSortingLayerID = _sortingLayerID = meshRenderer.sortingLayerID;
            }
        }

        // Unity default transparent sorting order
        [FormerlySerializedAs("sortingOrder")]
        [SerializeField]
        protected int _sortingOrder = 0;
        protected int _lastSortingOrder = 0;
        /// <summary>
        /// Renderer's order within a sorting layer.
        /// </summary>
        public int sortingOrder
        {
            get {
                return meshRenderer.sortingOrder;
            }
            set {
#if UNITY_EDITOR
                _lastSortingOrder = value;
#endif
                meshRenderer.sortingOrder = _sortingOrder = value;
            }
        }

        [FormerlySerializedAs("overrideSorter")]
        [SerializeField]
        protected bool _overrideSorter = false;
        protected bool _lastOverrideSorter = false;
        /// <summary>
        /// Override SVG Sorter Default Behaviour
        /// </summary>
        public bool overrideSorter
        {
            get {
                return _overrideSorter;
            }
            set {
                _overrideSorter = value;
            }
        }

        [FormerlySerializedAs("overrideSorterChildren")]
        [SerializeField]
        protected bool _overrideSorterChildren = false;
        protected bool _lastOverrideSorterChildren = false;
        /// <summary>
        /// Override SVG Sorter Default Behaviour
        /// </summary>
        public bool overrideSorterChildren
        {
            get {
                return _overrideSorterChildren;
            }
            set {
                _overrideSorterChildren = value;
            }
        }

        /// <summary>
        /// Get SVG Outline, part of the ISVGPath interface..
        /// </summary>
        public SVGPath[] shape
        {
            get {
                if(_vectorGraphics == null) return null;
                return _vectorGraphics.colliderShape;
            }
        }

        // We have to clear editor data and load runtime data
        // Also it handles duplicating game objects
        protected override void Awake()
        {
            base.Awake();
            CacheComponents();            
            meshFilter.sharedMesh = null;
            if(_vectorGraphics != null)
            {
                _vectorGraphics.AddReference(this);
            }

            Clear();
            PrepareForRendering(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnableMeshRenderer(true);
        }
        
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (_vectorGraphics != null && _vectorGraphics.sharedMesh != null)
            {
                Bounds bounds = _vectorGraphics.bounds;
                Matrix4x4 gizmoMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                Gizmos.matrix = gizmoMatrix;
            }
        }
#endif

        protected override void OnDisable()
        {
            EnableMeshRenderer(false);
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (_vectorGraphics != null)
            {
                _vectorGraphics.RemoveReference(this);
            }
            base.OnDestroy();
        }

        void CacheComponents()
        {
            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
                if (_meshFilter == null)
                {
                    _meshFilter = gameObject.AddComponent<MeshFilter>();
                }
            }

            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
                if (_meshRenderer == null)
                {
                    _meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }
            }
        }
        
        public void UpdateRenderer()
        {
            PrepareForRendering(true);
        }
        
        protected override void OnDidApplyAnimationProperties()
        {
            if (!meshRenderer.isPartOfStaticBatch)
                PrepareForRendering();
        }
        
        // This is the main rendering method
        protected void PrepareForRendering(bool force = false)
        {           
#if UNITY_EDITOR
            if(_lastSortingOrder != _sortingOrder){ 
                sortingOrder = _sortingOrder;
            }
            if(_lastSortingLayerID != _sortingLayerID){ 
                sortingLayerID = _sortingLayerID;
            }
#endif
            if(_vectorGraphics == null)
            {
                if(_lastVectorGraphics != null)
                {
                    _lastVectorGraphics.RemoveReference(this);
                    _lastVectorGraphics = null;
                }
                Clear();
            } else {
                bool meshChanged = force || _lastType != _type || meshFilter.sharedMesh == null;
                bool colorChanged = force || _lastColor != _color;
                bool materialChanged = force || _lastOpaqueMaterial != _opaqueMaterial || _lastTransparentMaterial != _transparentMaterial;
#if UNITY_EDITOR
                for(int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
                {
                    if(meshRenderer.sharedMaterials[i] != null) continue;
                    materialChanged = true;
                    break;
                }
#endif

                if(_lastVectorGraphics != _vectorGraphics)
                {
                    meshChanged = true;
                    colorChanged = true;
                    materialChanged = true;

                    if (_lastVectorGraphics != null)
                    {
                        _lastVectorGraphics.RemoveReference(this);
                    }
                    if(_vectorGraphics != null)
                    {
                        _vectorGraphics.AddReference(this);
                    }
                }

                if(useLayers || !useSharedMesh)
                {
                    if(_lastUseSharedMesh != false) meshChanged = true;
                    if(!meshChanged)
                    {
                        if(_type == Type.Sliced && rectTransform != null)
                        {
                            _rectTransformRect = rectTransform.rect;
                            if(_rectTransformRect != _lastRectTransformRect)
                            {
                                meshChanged = true;
                                _lastRectTransformRect = _rectTransformRect;
                            }
                        }
                    }
                }

                if(useLayers)
                {
                    if(_layers == null) _layers = _vectorGraphics.layersClone;                    
                    if(meshChanged || colorChanged)
                    {
                        InitMesh();
                        materialChanged = true;
                        
                        if(_type == Type.Sliced)
                        {
                            UpdateSlicedMesh();
                        }
                        
                        UpdateColors(force);
                        _lastFrameChanged = Time.frameCount;
                        materialChanged = true;
                        
                        if(_OnPrepareForRendering != null)
                            _OnPrepareForRendering(_layers, _vectorGraphics, force);
                        
                        GenerateMesh();
                        
                        if(meshFilter.sharedMesh != _mesh)
                            meshFilter.sharedMesh = _mesh;
                    }
                } else {
                    if(useSharedMesh)
                    {
                        _sharedMesh = _vectorGraphics.sharedMesh;
                        meshFilter.sharedMesh = _sharedMesh;
                    } else {
                        // Cache Mesh
                        if(meshChanged)
                        {
                            InitMesh();
                            materialChanged = true;
                            if(_type == Type.Sliced)
                            {
                                UpdateSlicedMesh();
                            }
                            
                            if(onVectorGraphicsChanged != null)
                                onVectorGraphicsChanged(_vectorGraphics);
                        }

                        if(meshChanged || colorChanged)
                        {
                            UpdateColors(force);
                            _lastFrameChanged = Time.frameCount;
                            materialChanged = true;
                        }
                        
                        if(meshFilter.sharedMesh != _mesh)
                            meshFilter.sharedMesh = _mesh;
                    }
                }
                
                if(materialChanged)
                {
                    UpdateMaterials();
                }

                _lastOpaqueMaterial = _opaqueMaterial;
                _lastTransparentMaterial = _transparentMaterial;
                _lastVectorGraphics = _vectorGraphics;
                _lastColor = _color;
                _lastType = _type;
                _lastUseSharedMesh = useSharedMesh;                
            }

#if UNITY_EDITOR
            UpdateTimeStamp();
#endif
        }

        protected void GenerateMesh()
        {
            Shader[] outputShaders;
            SVGMesh.CombineMeshes(_layers, _mesh, out outputShaders, _vectorGraphics.useGradients, _vectorGraphics.format, _vectorGraphics.compressDepth, _vectorGraphics.antialiasing);
        }

#if UNITY_EDITOR
        // Clear SVG Renderer when hit Reset in the Editor
        protected override void Reset()
        {
            if(!UnityEditor.EditorApplication.isPlaying)
            {
                PrepareForRendering(true);
            }

            base.Reset();
        }
#endif

#if UNITY_EDITOR
        void UpdateTimeStamp()
        {
            if(!UnityEditor.EditorApplication.isPlaying)
            {
                if(_vectorGraphics != null)
                {
                    System.Reflection.FieldInfo assetTicksInfo = typeof(SVGAsset).GetField("_lastTimeModified", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    long assetTicks = (long)assetTicksInfo.GetValue(_vectorGraphics);
                    if(_lastTimeModified != assetTicks)
                    {
                        _lastTimeModified = assetTicks;
                        //Clear();
                    }
                }
            }
        }
#endif

        protected Color32[] _finalColors;
        protected void UpdateColors(bool force = false)
        {
            if(_color == Color.white) return;
            if(useLayers)
            {
                Color32 tempColor = _color;
                bool alphaBlended = tempColor.a != (byte)255;
                int totalLayers = _layers.Length, totalShapes;
                int i, j;
                for(i = 0; i < totalLayers; i++)
                {
                    totalShapes = _layers[i].shapes.Length;
                    for(j = 0; j < totalShapes; j++)
                    {
                        if(_layers[i].shapes[j].fill == null) continue;

                        Color32 originalColor = _layers[i].shapes[j].fill.color;
                        originalColor.r = (byte)((originalColor.r * tempColor.r) / 255);
                        originalColor.g = (byte)((originalColor.g * tempColor.g) / 255);
                        originalColor.b = (byte)((originalColor.b * tempColor.b) / 255);
                        originalColor.a = (byte)((originalColor.a * tempColor.a) / 255);
                        _layers[i].shapes[j].fill.color = originalColor;

                        if(alphaBlended)
                        {
                            _layers[i].shapes[j].fill.blend = FILL_BLEND.ALPHA_BLENDED;
                        }
                    }
                }
            } else {
                if(_sharedMesh != null)
                {
                    if(_cachedColors == null || _cachedColors.Length != _sharedMesh.vertexCount)
                    {
                        Color32[] originalColors = _sharedMesh.colors32;
                        if(originalColors == null || originalColors.Length == 0)
                            return;

                        _finalColors = new Color32[originalColors.Length];
                        _cachedColors = (Color32[])originalColors.Clone();
                    }

                    int colorsLength = _cachedColors.Length;
                    Color32 tempColor = _color;
                    for(int i = 0; i < colorsLength; i++)
                    {
                        _finalColors[i].r = (byte)((_cachedColors[i].r * tempColor.r) / 255);
                        _finalColors[i].g = (byte)((_cachedColors[i].g * tempColor.g) / 255);
                        _finalColors[i].b = (byte)((_cachedColors[i].b * tempColor.b) / 255);
                        _finalColors[i].a = (byte)((_cachedColors[i].a * tempColor.a) / 255);
                    }

                    _mesh.colors32 = _finalColors;
                    meshFilter.sharedMesh = _mesh;
                }
            }
        }

        /// <summary>
        /// Whether the Image has a border to work with.
        /// </summary>        
        public bool hasBorder
        {
            get
            {
                if (_vectorGraphics != null)
                {
                    return _vectorGraphics.border.sqrMagnitude > 0f;
                }
                return false;
            }
        }

        /// <summary>
        /// Conversion ratio for UI Interpretation
        /// </summary>
        protected float pixelsPerUnit
        {
            get
            {
                float spritePixelsPerUnit = 100;
                return spritePixelsPerUnit;
            }
        }

        protected float InverseLerp(float from, float to, float value)
        {
            if (from < to)
            {               
                value -= from;
                value /= to - from;
                return value;
            }
            else
            {
                return 1f - (value - to) / (from - to);
            }
        }

        protected float SafeDivide(float a, float b)
        {
            if(b == 0) return 0f;
            return a / b;
        }
        
        protected string BorderToString(Vector4 border)
        {
            return string.Format("left: {0}, bottom: {1}, right: {2}, top: {3}", border.x, border.y, border.z, border.w);
        }

        const float epsilon = 0.0000001f;
        protected Vector3[] _finalVertices;
        protected void UpdateSlicedMesh()
        {
            if(hasBorder && rectTransform != null)
            {
                Bounds bounds = _vectorGraphics.bounds;
                Vector4 v = new Vector4(
                    _rectTransformRect.x,
                    _rectTransformRect.y,
                    _rectTransformRect.width,
                    _rectTransformRect.height
                    );

                // LEFT = X, BOTTOM = Y, RIGHT = Z, TOP = W
                Vector4 border = _vectorGraphics.border;                
                Vector4 borderCalc = new Vector4(border.x + epsilon, border.y + epsilon, 1f - border.z - epsilon, 1f - border.w - epsilon);
                
                Vector2 normalizedPosition;
                
                float rectSize = vectorGraphics.scale * 100f;
                Vector2 size = new Vector2(bounds.size.x * rectSize, bounds.size.y * rectSize);
                Vector4 transformRect = new Vector4(v.x, v.y, v.x + v.z, v.y + v.w);
                Vector4 borderRect = new Vector4(size.x * border.x,
                                                 size.y * border.y,
                                                 size.x * border.z,
                                                 size.y * border.w);
                
                Vector2 scale = new Vector2(SafeDivide(1f, (1f - (border.x + border.z))) * (v.z - (borderRect.x + borderRect.z)),
                                            SafeDivide(1f, (1f - (border.y + border.w))) * (v.w - (borderRect.w + borderRect.y)));
                
                float minWidth = borderRect.x + borderRect.z;
                if(minWidth != 0f)
                {
                    minWidth = Mathf.Clamp01(v.z / minWidth);
                    if(minWidth != 1f)
                    {
                        scale.x = 0f;
                        size.x *= minWidth;
                        borderRect.x *= minWidth;
                        borderRect.z *= minWidth;
                    }
                }
                
                float minHeight = borderRect.w + borderRect.y;
                if(minHeight != 0f)
                {
                    minHeight = Mathf.Clamp01(v.w / minHeight);
                    if(minHeight != 1f)
                    {
                        scale.y = 0f;
                        size.y *= minHeight;
                        borderRect.w *= minHeight;
                        borderRect.y *= minHeight;
                    }
                    
                }
                
                float borderTop = transformRect.w - borderRect.w;
                float borderLeft = transformRect.x + borderRect.x;

                if(useLayers)
                {
                    int totalLayers = _layers.Length, totalShapes, totalVertices;
                    int i, j, k;
                    for(i = 0; i < totalLayers; i++)
                    {
                        totalShapes = _layers[i].shapes.Length;
                        for(j = 0; j < totalShapes; j++)
                        {
                            totalVertices = _layers[i].shapes[j].vertices.Length;
                            for(k = 0; k < totalVertices; k++)
                            {
                                normalizedPosition.x = InverseLerp(bounds.min.x, bounds.max.x, _layers[i].shapes[j].vertices[k].x);
                                normalizedPosition.y = InverseLerp(bounds.min.y, bounds.max.y, _layers[i].shapes[j].vertices[k].y);
                                
                                if(border.x != 0f && normalizedPosition.x <= borderCalc.x)
                                {
                                    _layers[i].shapes[j].vertices[k].x = transformRect.x + normalizedPosition.x * size.x;
                                } else if(border.z != 0f && normalizedPosition.x >= borderCalc.z)
                                {
                                    _layers[i].shapes[j].vertices[k].x = transformRect.z - (1f - normalizedPosition.x) * size.x;
                                } else {
                                    _layers[i].shapes[j].vertices[k].x = borderLeft + (normalizedPosition.x - border.x) * scale.x;
                                }
                                
                                if(border.w != 0f && normalizedPosition.y >= borderCalc.w)
                                {
                                    _layers[i].shapes[j].vertices[k].y = transformRect.w - (1f - normalizedPosition.y) * size.y;
                                } else if(border.y != 0f && normalizedPosition.y <= borderCalc.y)
                                {
                                    _layers[i].shapes[j].vertices[k].y = transformRect.y + normalizedPosition.y * size.y;
                                } else {
                                    _layers[i].shapes[j].vertices[k].y = borderTop - (((1f - normalizedPosition.y) - border.w) * scale.y);
                                }     
                            }
                        }
                    }
                } else {
                    if(_cachedVertices == null)
                    {
                        if(_sharedMesh == null) _sharedMesh = _vectorGraphics.sharedMesh;
                        Vector3[] originalVertices = _sharedMesh.vertices;
                        if(originalVertices == null || originalVertices.Length == 0)
                            return;
                        
                        _finalVertices = new Vector3[originalVertices.Length];
                        _cachedVertices = (Vector3[])originalVertices.Clone();
                    }

                    int cachedVerticesLength = _cachedVertices.Length;
                    for(int i = 0; i < cachedVerticesLength; i++)
                    {
                        normalizedPosition.x = InverseLerp(bounds.min.x, bounds.max.x, _cachedVertices[i].x);
                        normalizedPosition.y = InverseLerp(bounds.min.y, bounds.max.y, _cachedVertices[i].y);
                        
                        if(border.x != 0f && normalizedPosition.x <= borderCalc.x)
                        {
                            _finalVertices[i].x = transformRect.x + normalizedPosition.x * size.x;
                        } else if(border.z != 0f && normalizedPosition.x >= borderCalc.z)
                        {
                            _finalVertices[i].x = transformRect.z - (1f - normalizedPosition.x) * size.x;
                        } else {
                            _finalVertices[i].x = borderLeft + (normalizedPosition.x - border.x) * scale.x;
                        }
                        
                        if(border.w != 0f && normalizedPosition.y >= borderCalc.w)
                        {
                            _finalVertices[i].y = transformRect.w - (1f - normalizedPosition.y) * size.y;
                        } else if(border.y != 0f && normalizedPosition.y <= borderCalc.y)
                        {
                            _finalVertices[i].y = transformRect.y + normalizedPosition.y * size.y;
                        } else {
                            _finalVertices[i].y = borderTop - (((1f - normalizedPosition.y) - border.w) * scale.y);
                        }                    
                    }

                    _mesh.vertices = _finalVertices;
                    meshFilter.sharedMesh = _mesh;     
                }
            }
        }

        internal bool AtlasContainsMaterial(Material material)
        {
            return SVGAtlas.Instance.ContainsMaterial(material);
        }

        protected void SwapMaterials(bool transparent = true)
        {
            if(_vectorGraphics == null)
            {
                CleanMaterials();
                return;
            }
            bool hasGradients = _vectorGraphics.hasGradients || _vectorGraphics.useGradients == SVGUseGradients.Always;
            Material sharedOpaqueMaterial = SVGAtlas.Instance.GetOpaqueMaterial(hasGradients);
            Material sharedTransparentMaterial = SVGAtlas.Instance.GetTransparentMaterial(_vectorGraphics.antialiasing, hasGradients);

            int subMeshCount = 0;
            if(useLayers)
            {
                subMeshCount = _mesh.subMeshCount;
            } else {
                if(_sharedMesh != null) subMeshCount = _sharedMesh.subMeshCount;
            }

            if(_vectorGraphics.isOpaque)
            {
                if(transparent)
                {
                    if(_transparentMaterial != null)
                    {
                        SetSharedMaterials(subMeshCount, _transparentMaterial, _transparentMaterial);
                    } else {
                        SetSharedMaterials(subMeshCount, sharedTransparentMaterial, sharedTransparentMaterial);
                    }
                } else {
                    if(_opaqueMaterial == null && _transparentMaterial == null)
                    {
                        SetSharedMaterials(subMeshCount, sharedOpaqueMaterial, sharedTransparentMaterial);
                    } else if(_opaqueMaterial != null && _transparentMaterial != null)
                    {
                        SetSharedMaterials(subMeshCount, _opaqueMaterial, _transparentMaterial);
                    } else if(_transparentMaterial != null)
                    {
                        SetSharedMaterials(subMeshCount, sharedOpaqueMaterial, _transparentMaterial);
                    } else if(_opaqueMaterial != null)
                    {
                        SetSharedMaterials(subMeshCount, _opaqueMaterial, sharedTransparentMaterial);
                    }
                }
            } else {
                if(_transparentMaterial == null)
                {
                    SetSharedMaterials(subMeshCount, sharedTransparentMaterial, sharedTransparentMaterial);
                } else {
                    SetSharedMaterials(subMeshCount, _transparentMaterial, _transparentMaterial);
                }
            }
        }

        void SetSharedMaterials(int subMeshCount, Material firstMaterial, Material secondMaterial)
        {
            if(subMeshCount < 2)
            {
                meshRenderer.sharedMaterials = new Material[]{ firstMaterial };
            } else {
                meshRenderer.sharedMaterials = new Material[]{ firstMaterial, secondMaterial };
            }
//            Debug.Log("SetSharedMaterials, subMeshCount: "+subMeshCount+", a: "+firstMaterial+", b: "+secondMaterial);
        }

        public void UpdateMaterials()
        {
            if(_opaqueMaterial != null) SVGAtlas.Instance.UpdateMaterialProperties(_opaqueMaterial);
            if(_transparentMaterial != null) SVGAtlas.Instance.UpdateMaterialProperties(_transparentMaterial);
            SwapMaterials(_color.a != 1);
        }

        /// <summary>
        /// Mark the Graphic as dirty and prepare it for rendering.
        /// </summary>
        /// <param name="force">Force re-updating the whole object</param>
        public void SetAllDirty()
        {
#if UNITY_EDITOR
            if(!UnityEditor.EditorApplication.isPlaying)
            {
                PrepareForRendering(true);
            } else {
                if(!meshRenderer.isPartOfStaticBatch)
                    PrepareForRendering(true);
            }
#else
            if(!meshRenderer.isPartOfStaticBatch)
                PrepareForRendering(true);
#endif
        }
        
        void EnableMeshRenderer(bool value)
        {
#if UNITY_EDITOR
            if(UnityEditor.EditorApplication.isPlaying)
            {
                if(!meshRenderer.isPartOfStaticBatch)
                    meshRenderer.enabled = value;
            } else {
                meshRenderer.enabled = value;
            }
#else
            if(!meshRenderer.isPartOfStaticBatch)
                meshRenderer.enabled = value;
#endif
        }

        bool useLayers
        {
            get {
                return _vectorGraphics.useLayers;
            }
        }

        protected bool _lastUseSharedMesh;
        bool useSharedMesh
        {
            get {
                return !useLayers && _color == Color.white && _type == Type.Simple;
            }
        }

        // Mesh has changed
        void InitMesh()
        {
//            Debug.Log("InitMesh");
            if(_vectorGraphics == null)
            {
                _lastVectorGraphics = null;
                Clear();
            } else {
                if(useLayers)
                {
                    _layers = _vectorGraphics.layersClone;

                    if(_mesh == null)
                    {
                        _mesh = new Mesh();
                        _mesh.hideFlags = HideFlags.DontSave;
                    } else {
                        _mesh.Clear();
                    }

                    _mesh.name = _vectorGraphics.name + " Instance "+_mesh.GetInstanceID();
                    meshFilter.sharedMesh = _mesh;
                } else {
                    CleanMesh();
                    if(_sharedMesh != _vectorGraphics.sharedMesh)
                        _sharedMesh = _vectorGraphics.sharedMesh;
                    if(useSharedMesh)
                    {
                        if(meshFilter.sharedMesh != _sharedMesh)
                            meshFilter.sharedMesh = _sharedMesh;
                    } else {
                        if(_mesh == null)
                        {
                            _mesh = new Mesh();
                            _mesh.hideFlags = HideFlags.DontSave;
                        } else {
                            _mesh.Clear();
                        }

                        SVGMeshUtils.Fill(_vectorGraphics.sharedMesh, _mesh);
                        _mesh.name += " Instance "+_mesh.GetInstanceID();
                        if(meshFilter.sharedMesh != _mesh) 
                            meshFilter.sharedMesh = _mesh;
                    }
                }
            }
        }

        public void AddModifier(ISVGModify modifier)
        {
            if(_modifiers.Contains(modifier)) return;
            _modifiers.Add(modifier);
        }

        public void RemoveModifier(ISVGModify modifier)
        {
            if(!_modifiers.Contains(modifier)) return;
            _modifiers.Remove(modifier);
        }

        /// <summary>
        /// Is this renderer visible in any camera? (Read Only)
        /// </summary>
        public bool isVisible
        {
            get {
                return _meshRenderer.isVisible;
            }
        }

        protected List<ISVGModify> _modifiers = new List<ISVGModify>();
        public List<ISVGModify> modifiers
        {
            get {
                return _modifiers;
            }
        }

        protected void Clear()
        {
            CleanMaterials();
            CleanMesh();
            CleanLayers();
            CleanCache();
        }
        
        void CleanMaterials()
        {
//            Debug.Log("CleanMaterials");
            meshRenderer.sharedMaterials = new Material[0];
        }
        
        void CleanMesh()
        {
            if(_mesh != null) _mesh.Clear();            
        }

        void CleanLayers()
        {
            if(_layers != null) _layers = null;
        }

        void CleanCache()
        {
            if(_cachedColors != null) _cachedColors = null;
            if(_finalColors != null) _finalColors = null;
            if(_cachedVertices != null) _cachedVertices = null;
            if(_finalVertices != null) _finalVertices = null;
        }

        void DestroyArray<T>(T[] array) where T : UnityEngine.Object
        {
            if(array == null)
                return;
            foreach(T item in array)
            {
                if(item == null)
                    continue;
                DestroyObjectInternal(item);
            }
        }

        void DestroyObjectInternal(Object obj)
        {
            if(obj == null)
                return;
            
#if UNITY_EDITOR
            if(!UnityEditor.AssetDatabase.Contains(obj))
            {
                if(UnityEditor.EditorApplication.isPlaying)
                {
                    Destroy(obj);
                } else {
                    DestroyImmediate(obj);
                }
            }
#else
            Destroy(obj);
#endif
        }
    }
}
