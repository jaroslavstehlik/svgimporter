

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

using System.Collections;
using System.Collections.Generic;

namespace SVGImporter
{
    using Utils;
    using Rendering;
    using Geometry;
    
    [ExecuteInEditMode]
    [HelpURL(SVGDocumentation.url)]
    [AddComponentMenu("Rendering/SVG Renderer", 20)]
    public class SVGRenderer : UIBehaviour, ISVGShape, ISVGRenderer, ISVGAtlasClient
    {

        /// <summary>Raised when the renderer's SVG Asset changes or is reimported, with the new asset (or null).</summary>
        public event System.Action<SVGAsset> vectorGraphicsChanged;

        /// <summary>
        /// Raised before the renderer builds its mesh from a copy of the asset's layers, which
        /// handlers may change; modifiers do. Only for assets imported with Use Layers.
        /// </summary>
        public event System.Action<SVGLayer[], SVGAsset> willBuildMesh;

        private SVGDrawMode _lastType;
        [FormerlySerializedAs("type")]
        [SerializeField] private SVGDrawMode _type = SVGDrawMode.Simple;
        /// <summary>Simple, or Sliced to fit the RectTransform's size by stretching between the asset's border.</summary>
        public SVGDrawMode drawMode
        {
            get { return _type; }
            set {
                if(_type == value) return;
                _type = value;
                if(!meshRenderer.isPartOfStaticBatch) PrepareForRendering();
            }
        }

        // Tracking of modified assets
        private Rect _rectTransformRect;
        private Rect _lastRectTransformRect;

        // Tracking of mesh change
        private int _lastFrameChanged;
        internal int lastFrameChanged
        {
            get {
                return _lastFrameChanged;
            }
        }

        // Serialized SVG Asset
        [FormerlySerializedAs("vectorGraphics")]
        [SerializeField]
        private SVGAsset _vectorGraphics;
        private SVGAsset _lastVectorGraphics;

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
        private Color _color = Color.white;
        private Color _lastColor = Color.white;
        private Color32[] _cachedColors;
        private Vector3[] _cachedVertices;

        /// <summary>
        /// Rendering color for the SVG Asset..
        /// </summary>
        public Color color
        {
            get {
                return _color;
            }
            set {
                if(_color == value) return;
                _color = value;
                if(!meshRenderer.isPartOfStaticBatch) PrepareForRendering();
            }
        }
        
        [FormerlySerializedAs("opaqueMaterial")]
        [SerializeField]
        private Material _opaqueMaterial;
        private Material _lastOpaqueMaterial;
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
        private Material _transparentMaterial;
        private Material _lastTransparentMaterial;
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

        [SerializeField]
        private bool _receive2DLights = true;
        private bool _lastReceive2DLights = true;
        /// <summary>
        /// Whether the URP 2D Renderer's 2D lights light the renderer, as they light sprites with the
        /// default lit material. Off draws it unlit, like the Sprite-Unlit material. Other render
        /// pipelines and renderers always draw SVGs unlit. Override materials ignore this.
        /// </summary>
        public bool receive2DLights
        {
            get {
                return _receive2DLights;
            }
            set {
                if(_receive2DLights != value)
                {
                    _receive2DLights = value;
                    UpdateMaterials();
                }
            }
        }

        [SerializeField]
        private SpriteMaskInteraction _maskInteraction = SpriteMaskInteraction.None;
        private SpriteMaskInteraction _lastMaskInteraction = SpriteMaskInteraction.None;
        /// <summary>
        /// How the renderer interacts with Sprite Masks, as a sprite's does: visible only inside
        /// masks, only outside them, or regardless of them. A masked renderer draws all of its
        /// asset with the transparent material, after the masks. Override materials ignore this.
        /// </summary>
        public SpriteMaskInteraction maskInteraction
        {
            get {
                return _maskInteraction;
            }
            set {
                if(_maskInteraction != value)
                {
                    _maskInteraction = value;
                    UpdateMaterials();
                }
            }
        }

        private MeshFilter _meshFilter;
        public MeshFilter meshFilter {
            get
            {
                if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
                return _meshFilter;
            }
        }

        private MeshRenderer _meshRenderer;
        public MeshRenderer meshRenderer {
            get {
                if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
                return _meshRenderer;
            }
        }

        internal RectTransform rectTransform
        {
            get {
                return transform as RectTransform;
            }
        }

        // Cached SVG Layers
        private SVGLayer[] _layers;
        // The asset's layers as the renderer draws them, before modifiers and tints change _layers.
        [System.NonSerialized] SVGLayer[] _layersSource;
        // Shared mesh for better gpu instancing
        private Mesh _sharedMesh;
        // Instanced mesh for custom coloring
        private Mesh _mesh;
        [System.NonSerialized] long _lastImportHash;

        // Sorting and lighting: stored here and applied to the MeshRenderer, which this component
        // owns, keeps hidden and recreates when it's missing.
        [FormerlySerializedAs("sortingLayerID")]
        [SerializeField] private int _sortingLayerID = 0;
        [FormerlySerializedAs("sortingOrder")]
        [SerializeField] private int _sortingOrder = 0;
        [SerializeField] private ShadowCastingMode _shadowCastingMode = ShadowCastingMode.Off;
        [SerializeField] private bool _receiveShadows = false;
        [SerializeField] private LightProbeUsage _lightProbeUsage = LightProbeUsage.Off;
        [SerializeField] private RenderingLayerMask _renderingLayerMask = RenderingLayerMask.defaultRenderingLayerMask;
        // 0 in renderers saved before these settings moved here, which were set on the visible
        // MeshRenderer: they're copied from it once.
        [SerializeField] private int _rendererSettingsVersion = 0;

        /// <summary>The renderer's sorting layer, by ID.</summary>
        public int sortingLayerID
        {
            get { return _sortingLayerID; }
            set {
                _sortingLayerID = SortingLayer.IsValid(value) ? value : 0;
                meshRenderer.sortingLayerID = _sortingLayerID;
            }
        }

        /// <summary>The renderer's sorting layer, by name.</summary>
        public string sortingLayerName
        {
            get { return SortingLayer.IDToName(_sortingLayerID); }
            set { sortingLayerID = SortingLayer.NameToID(value); }
        }

        /// <summary>The renderer's order within its sorting layer.</summary>
        public int sortingOrder
        {
            get { return _sortingOrder; }
            set {
                _sortingOrder = value;
                meshRenderer.sortingOrder = value;
            }
        }

        /// <summary>Whether the renderer casts shadows. Off by default, as for sprites.</summary>
        public ShadowCastingMode shadowCastingMode
        {
            get { return _shadowCastingMode; }
            set { _shadowCastingMode = value; meshRenderer.shadowCastingMode = value; }
        }

        /// <summary>Whether the renderer receives shadows.</summary>
        public bool receiveShadows
        {
            get { return _receiveShadows; }
            set { _receiveShadows = value; meshRenderer.receiveShadows = value; }
        }

        /// <summary>How the renderer uses light probes.</summary>
        public LightProbeUsage lightProbeUsage
        {
            get { return _lightProbeUsage; }
            set { _lightProbeUsage = value; meshRenderer.lightProbeUsage = value; }
        }

        /// <summary>The rendering layers the renderer is on, for lights and effects that filter by them.</summary>
        public RenderingLayerMask renderingLayerMask
        {
            get { return _renderingLayerMask; }
            set { _renderingLayerMask = value; meshRenderer.renderingLayerMask = value; }
        }

        void ApplyRendererSettings()
        {
            MeshRenderer target = _meshRenderer;
            if (target == null) return;
            if (target.sortingLayerID != _sortingLayerID) target.sortingLayerID = _sortingLayerID;
            if (target.sortingOrder != _sortingOrder) target.sortingOrder = _sortingOrder;
            if (target.shadowCastingMode != _shadowCastingMode) target.shadowCastingMode = _shadowCastingMode;
            if (target.receiveShadows != _receiveShadows) target.receiveShadows = _receiveShadows;
            if (target.lightProbeUsage != _lightProbeUsage) target.lightProbeUsage = _lightProbeUsage;
            if (target.renderingLayerMask != (uint)_renderingLayerMask) target.renderingLayerMask = _renderingLayerMask;
        }

        /// <summary>
        /// Get SVG Outline, part of the ISVGPath interface..
        /// </summary>
        SVGPath[] ISVGShape.shape
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

            Clear();
            PrepareForRendering(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SVGAtlas.Register(this);
            // After a script reload the atlas and the meshes built from it are new: rebuild.
            if (!meshRenderer.isPartOfStaticBatch)
            {
                CleanLayers();
                PrepareForRendering(true);
            }
            EnableMeshRenderer(true);
        }

        void ISVGAtlasClient.OnAtlasReset()
        {
            if (meshRenderer.isPartOfStaticBatch) return;
            CleanLayers();
            PrepareForRendering(true);
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
            SVGAtlas.Unregister(this);
            EnableMeshRenderer(false);
            base.OnDisable();
        }

        // The SVG Opaque Sorting Group above this renderer, kept to re-sort it when the renderer
        // leaves. Groups learn about changes to their own children; renderers report the rest:
        // being moved anywhere below a group, and changes to their own children.
        [System.NonSerialized] SVGOpaqueSortingGroup _sortingGroup;

        void MarkSortingGroupDirty()
        {
            SVGOpaqueSortingGroup group = GetComponentInParent<SVGOpaqueSortingGroup>(true);
            if(_sortingGroup != null && _sortingGroup != group) _sortingGroup.SetDirty();
            if(group != null) group.SetDirty();
            _sortingGroup = group;
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            MarkSortingGroupDirty();
        }

        void OnTransformChildrenChanged()
        {
            MarkSortingGroupDirty();
        }

        protected override void OnDestroy()
        {
            if(_sortingGroup != null) _sortingGroup.SetDirty();
            // The renderer's own mesh, for tinted, sliced or layered assets.
            DestroyObjectInternal(_mesh);
            _mesh = null;
#if UNITY_EDITOR
            // Removing the component in the Editor removes the hidden components it added. Later,
            // because Unity doesn't allow destroying them while it destroys this one; not when the
            // whole object goes, as in closing a scene.
            if (!Application.isPlaying)
            {
                GameObject owner = gameObject;
                MeshFilter filter = _meshFilter;
                MeshRenderer renderer = _meshRenderer;
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (owner == null || owner.GetComponent<SVGRenderer>() != null) return;
                    if (renderer != null && (renderer.hideFlags & HideFlags.HideInInspector) != 0) DestroyImmediate(renderer);
                    if (filter != null && (filter.hideFlags & HideFlags.HideInInspector) != 0) DestroyImmediate(filter);
                };
            }
#endif
            base.OnDestroy();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyRendererSettings();
        }
#endif

        // The MeshFilter and MeshRenderer this component draws with, hidden as a sprite's are; they're
        // added when missing and removed with the component (see OnDestroy).
        void CacheComponents()
        {
            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
                if (_meshFilter == null)
                    _meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            bool existed = true;
            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
                if (_meshRenderer == null)
                {
                    _meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    existed = false;
                }
            }

            if (_rendererSettingsVersion == 0)
            {
                // Renderers saved before 2.0 set these on the MeshRenderer; new ones use the defaults.
                if (existed)
                {
                    _shadowCastingMode = _meshRenderer.shadowCastingMode;
                    _receiveShadows = _meshRenderer.receiveShadows;
                    _lightProbeUsage = _meshRenderer.lightProbeUsage;
                    _renderingLayerMask = _meshRenderer.renderingLayerMask;
                }
                _rendererSettingsVersion = 1;
            }

            Hide(_meshFilter);
            Hide(_meshRenderer);
            ApplyRendererSettings();
        }

        static void Hide(Component component)
        {
            if ((component.hideFlags & HideFlags.HideInInspector) == 0)
                component.hideFlags |= HideFlags.HideInInspector;
        }
        
        internal void UpdateRenderer()
        {
            PrepareForRendering(true);
        }
        
        protected override void OnDidApplyAnimationProperties()
        {
            if (!meshRenderer.isPartOfStaticBatch)
                PrepareForRendering();
        }
        
        // This is the main rendering method
        private void PrepareForRendering(bool force = false)
        {
            SVGAtlas.BeginRebuild();
            try { Rebuild(force); }
            finally { SVGAtlas.EndRebuild(); }
        }

        void Rebuild(bool force)
        {
            if(_vectorGraphics == null)
            {
                if(_lastVectorGraphics != null)
                {
                    _lastVectorGraphics = null;
                    if(vectorGraphicsChanged != null)
                        vectorGraphicsChanged(null);
                }
                Clear();
            } else {
                // Cached vertices and colours are copies of the asset's mesh: start over when the
                // asset changes or everything is rebuilt.
                if(force || _lastVectorGraphics != _vectorGraphics)
                {
                    CleanCache();
                    _sharedMesh = null;
                }
                bool meshChanged = force || _lastType != _type || meshFilter.sharedMesh == null;
                bool colorChanged = force || _lastColor != _color;
                bool materialChanged = force || _lastOpaqueMaterial != _opaqueMaterial || _lastTransparentMaterial != _transparentMaterial || _lastReceive2DLights != _receive2DLights || _lastMaskInteraction != _maskInteraction;
#if UNITY_EDITOR
                // Materials go missing in the editor, for example when the atlas is recreated.
                meshRenderer.GetSharedMaterials(s_Materials);
                for(int i = 0; i < s_Materials.Count; i++)
                {
                    if(s_Materials[i] != null) continue;
                    materialChanged = true;
                    break;
                }
#endif

                // A different asset, or the same one reimported; not every forced rebuild.
                bool assetChanged = _lastVectorGraphics != _vectorGraphics || _vectorGraphics.importHash != _lastImportHash;
                _lastImportHash = _vectorGraphics.importHash;
                // The asset's thickness decides how far apart opaque sorting places renderers.
                if(assetChanged) MarkSortingGroupDirty();
                if(_lastVectorGraphics != _vectorGraphics)
                {
                    meshChanged = true;
                    colorChanged = true;
                    materialChanged = true;

                }

                if(useLayers || !useSharedMesh)
                {
                    if(_lastUseSharedMesh != false) meshChanged = true;
                    if(!meshChanged)
                    {
                        if(_type == SVGDrawMode.Sliced && rectTransform != null)
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
                    if(_layers == null) InitLayers();
                    if(meshChanged || colorChanged)
                    {
                        InitMesh();
                        materialChanged = true;
                        
                        if(_type == SVGDrawMode.Sliced)
                        {
                            UpdateSlicedMesh();
                        }
                        
                        UpdateColors(force);
                        _lastFrameChanged = Time.frameCount;
                        materialChanged = true;
                        
                        if(willBuildMesh != null)
                            willBuildMesh(_layers, _vectorGraphics);
                        
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
                            if(_type == SVGDrawMode.Sliced)
                            {
                                UpdateSlicedMesh();
                            }
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

                _lastVectorGraphics = _vectorGraphics;
                // Colliders and other listeners follow asset changes and reimports.
                if(assetChanged && vectorGraphicsChanged != null)
                    vectorGraphicsChanged(_vectorGraphics);

                _lastOpaqueMaterial = _opaqueMaterial;
                _lastTransparentMaterial = _transparentMaterial;
                _lastReceive2DLights = _receive2DLights;
                _lastMaskInteraction = _maskInteraction;
                _lastVectorGraphics = _vectorGraphics;
                _lastColor = _color;
                _lastType = _type;
                _lastUseSharedMesh = useSharedMesh;                
            }
        }

        void InitLayers()
        {
            _layersSource = _vectorGraphics.CloneLayers();
            _layers = _vectorGraphics.CloneLayers();
        }

        /// <summary>
        /// Rebuilds the mesh with the modifiers applied, as they animate: the layers are reset to
        /// the asset's in place and only the mesh is rebuilt. Anything else that changed, such as
        /// the asset, gets a full rebuild.
        /// </summary>
        internal void RebuildModifiedMesh()
        {
            if(_vectorGraphics == null || meshRenderer.isPartOfStaticBatch) return;
            if(!useLayers || _layers == null || _layersSource == null || _mesh == null || meshFilter.sharedMesh != _mesh
                || _lastVectorGraphics != _vectorGraphics || _vectorGraphics.importHash != _lastImportHash
                || _lastType != _type || _lastColor != _color)
            {
                PrepareForRendering(true);
                return;
            }

            ResetLayers(_layersSource, _layers);
            if(_type == SVGDrawMode.Sliced) UpdateSlicedMesh();
            UpdateColors();
            _lastFrameChanged = Time.frameCount;
            if(willBuildMesh != null)
                willBuildMesh(_layers, _vectorGraphics);
            GenerateMesh();
        }

        // Copies the source layers' values into the target's arrays, which have the same lengths.
        static void ResetLayers(SVGLayer[] source, SVGLayer[] target)
        {
            for(int i = 0; i < source.Length; i++)
            {
                SVGShape[] from = source[i].shapes, to = target[i].shapes;
                if(from == null) continue;
                for(int j = 0; j < from.Length; j++)
                {
                    SVGShape shape = from[j];
                    shape.vertices = CopyInto(from[j].vertices, to[j].vertices);
                    shape.triangles = CopyInto(from[j].triangles, to[j].triangles);
                    shape.colors = CopyInto(from[j].colors, to[j].colors);
                    shape.angles = CopyInto(from[j].angles, to[j].angles);
                    shape.fill = to[j].fill;
                    if(shape.fill != null && from[j].fill != null) shape.fill.CopyFrom(from[j].fill);
                    to[j] = shape;
                }
            }
        }

        static T[] CopyInto<T>(T[] from, T[] to)
        {
            if(from == null) return null;
            if(to == null || to.Length != from.Length) return (T[])from.Clone();
            System.Array.Copy(from, to, from.Length);
            return to;
        }

        [System.NonSerialized] SVGMeshBuffers _meshBuffers;
        private void GenerateMesh()
        {
            if(_meshBuffers == null) _meshBuffers = new SVGMeshBuffers();
            string[] shaders;
            SVGMesh.CombineMeshes(_layers, _mesh, out shaders, _vectorGraphics.useGradients, _vectorGraphics.format, _vectorGraphics.compressDepth, _vectorGraphics.softEdges, _vectorGraphics.depthOffset, _meshBuffers);
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

        private Color32[] _finalColors;
        private void UpdateColors(bool force = false)
        {
            if(_color == Color.white)
            {
                // Back to the asset's own colours on a mesh that was tinted before.
                if(!useLayers && _mesh != null && _sharedMesh != null && meshFilter.sharedMesh == _mesh && _mesh.vertexCount == _sharedMesh.vertexCount)
                    _mesh.colors32 = _sharedMesh.colors32;
                return;
            }
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
                            _layers[i].shapes[j].fill.blend = SVGFillBlend.Transparent;
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
        private float pixelsPerUnit
        {
            get
            {
                float spritePixelsPerUnit = 100;
                return spritePixelsPerUnit;
            }
        }

        private float InverseLerp(float from, float to, float value)
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

        private float SafeDivide(float a, float b)
        {
            if(b == 0) return 0f;
            return a / b;
        }
        
        private string BorderToString(Vector4 border)
        {
            return string.Format("left: {0}, bottom: {1}, right: {2}, top: {3}", border.x, border.y, border.z, border.w);
        }

        const float epsilon = 0.0000001f;
        private Vector3[] _finalVertices;
        private void UpdateSlicedMesh()
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

        private void SwapMaterials(bool transparent = true)
        {
            if(_vectorGraphics == null)
            {
                CleanMaterials();
                return;
            }
            bool hasGradients = _vectorGraphics.hasGradients || _vectorGraphics.useGradients == SVGUseGradients.Always;
            Material sharedOpaqueMaterial = SVGAtlas.Instance.GetOpaqueMaterial(hasGradients, _receive2DLights, _maskInteraction);
            Material sharedTransparentMaterial = SVGAtlas.Instance.GetTransparentMaterial(_vectorGraphics.softEdges, hasGradients, _receive2DLights, _maskInteraction);

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

        // Scratch list for reading and setting the renderer's materials without allocating.
        static readonly List<Material> s_Materials = new List<Material>(2);

        void SetSharedMaterials(int subMeshCount, Material firstMaterial, Material secondMaterial)
        {
            meshRenderer.GetSharedMaterials(s_Materials);
            int count = subMeshCount < 2 ? 1 : 2;
            if(s_Materials.Count == count && s_Materials[0] == firstMaterial && (count == 1 || s_Materials[1] == secondMaterial))
                return;
            s_Materials.Clear();
            s_Materials.Add(firstMaterial);
            if(count == 2) s_Materials.Add(secondMaterial);
            meshRenderer.SetSharedMaterials(s_Materials);
        }

        internal void UpdateMaterials()
        {
            if(_opaqueMaterial != null) SVGAtlas.Instance.UpdateMaterialProperties(_opaqueMaterial);
            if(_transparentMaterial != null) SVGAtlas.Instance.UpdateMaterialProperties(_transparentMaterial);
            // Sprite Masks are transparent, so masked renderers draw in the transparent queue, after
            // them, as sprites do.
            SwapMaterials(_color.a != 1 || _maskInteraction != SpriteMaskInteraction.None);
        }

        /// <summary>Rebuilds the renderer's mesh and materials, for example after changing the asset's layers from a script.</summary>
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

        private bool _lastUseSharedMesh;
        bool useSharedMesh
        {
            get {
                return !useLayers && _color == Color.white && _type == SVGDrawMode.Simple;
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
                    InitLayers();

                    if(_mesh == null)
                    {
                        _mesh = new Mesh();
                        _mesh.hideFlags = HideFlags.DontSave;
                    } else {
                        _mesh.Clear();
                    }

                    _mesh.name = _vectorGraphics.name + " Instance";
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
                        _mesh.name += " Instance";
                        if(meshFilter.sharedMesh != _mesh) 
                            meshFilter.sharedMesh = _mesh;
                    }
                }
            }
        }

        internal void AddModifier(SVGModifier modifier)
        {
            if(_modifiers.Contains(modifier)) return;
            _modifiers.Add(modifier);
        }

        internal void RemoveModifier(SVGModifier modifier)
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

        readonly List<SVGModifier> _modifiers = new List<SVGModifier>();
        internal List<SVGModifier> modifiers
        {
            get {
                return _modifiers;
            }
        }

        private void Clear()
        {
            CleanMaterials();
            CleanMesh();
            CleanLayers();
            CleanCache();
        }
        
        void CleanMaterials()
        {
//            Debug.Log("CleanMaterials");
            s_Materials.Clear();
            meshRenderer.SetSharedMaterials(s_Materials);
        }
        
        void CleanMesh()
        {
            if(_mesh != null) _mesh.Clear();            
        }

        void CleanLayers()
        {
            _layers = null;
            _layersSource = null;
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
