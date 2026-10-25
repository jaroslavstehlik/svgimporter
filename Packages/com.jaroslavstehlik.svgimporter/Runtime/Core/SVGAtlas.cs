

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SVGImporter
{
    using Rendering;
	using Utils;

    /// <summary>
    /// The gradients of an atlas, by slot, and an index of them by content (GradientKey): a
    /// gradient that draws the same as a registered one gets that one's slot. Also used during
    /// import, where the slots become the asset's gradient indices.
    /// </summary>
    public class SVGAtlasData
    {
        public CCGradient[] gradients;
        Dictionary<GradientKey, CCGradient> byContent;
        private static readonly CCGradient DefaultGradient = GetDefaultGradient();

        public void Init(int length)
        {
            gradients = new CCGradient[length];
            byContent = new Dictionary<GradientKey, CCGradient>();
        }

        /// <summary>The registered gradients, in slot order.</summary>
        public CCGradient[] registeredGradients
        {
            get { return gradients == null ? new CCGradient[0] : gradients.Where(g => g != null).ToArray(); }
        }

        /// <summary>Rebuilds the content index from the slots.</summary>
        public void RebuildGradientCache()
        {
            byContent = new Dictionary<GradientKey, CCGradient>();
            if (gradients == null) return;
            foreach (CCGradient gradient in gradients)
            {
                if (gradient == null || !gradient.initialised) continue;
                GradientKey key = new GradientKey(gradient);
                if (!byContent.ContainsKey(key)) byContent.Add(key, gradient);
            }
        }

        [System.Obsolete("Use RebuildGradientCache.")]
        public void InitGradientCache() { if (byContent == null) RebuildGradientCache(); }

        [System.Obsolete("Use RebuildGradientCache.")]
        public void ClearGradientCache() { byContent = null; }

        public static CCGradient GetDefaultGradient()
        {
            CCGradientColorKey[] colorKeys = new CCGradientColorKey[]{
                new CCGradientColorKey(Color.white, 0f), new CCGradientColorKey(Color.white, 1f)
            };
            CCGradientAlphaKey[] alphaKeys = new CCGradientAlphaKey[]{
                new CCGradientAlphaKey(1f, 0f), new CCGradientAlphaKey(1f, 1f)
            };            
            return new CCGradient(colorKeys, alphaKeys);
        }

        public static bool IsDefaultGradient(CCGradient gradient)
        {
            return DefaultGradient.GradientEquals(gradient);
        }

        public CCGradient AddGradient(CCGradient gradient)
        {
            bool gradientExist;
            return AddGradient(gradient, out gradientExist);
        }

        /// <summary>
        /// Returns the registered gradient that draws the same as gradient, or registers gradient
        /// in a free slot (setting its index). Null if there is no free slot.
        /// </summary>
        public CCGradient AddGradient(CCGradient gradient, out bool gradientExist)
        {
            gradientExist = false;
            if (gradient == null || !gradient.initialised || gradients == null)
                return null;
            if (byContent == null)
                RebuildGradientCache();

            GradientKey key = new GradientKey(gradient);
            CCGradient registered;
            if (byContent.TryGetValue(key, out registered))
            {
                gradientExist = true;
                return registered;
            }
            int slot = System.Array.IndexOf(gradients, null);
            if (slot < 0)
                return null; // Full: SVGAtlas grows the atlas and tries again.
            gradient.index = slot;
            gradients[slot] = gradient;
            byContent.Add(key, gradient);
            return gradient;
        }

        /// <summary>Adds empty slots at the end; existing gradients keep theirs.</summary>
        public void Grow(int length)
        {
            if (gradients == null || length <= gradients.Length) return;
            System.Array.Resize(ref gradients, length);
        }

        public bool RemoveGradient(CCGradient gradient)
        {
            if (gradient == null || !gradient.initialised || gradients == null || byContent == null)
                return false;
            GradientKey key = new GradientKey(gradient);
            CCGradient registered;
            if (!byContent.TryGetValue(key, out registered))
                return false;
            byContent.Remove(key);
            if (registered.index >= 0 && registered.index < gradients.Length && gradients[registered.index] == registered)
                gradients[registered.index] = null;
            return true;
        }
        
        public CCGradient GetGradient (int index)
        {
            index = Mathf.Clamp (index, 0, gradients.Length - 1);
            return gradients [index];
        }

        public SVGFill GetGradient (SVGFill gradient)
        {
            gradient.gradientColors = GetGradient (gradient.gradientColors);
            return gradient;
        }

        /// <summary>The registered gradient that draws the same as gradient, or null.</summary>
        public CCGradient GetGradient (CCGradient gradient)
        {
            if (gradient == null || !gradient.initialised || gradients == null)
                return null;
            if (byContent == null)
                RebuildGradientCache();
            CCGradient registered;
            return byContent.TryGetValue(new GradientKey(gradient), out registered) ? registered : null;
        }

        public bool HasGradient (CCGradient gradient)
        {
            return GetGradient(gradient) != null;
        }

        public void Clear()
        {
            gradients = null;
            byContent = null;
        }
    }

    /// <summary>Renderers that draw meshes or materials from the atlas; see SVGAtlas.</summary>
    internal interface ISVGAtlasClient
    {
        /// <summary>The atlas was replaced: rebuild meshes and materials taken from it.</summary>
        void OnAtlasReset();
    }

    /// <summary>
    /// All gradients of the loaded SVG Assets share one texture, so that SVG meshes share materials
    /// and batch. Each gradient has a slot in the texture, and the meshes drawn with the atlas store
    /// slot numbers (see SVGAsset.runtimeMesh).
    ///
    /// The atlas is a cache that can be thrown away and rebuilt at any time: its slot registry isn't
    /// serialized. Anything built from it records the atlas generation it was built for:
    /// - Script reloads destroy the atlas and everything built from it; after the reload renderers
    ///   rebuild in OnEnable and the atlas is created again on demand.
    /// - When the atlas is replaced within a session (it can't grow further, or ResetAtlas), the
    ///   generation changes and every registered renderer (ISVGAtlasClient) rebuilds immediately.
    /// - The atlas grows instead of running out of slots. Gradients are never removed while it
    ///   exists, so a slot never changes meaning.
    /// </summary>
    [ExecuteInEditMode]
    public class SVGAtlas : MonoBehaviour {

        /// <summary>
        /// Changes whenever a new atlas is created, so slot numbers and materials from an earlier
        /// atlas can be recognised.
        /// </summary>
        public static int generation { get; private set; }

        static readonly HashSet<ISVGAtlasClient> clients = new HashSet<ISVGAtlasClient>();
        // Meshes holding slot numbers of the current atlas; destroyed with it.
        static readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();

        internal static void Register(ISVGAtlasClient client) { clients.Add(client); }
        internal static void Unregister(ISVGAtlasClient client) { clients.Remove(client); }
        internal static void Own(UnityEngine.Object obj) { ownedObjects.Add(obj); }

        /// <summary>Rebuilds the renderers that draw any of these assets, for example after a reimport.</summary>
        public static void RefreshRenderers(ICollection<SVGAsset> assets)
        {
            if (assets == null || assets.Count == 0) return;
            foreach (ISVGAtlasClient client in clients.ToArray())
            {
                ISVGRenderer renderer = client as ISVGRenderer;
                if (renderer != null && renderer.vectorGraphics != null && assets.Contains(renderer.vectorGraphics))
                    client.OnAtlasReset();
            }
        }

        protected bool _atlasHasChanged;
        public bool atlasHasChanged
        {
            get {
                return _atlasHasChanged;
            }
        }

        protected static bool _beingDestroyed;
        public static bool beingDestroyed
        {
            get {
                return _beingDestroyed;
            }
        }

		protected static Texture2D _whiteTexture;
		public static Texture2D whiteTexture
		{
			get {
				if(_whiteTexture == null) _whiteTexture = GenerateWhiteTexture();
				return _whiteTexture;
			}
		}

        /// <summary>No longer used: shaders compute gradient positions themselves.</summary>
        [System.Obsolete("Gradient shaders compute gradient positions; there is no gradient shape texture any more.")]
        public static Texture2D gradientShapeTexture
        {
            get { return null; }
        }

        [System.Obsolete("Gradient shaders compute gradient positions; there is no gradient shape texture any more.")]
        public static int gradientShapeTextureSize { get { return 0; } set { } }

        [System.Obsolete("Gradient shaders compute gradient positions; there is no gradient shape texture any more.")]
        public static void ClearGradientShapeTexture () { }

        //todo Atlas data gets deleted!!!
        protected SVGAtlasData _atlasData;
        public SVGAtlasData atlasData
        {
            get {
                return _atlasData;
            }
        }

        protected Material _ui;
        public Material ui 
        {
            get {
                if(_ui == null)
                {
                    _ui = new Material(SVGShader.UI);
                    _ui.hideFlags = HideFlags.DontSave;
                }
                return _ui;
            }
        }
        
        protected Material _uiAntialiased;
        public Material uiAntialiased 
        {
            get {
                if(_uiAntialiased == null)
                {
                    _uiAntialiased = new Material(SVGShader.UIAntialiased);
                    _uiAntialiased.hideFlags = HideFlags.DontSave;
                }
                return _uiAntialiased;
            }
        }

        protected Material _opaqueSolid;
        public Material opaqueSolid
        {
            get {
                if(_opaqueSolid == null)
                {
                    _opaqueSolid = new Material(SVGShader.SolidColorOpaque);
                    _opaqueSolid.hideFlags = HideFlags.DontSave;
                }
                return _opaqueSolid;
            }
        }

        protected Material _transparentSolid;
        public Material transparentSolid
        {
            get {
                if(_transparentSolid == null)
                {
                    _transparentSolid = new Material(SVGShader.SolidColorAlphaBlended);
                    _transparentSolid.hideFlags = HideFlags.DontSave;
                }
                return _transparentSolid;
            }
        }

        protected Material _transparentSolidAntialiased;
        public Material transparentSolidAntialiased
        {
            get {
                if(_transparentSolidAntialiased == null)
                {
                    _transparentSolidAntialiased = new Material(SVGShader.SolidColorAlphaBlendedAntialiased);
                    _transparentSolidAntialiased.hideFlags = HideFlags.DontSave;
                }
                return _transparentSolidAntialiased;
            }
        }

        protected Material _opaqueGradient;
        public Material opaqueGradient
        {
            get {
                if(_opaqueGradient == null)
                {
                    _opaqueGradient = new Material(SVGShader.GradientColorOpaque);
                    _opaqueGradient.hideFlags = HideFlags.DontSave;
                }
                return _opaqueGradient;
            }
        }

        protected Material _transparentGradient;
        public Material transparentGradient
        {
            get {
                if(_transparentGradient == null)
                {
                    _transparentGradient = new Material(SVGShader.GradientColorAlphaBlended);
                    _transparentGradient.hideFlags = HideFlags.DontSave;
                }
                return _transparentGradient;
            }
        }
        
        protected Material _transparentGradientAntialiased;
        public Material transparentGradientAntialiased
        {
            get {
                if(_transparentGradientAntialiased == null)
                {
                    _transparentGradientAntialiased = new Material(SVGShader.GradientColorAlphaBlendedAntialiased);
                    _transparentGradientAntialiased.hideFlags = HideFlags.DontSave;
                }
                return _transparentGradientAntialiased;
            }
        }

        /// <summary>Kept for compatibility: the atlas is bound globally (see BindGlobals), not per material.</summary>
        public void UpdateMaterialProperties(Material material)
        {
        }

        /// <summary>Global shader properties: the atlas texture and its layout.</summary>
        public const string globalAtlasKey = "_SVGGradientAtlas";
        public const string globalAtlasParamsKey = "_SVGGradientAtlasParams";

        /// <summary>
        /// Binds the atlas for every SVG material, including custom ones: one texture serves all of
        /// them, so it is a global shader property rather than a material property.
        /// </summary>
        void BindGlobals()
        {
            if (atlasTextures == null || atlasTextures.Count == 0 || atlasTextures[0] == null) return;
            Shader.SetGlobalTexture(globalAtlasKey, atlasTextures[0]);
            Shader.SetGlobalVector(globalAtlasParamsKey, new Vector4(atlasTextureWidth, atlasTextureHeight, gradientWidth, gradientHeight));
        }

    	public List<Texture2D> atlasTextures;		
    	public List<Material> materials;
    	
        public const int defaultGradientWidth = 128;
        public const int defaultGradientHeight = 4;
        public const int defaultAtlasTextureWidth = 512;
        public const int defaultAtlasTextureHeight = 64;
        // The atlas doubles in height when it runs out of slots, up to this height.
        const int maxAtlasTextureHeight = 4096;
        const int atlasIndex = 0;

    	public int gradientWidth = 128;
    	public int gradientHeight = 4;
        public int atlasTextureWidth = defaultAtlasTextureWidth;
        public int atlasTextureHeight = defaultAtlasTextureHeight;
    	
        protected void Awake()
        {
            if(Application.isPlaying)
                DontDestroyOnLoad(gameObject);
            _atlasHasChanged = false;
            _beingDestroyed = false;

            // The atlas must be up to date before anything renders. The Built-in pipeline
            // raises Camera.onPreRender, scriptable pipelines (URP) raise
            // beginContextRendering instead, and UI-only scenes may have neither.
            Camera.onPreRender += OnAtlasPreRender;
            UnityEngine.Rendering.RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
            Canvas.willRenderCanvases += OnWillRenderCanvases;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorUpdate;
#endif
        }

#if UNITY_EDITOR
        private void EditorUpdate()
        {
            if (!Application.isPlaying) OnAtlasPreRender(Camera.current);
        }
#endif
        public void OnPreRender()
        {
            OnAtlasPreRender();
        }

        void OnBeginContextRendering(UnityEngine.Rendering.ScriptableRenderContext context, List<Camera> cameras)
        {
            OnAtlasPreRender();
        }

        void OnWillRenderCanvases()
        {
            OnAtlasPreRender();
        }

        protected void OnDestroy()
        {
            _beingDestroyed = true;
            Camera.onPreRender -= OnAtlasPreRender;
            UnityEngine.Rendering.RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
        }

        public void OnAtlasPreRender(Camera camera = null)
        {
            SVGImporterSettings.UpdateAntialiasing();

            if (_atlasHasChanged)
            {
                RebuildAtlas();
                _atlasHasChanged = false;
#if UNITY_EDITOR
                UpdateMaterialList();
#endif
            }
            BindGlobals();
        }

        protected static SVGAtlas _Instance;

        /// <summary>Whether an atlas exists, without creating one.</summary>
        internal static bool exists { get { return _Instance != null; } }
        public static SVGAtlas Instance
        {
            get {
                if(_Instance == null)
                {
                    // An atlas object that exists while _Instance is null was created before a
                    // script reload (or by an older version of SVG Importer): its slot registry is
                    // gone, so its slots can't be trusted. Renderers rebuild after a reload anyway.
                    foreach (SVGAtlas stale in Resources.FindObjectsOfTypeAll<SVGAtlas>())
                    {
                        if (stale != null && (stale.gameObject.hideFlags & HideFlags.DontSave) != 0)
                            stale.DestroyAtlas();
                    }

                    GameObject go = new GameObject("SVGAtlas", typeof(SVGAtlas));
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _Instance = go.GetComponent<SVGAtlas>();
                    _Instance.hideFlags = HideFlags.DontSave;
                    _Instance.Init();
                }
                
                return _Instance;
            }
        }

        public bool ContainsMaterial(Material material)
        {
            if(material == _ui) return true;
            if(material == _uiAntialiased) return true;
            if(material == _opaqueSolid) return true;
            if(material == _transparentSolid) return true;
            if(material == _transparentSolidAntialiased) return true;
            if(material == _opaqueGradient) return true;
            if(material == _transparentGradient) return true;
            if(material == _transparentGradientAntialiased) return true;

            if(materials != null) {
                if(materials.Contains(material)) return true;
            }

            return false;
        }

        public void UpdateMaterialList()
        {
            if(materials == null) materials = new List<Material>();
            materials.Clear();
            if(_ui != null) materials.Add(_ui);
            if(_uiAntialiased != null) materials.Add(_uiAntialiased);
            if(_opaqueSolid != null) materials.Add(_opaqueSolid);
            if(_transparentSolid != null) materials.Add(_transparentSolid);
            if(_opaqueGradient != null) materials.Add(_opaqueGradient);
            if(_transparentGradient != null) materials.Add(_transparentGradient);
            if(_transparentGradientAntialiased != null) materials.Add(_transparentGradientAntialiased);
        }

        public void UpdateGradientList()
        {

        }

        /// <summary>
        /// Replaces the atlas with an empty one. Every registered renderer rebuilds and registers
        /// the gradients it uses again.
        /// </summary>
        public static void ResetAtlas()
        {
            if (_Instance != null) _Instance.DestroyAtlas();
            foreach (ISVGAtlasClient client in clients.ToArray())
                client.OnAtlasReset();
        }

        [System.Obsolete("Use SVGAtlas.ResetAtlas().")]
        public void ClearAll()
        {
            ResetAtlas();
        }

        /// <summary>Destroys the atlas, its textures and materials, and the meshes built from it.</summary>
        void DestroyAtlas()
        {
            if (_Instance == this) _Instance = null;
            Material[] own = { _ui, _uiAntialiased, _opaqueSolid, _transparentSolid, _transparentSolidAntialiased, _opaqueGradient, _transparentGradient, _transparentGradientAntialiased };
            foreach (Material material in own)
                if (material != null) DestroyObjectInternal(material);
            ClearMaterials();
            ClearAtlasTextures();
            if (_atlasData != null) _atlasData.Clear();
            if (_Instance == null)
            {
                foreach (UnityEngine.Object obj in ownedObjects)
                    if (obj != null) DestroyObjectInternal(obj);
                ownedObjects.Clear();
            }
            DestroyObjectInternal(gameObject);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void DestroyBeforeScriptReload()
        {
            // The slot registry doesn't survive a script reload, so neither may anything that uses
            // slot numbers. Renderers rebuild in OnEnable after the reload.
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                if (_Instance != null) _Instance.DestroyAtlas();
            };
        }
#endif

    	protected void Init ()
    	{
//            Debug.Log("Inited SVG Atlas: "+Time.frameCount+", playmode: "+Application.isPlaying+" InstanceID: "+GetInstanceID());
    		if (materials == null) materials = new List<Material> ();
            generation++;
            if(_atlasData == null)
            {
                _atlasData = new SVGAtlasData();
                _atlasData.Init(slotCount);
                AddGradient(SVGAtlasData.GetDefaultGradient());
            }
    	}
    	
        /// <summary>The most stops a gradient keeps; gradients with more are resampled.</summary>
        public const int maxStops = 64;

        /// <summary>
        /// Writes a gradient's stops into its slot of the atlas (DESIGN.md, 7.1): row 0 holds two
        /// texels per stop, its colour (sRGB, straight alpha) and its offset; row 1 starts with the
        /// stop count. The gradient shaders find the stops around a position and interpolate.
        /// </summary>
        public static void RenderGradient (Texture2D texture, CCGradient gradient, int x, int y, int gradientWidth, int gradientHeight)
        {
            if (texture == null || gradient == null || !gradient.initialised)
                return;

            List<CCGradientStop> stops = LimitStops(gradient.GetStops(), Mathf.Min(maxStops, gradientWidth / 2));
            Color[] pixels = new Color[gradientWidth * gradientHeight];
            for (int i = 0; i < stops.Count; i++)
            {
                pixels[i * 2] = stops[i].color;
                pixels[i * 2 + 1] = new Color(stops[i].offset, 0f, 0f, 0f);
            }
            if (gradientHeight > 1)
                pixels[gradientWidth] = new Color(stops.Count, 0f, 0f, 0f);
            texture.SetPixels(x, y, gradientWidth, gradientHeight, pixels);
        }

        // Gradients with more stops than a slot holds are resampled evenly.
        static List<CCGradientStop> LimitStops(List<CCGradientStop> stops, int limit)
        {
            if (stops.Count == 0)
                stops.Add(new CCGradientStop(0f, Color.black));
            if (stops.Count <= limit)
                return stops;
            var resampled = new List<CCGradientStop>(limit);
            for (int i = 0; i < limit; i++)
            {
                float t = i / (float)(limit - 1);
                resampled.Add(new CCGradientStop(t, CCGradient.Evaluate(stops, t)));
            }
            return resampled;
        }

        int slotCount
        {
            get { return (atlasTextureWidth / gradientWidth) * (atlasTextureHeight / gradientHeight); }
        }

    	public int imagePerRow {
    		get {
    			return atlasTextureWidth / gradientWidth;
    		}
    	}

        public bool GetCoords (out int x, out int y, int imageIndex)
        {
            bool newTexture = (atlasTextures == null || atlasTextures.Count == 0);            
			GetCoords(out x, out y, imageIndex, gradientWidth, gradientHeight, atlasTextureWidth, atlasTextureHeight);
            return newTexture;
        }

		public static void GetCoords(out int x, out int y, int imageIndex, int gradientWidth, int gradientHeight, int atlasTextureWidth, int atlasTextureHeight)
		{
			int index = imageIndex * gradientWidth;
			x = index % atlasTextureWidth;
			y = Mathf.FloorToInt (index / atlasTextureWidth) * gradientHeight;
		}

        public Texture CreateAtlasTexture (int index, int width, int height)
        {
//            Debug.Log("CreateAtlasTexture");
            if (atlasTextures == null)
                atlasTextures = new List<Texture2D> ();

			Texture2D texture = CreateTexture(width, height);            
            texture.hideFlags = HideFlags.DontSave;
            texture.name = "Atlas "+index.ToString();


            if (index >= atlasTextures.Count - 1) {               
                atlasTextures.Add (texture);
            } else if (index >= 0) {                         
                atlasTextures [index] = texture;
            }

			return texture;
        }

		public static Texture2D CreateTexture(int width, int height)
		{
			// Stop data, not an image: half floats for the offsets, no sRGB decoding (the shaders
			// interpolate in sRGB) and no filtering.
			Texture2D texture = new Texture2D (width, height, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Clamp;
//			texture.alphaIsTransparency = true;
			texture.anisoLevel = 0;
			return texture;
		}

        public CCGradient AddGradient(CCGradient gradient)
        {
            if (gradient == null || !gradient.initialised)
                return null;

            if(_atlasData == null)
            {
                _atlasData = new SVGAtlasData();
                _atlasData.Init(slotCount);
            }
            bool gradientExist;
            CCGradient added = _atlasData.AddGradient(gradient, out gradientExist);
            if (added == null)
            {
                if (atlasTextureHeight * 2 > Mathf.Min(maxAtlasTextureHeight, SystemInfo.maxTextureSize))
                {
                    // Can't grow: start over with only the gradients renderers use now.
                    ResetAtlas();
                    return Instance.AddGradient(gradient);
                }
                Grow();
                added = _atlasData.AddGradient(gradient, out gradientExist);
            }
            gradient = added;
            if(gradientExist) return gradient;

            int x = 0, y = 0;
            GetCoords (out x, out y, gradient.index);

//            Debug.Log("AddGradient: x: "+x+", y: "+y);
            gradient.atlasIndex = atlasIndex;
            _atlasHasChanged = true;

            return  gradient;
        }
        
        /// <summary>
        /// Doubles the atlas height. Slots are numbered row by row, so existing slots keep their
        /// position; the texture is resized and every gradient written again.
        /// </summary>
        void Grow()
        {
            atlasTextureHeight *= 2;
            _atlasData.Grow(slotCount);
            if (atlasTextures != null)
            {
                foreach (Texture2D texture in atlasTextures)
                {
                    if (texture == null) continue;
                    texture.Reinitialize(atlasTextureWidth, atlasTextureHeight, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat, false);
                }
            }
            _atlasHasChanged = true;
        }

        public bool RemoveGradient(CCGradient gradient)
        {
            if (gradient == null || !gradient.initialised)
                return false;
            
            if(_atlasData == null) return false;
            if(!_atlasData.RemoveGradient(gradient))            
            {
                return false;
            }

//            Debug.Log("RemoveGradient");
            return  true;
        }

        public CCGradient GetGradient(CCGradient gradient)
        {
            if (gradient == null || !gradient.initialised)
                return null;
            
            if(_atlasData == null) return null;
            return _atlasData.GetGradient(gradient);
        }

        public bool HasGradient(CCGradient gradient)
        {
            if (gradient == null || !gradient.initialised)
                return false;
            
            if(_atlasData == null) return false;
            return _atlasData.HasGradient(gradient);;
        }

        public void RebuildAtlas ()
        {
//            long timeStart = System.DateTime.Now.Ticks;
            int atlasIndex = 0;

            if (_atlasData == null)
            {
                Debug.LogWarning("atlasData is null!", this);
                return;
            }
            CCGradient[] gradients = _atlasData.gradients;
            if(gradients == null) return;

            int x, y, gradientsLength = gradients.Length;
            for (int i = 0; i < gradientsLength; i++) {
                if(gradients [i] == null) continue;
                bool newTexture = GetCoords (out x, out y, gradients [i].index);
                if (newTexture) {
                    CreateAtlasTexture (atlasIndex, atlasTextureWidth, atlasTextureHeight);
                }
				RenderGradient (atlasTextures [atlasIndex], gradients [i], x, y, gradientWidth, gradientHeight);
            }
            
            for (int i = 0; i < atlasTextures.Count; i++) {
                atlasTextures [i].Apply (false);
            }

//            Debug.Log("RebuildAtlas");
            //Debug.Log("RebuildAtlas took: "+System.TimeSpan.FromTicks(System.DateTime.Now.Ticks - timeStart).Milliseconds+" ms");
        }

		public static Texture2D GenerateGradientAtlasTexture(CCGradient[] gradients, int gradientWidth, int gradientHeight)
		{
			if(gradients == null || gradients.Length == 0)
				return null;

			int gradientCount = gradients.Length;
			int atlasTextureWidth = gradientWidth * 2;
			int atlasTextureHeight = Mathf.CeilToInt((gradientCount * gradientWidth) / atlasTextureWidth) * gradientHeight + gradientHeight;
			Texture2D texture = CreateTexture(atlasTextureWidth, atlasTextureHeight);

			int x, y;
			for (int i = 0; i < gradients.Length; i++) {
				GetCoords(out x, out y, i, gradientWidth, gradientHeight, atlasTextureWidth, atlasTextureHeight);
				RenderGradient (texture, gradients [i], x, y, gradientWidth, gradientHeight);		
			}

			texture.Apply (false);
			return texture;
		}

        const float PI2 = Mathf.PI * 2f;
        [System.Obsolete("Gradient shaders compute gradient positions; there is no gradient shape texture any more.")]
        public static Texture2D GenerateGradientShapeTexture (int textureSize)
        {
            // Holds gradient positions, not colors, so it must not be sRGB-decoded in Linear color space.
            Texture2D texture = new Texture2D (textureSize, textureSize, TextureFormat.ARGB32, false, true);
            texture.hideFlags = HideFlags.DontSave;
            texture.name = "Gradient Shape Texture";
            texture.anisoLevel = 0;
            texture.filterMode = FilterMode.Trilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            
            int totalPixels = textureSize * textureSize;
            Color32[] texturePixels = new Color32[totalPixels];
            float angle;
            
            float x = 0, y = 0, halfSize = textureSize * 0.5f, sizeMinusOne = textureSize - 1;
            for (int i = 0; i < totalPixels; i++) {
                x = i % textureSize;
                y = Mathf.Floor ((float)i / (float)textureSize);
                
                // linear
                texturePixels [i].r = (byte)Mathf.RoundToInt (x / sizeMinusOne * 255);
                
                // radial
                texturePixels [i].g = (byte)Mathf.RoundToInt (Mathf.Clamp01 (Mathf.Sqrt (Mathf.Pow (halfSize - x, 2f) + Mathf.Pow (halfSize - y, 2f)) / (halfSize - 1f)) * 255);
                
                // conical
                angle = Mathf.Atan2(-halfSize + y, -halfSize + x);
                if(angle < 0)
                {
                    angle = PI2 + angle;
                }

                texturePixels [i].b = (byte)Mathf.RoundToInt(Mathf.Clamp01((angle / PI2)) * 255);

                // solid
                texturePixels [i].a = (byte)255;
            }
            
            texture.SetPixels32 (texturePixels);
            texture.Apply (true);
            return texture;
        }

		public static Texture2D GenerateWhiteTexture ()
		{
			Texture2D texture = new Texture2D (1, 1, TextureFormat.ARGB32, false);
			texture.hideFlags = HideFlags.DontSave;
			texture.name = "White Texture";
			texture.anisoLevel = 0;
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.SetPixel(0, 0, Color.white);
			texture.Apply (false);
			return texture;
		}

        public Material GetMaterial (SVGFill fill)
        {
            Material output = null;
            switch (fill.fillType)
            {
                case FILL_TYPE.SOLID:
                    output = GetColorMaterial(fill);
                    break;
                case FILL_TYPE.GRADIENT:
                    output = GetGradientMaterial(fill);
                    break;
                case FILL_TYPE.TEXTURE:
                    break;
            }
            return output;
        }
        
        protected Material GetGradientMaterial (SVGFill fill)
        {       
            Material output = null;
            Shader shader = null;
            switch (fill.blend) {
                case FILL_BLEND.OPAQUE:
                    shader = SVGShader.GradientColorOpaque;
                    break;
                case FILL_BLEND.ALPHA_BLENDED:
                    shader = SVGShader.GradientColorAlphaBlended;
                    break;
                    /*
                case FILL_BLEND.ADDITIVE:
                    shader = SVGShader.GradientColorAdditive;
                    break;
                case FILL_BLEND.MULTIPLY:
                    shader = SVGShader.GradientColorMultiply;
                    break;
                    */
                default:
                    shader = SVGShader.GradientColorOpaque;
                    break;
            }
            
            for (int i = 0; i < materials.Count; i++) {
                if (materials [i] == null)
                    continue;           
                if (materials [i].shader != shader)
                    continue;           
                if (fill.gradientColors.atlasIndex < 0 || fill.gradientColors.atlasIndex >= atlasTextures.Count)
                {
                    throw new System.IndexOutOfRangeException();
                }
                Texture texture = atlasTextures [fill.gradientColors.atlasIndex];
                if (texture == null)
                    continue;
                if (materials [i].GetTexture (_GradientColorKey) != texture)
                    continue;
                
                output = materials [i];
                output.SetVector (_ParamsKey, new Vector4 (atlasTextureWidth, atlasTextureHeight, gradientWidth, gradientHeight));
            }
            
            if (output == null) {
                output = new Material (shader);
                Texture2D texture = atlasTextures [fill.gradientColors.atlasIndex];
                output.SetTexture (_GradientColorKey, texture);
                output.SetVector (_ParamsKey, new Vector4 (atlasTextureWidth, atlasTextureHeight, gradientWidth, gradientHeight));
                materials.Add (output);           
            }
         
            return output;
        }

        protected Material GetColorMaterial (SVGFill fill)
        {       
            Material output = null;
            Shader shader = null;
            switch (fill.blend) {
                case FILL_BLEND.OPAQUE:
                    shader = SVGShader.SolidColorOpaque;
                    break;
                case FILL_BLEND.ALPHA_BLENDED:
                    shader = SVGShader.SolidColorAlphaBlended;
                    break;
                    /*
                case FILL_BLEND.ADDITIVE:
                    shader = SVGShader.SolidColorAdditive;
                    break;
                case FILL_BLEND.MULTIPLY:
                    shader = SVGShader.SolidColorMultiply;
                    break;
                    */
                default:
                    shader = SVGShader.SolidColorOpaque;
                    break;
            }
            
            for (int i = 0; i < materials.Count; i++) {
                if (materials [i] == null)
                    continue;           
                if (materials [i].shader != shader)
                    continue;           
                
                output = materials [i];           
            }
            
            if (output == null) {
                output = new Material (shader);
                materials.Add (output);           
            }
            return output;
        }

        public Vector4 textureParams
        {
            get {
                return new Vector4 (atlasTextureWidth, atlasTextureHeight, gradientWidth, gradientHeight);
            }
        }

        protected string GetMegaBytes(int bits)
        {
            float size = bits / 1024 / 1024 / 8;
            if (size < 1f)
            {
                return Mathf.FloorToInt(bits / 1024 / 8).ToString() + " KB";
            } else
            {
                return size.ToString(".0") + " MB";
            }
        }

        public void ClearAllData ()
        {       
            Debug.Log("Clear Atlas Data");
            if(_atlasData != null)
            {
                _atlasData.Clear();
            }
        }

        public void ClearMaterials()
        {
            if(materials == null)
                return;

            for(int i = 0; i < materials.Count; i++)
            {
                if(materials[i] == null)
                    continue;
                DestroyObjectInternal(materials[i]);
            }
            materials.Clear();
            materials = null;
        }

        public void ClearAtlasTextures ()
        {
            if (atlasTextures == null || atlasTextures.Count == 0)
                return;

            for (int i = 0; i < atlasTextures.Count; i++) {
                if (atlasTextures [i] == null)
                    continue;
                           
                DestroyObjectInternal(atlasTextures [i]);
                atlasTextures [i] = null;
            }
            
            atlasTextures.Clear ();
        }
        
        static void DestroyObjectInternal(UnityEngine.Object target)
        {
            if(Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            } else {
                UnityEngine.Object.DestroyImmediate(target, true);
            }
        }

        internal static Camera[] GetAllCameras()
        {
            return Camera.allCameras;
        }
        
        internal static void AddComponent<T>(Component component) where T : MonoBehaviour
        {
            if(component == null)
                return;
            
            GameObject gameObject = component.gameObject; 
            
            if(gameObject == null)
                return;
            
            if(gameObject.GetComponent<T>() != null)
                return;
            
            gameObject.AddComponent<T>();
        }

        public const string _GradientColorKey = "_GradientColor";
        public const string _GradientShapeKey = "_GradientShape";
        public const string _ParamsKey = "_Params";
        public static void AssignMaterialGradients(Material material, Texture2D gradientAtlas, Texture2D gradientShape, int gradientWidth, int gradientHeight)
        {
            if(material == null)
                return;
            
            if(material.HasProperty(_GradientColorKey))
            {
                material.SetTexture(_GradientColorKey, gradientAtlas);
            }
            if(material.HasProperty(_GradientShapeKey))
            {
                material.SetTexture(_GradientShapeKey, gradientShape);
            }
            if(material.HasProperty(_ParamsKey) && gradientAtlas != null)
            {
                Vector4 materialParams = new Vector4(gradientAtlas.width, gradientAtlas.height, gradientWidth, gradientHeight);
                material.SetVector(_ParamsKey, materialParams);
            }
        }
        
        public static void AssignMaterialGradients(Material[] materials, Texture2D gradientAtlas, Texture2D gradientShape, int gradientWidth, int gradientHeight)
        {
            if(materials == null || materials.Length == 0)
                return;
            
            for(int i = 0; i < materials.Length; i++)
            {
                AssignMaterialGradients(materials[i], gradientAtlas, gradientShape, gradientWidth, gradientHeight);
            }
        }

        public Material GetTransparentMaterial(bool antialiasing, bool hasGradients)
        {
            if(antialiasing)
            {
                if(hasGradients)
                {
                    return transparentGradientAntialiased;
                } else {
                    return transparentSolidAntialiased;
                }
            } else {
                if(hasGradients)
                {
                    return transparentGradient;
                } else {
                    return transparentSolid;
                }
            }
        }

        public Material GetOpaqueMaterial(bool hasGradients)
        {
            if(hasGradients)
            {
                return opaqueGradient;
            } else {
                return opaqueSolid;
            }
        }
    }
}
