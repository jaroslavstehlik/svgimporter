// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

//#define PRIVATE_BETA

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter
{
    using Rendering;
	using Utils;

    public class SVGAtlasData
    {
        public CCGradient[] gradients;
        public Dictionary<string, CCGradient> gradientCache;

        public void Init(int length)
            {
            gradients = new CCGradient[length];
            gradientCache = new Dictionary<string, CCGradient> ();
        }

        public void ClearGradientCache()
        {
            if (gradientCache != null) gradientCache.Clear ();            
            gradientCache = null;
        }

        public void InitGradientCache()
        {
            string gradientHash;
            if (gradientCache == null) {
                gradientCache = new Dictionary<string, CCGradient> ();
                int gradientsLength = gradients.Length;
                for (int i = 0; i < gradientsLength; i++) {
                    if(gradients [i] == null) continue;
                    gradientHash = gradients [i].hash;
                    if (!gradientCache.ContainsKey (gradientHash)) {
                        gradientCache.Add (gradientHash, gradients [i]);
                    }
                }
            }
        }

        public void RebuildGradientCache()
        {
            ClearGradientCache ();
            InitGradientCache ();
        }

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

        public CCGradient AddGradient(CCGradient gradient)
        {
            bool gradientExist;
            return AddGradient(gradient, out gradientExist);
        }

        public CCGradient AddGradient(CCGradient gradient, out bool gradientExist)
        {
            gradientExist = false;
            if (gradient == null || !gradient.initialised)
                return null;
            
            if (gradientCache == null || gradientCache.Count == 0)
                RebuildGradientCache ();

            string gradientHash = gradient.hash;
            if (gradientCache.ContainsKey (gradientHash)) {          
                gradient = gradientCache [gradientHash];
                gradientExist = true;
            } else {
                int gradientsLength = gradients.Length;
                for(int i = 0; i < gradientsLength; i++)
                {
                    if(gradients[i] != null) continue;
                    gradient.index = i;
                    gradients[i] = gradient;
                    gradientCache.Add (gradientHash, gradient);
                    break;
                }
                gradientExist = false;
            }
            return  gradient;
        }

        public bool RemoveGradient(CCGradient gradient)
        {
            if (gradient == null || !gradient.initialised)
                return false;
            
            if (gradientCache == null || gradientCache.Count == 0)
                return false;
            
            string gradientHash = gradient.hash;
            if (gradientCache.ContainsKey (gradientHash)) {
                gradientCache.Remove(gradientHash);
                gradients[gradient.index] = null;
                return true;
            }
            return false;
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
        
        public CCGradient GetGradient (CCGradient gradient)
        {
            if (gradient == null || !gradient.initialised || gradientCache == null)
                return null;

            string gradientHash = gradient.hash;
            if (gradientCache.ContainsKey (gradientHash)) {
                return gradientCache [gradientHash];
            } else {
                return null;
            }
        }

        public bool HasGradient (CCGradient gradient)
        {
            if (gradient == null || !gradient.initialised || gradientCache == null)
                return false;
            
            string gradientHash = gradient.hash;
            if (gradientCache.ContainsKey (gradientHash)) {            
                gradient = gradientCache [gradientHash];
                return true;
            } else {
                return false;
            }
        }

        public void Clear()
        {
            if(gradients != null)
            {
                gradients = null;
            }

            if(gradientCache != null)
            {
                gradientCache.Clear();
                gradientCache = null;
            }
        }
    }

    [ExecuteInEditMode]
    public class SVGAtlas : MonoBehaviour {

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

        protected static Texture2D _gradientShapeTexture;
        public static Texture2D gradientShapeTexture
        {
            get {
                if(_gradientShapeTexture == null) _gradientShapeTexture = GenerateGradientShapeTexture(_gradientShapeTextureSize);                
                return _gradientShapeTexture;
            }
        }

        protected  static int _gradientShapeTextureSize = 512;
        public static int gradientShapeTextureSize {
            get {
                return _gradientShapeTextureSize;
            }
            set {
                if(_gradientShapeTextureSize == value) return;
                if(_gradientShapeTexture != null) DestroyImmediate(_gradientShapeTexture);
                _gradientShapeTexture = GenerateGradientShapeTexture(_gradientShapeTextureSize);
            }
        }

        public static void ClearGradientShapeTexture ()
        {
            if (gradientShapeTexture == null)
                return;
            
            DestroyImmediate(_gradientShapeTexture);
            _gradientShapeTexture = null;
        }

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
                    UpdateMaterialProperties(_ui);
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
                    UpdateMaterialProperties(_uiAntialiased);
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
                    UpdateMaterialProperties(_opaqueGradient);
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
                    UpdateMaterialProperties(_transparentGradient);
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
                    UpdateMaterialProperties(_transparentGradientAntialiased);
                }
                return _transparentGradientAntialiased;
            }
        }

        public void UpdateMaterialProperties(Material material)
        {
            if(material == null) return;
            if(atlasTextures != null && atlasTextures.Count > 0)
            {
                if(material.HasProperty(_GradientColorKey))
                    material.SetTexture (_GradientColorKey, atlasTextures[0]);
            }
            if(material.HasProperty(_GradientShapeKey))
                material.SetTexture (_GradientShapeKey, gradientShapeTexture);
            if(material.HasProperty(_ParamsKey))
                material.SetVector (_ParamsKey, new Vector4 (atlasTextureWidth, atlasTextureHeight, gradientWidth, gradientHeight));
        }

    	public List<Texture2D> atlasTextures;		
    	public List<Material> materials;
    	
        public const int defaultGradientWidth = 128;
        public const int defaultGradientHeight = 4;
        public const int defaultAtlasTextureWidth = 512;
        public const int defaultAtlasTextureHeight = 512;
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
            AddFakeCamera();

            Camera.onPreRender += OnAtlasPreRender;
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

        protected void OnDestroy()
        {
            _beingDestroyed = true;
            Camera.onPreRender -= OnAtlasPreRender;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
        }

        protected void AddFakeCamera()
        {
                Camera camera = gameObject.AddComponent<Camera>();
                camera.hideFlags = HideFlags.DontSave;
                camera.clearFlags = CameraClearFlags.Nothing;
                camera.orthographic = true;
                camera.depth = float.MinValue;
                camera.cullingMask = 0;
                camera.useOcclusionCulling = false;
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
        }

        protected static SVGAtlas _Instance;
        public static SVGAtlas Instance
        {
            get {
                if(_Instance == null)
                {
                    SVGAtlas[] instances = Resources.FindObjectsOfTypeAll<SVGAtlas>();
                    if(instances != null && instances.Length > 0)
                    {
                        _Instance = instances[0];
//                        Debug.Log("SVGAtlas, found instances: "+instances.Length);
                    }
                }
                if(_Instance == null)
                {
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

        public void ClearAll()
        {
            Debug.Log("Cleared SVG Atlas: "+Time.frameCount+", playmode: "+Application.isPlaying);
            if(_ui != null)
            {
                DestroyObjectInternal(_ui);
                _ui = null;
            }            
            if(_uiAntialiased != null)
            {
                DestroyObjectInternal(_uiAntialiased);
                _uiAntialiased = null;
            }
            if(_opaqueSolid != null)
            {
                DestroyObjectInternal(_opaqueSolid);
                _opaqueSolid = null;
            }
            if(_transparentSolid != null)
            {
                DestroyObjectInternal(_transparentSolid);
                _transparentSolid = null;
            }
            if(_transparentSolidAntialiased != null)
            {
                DestroyObjectInternal(_transparentSolidAntialiased);
                _transparentSolidAntialiased = null;
            }
            if(_opaqueGradient != null)
            {
                DestroyObjectInternal(_opaqueGradient);
                _opaqueGradient = null;
            }
            if(_transparentGradient != null)
            {
                DestroyObjectInternal(_transparentGradient);
                _transparentGradient = null;
            }
            if(_transparentGradientAntialiased != null)
            {
                DestroyObjectInternal(_transparentGradientAntialiased);
                _transparentGradientAntialiased = null;
            }

            ClearAllData();
            ClearMaterials();
            ClearAtlasTextures();
        }

    	protected void Init ()
    	{
//            Debug.Log("Inited SVG Atlas: "+Time.frameCount+", playmode: "+Application.isPlaying+" InstanceID: "+GetInstanceID());
    		if (materials == null) materials = new List<Material> ();
//            Debug.Log(_atlasData);
            if(_atlasData == null)
            {
//                Debug.Log("Creating Atlas Data! InstanceID: "+GetInstanceID());
                _atlasData = new SVGAtlasData();
                _atlasData.Init(atlasTextureWidth * atlasTextureHeight);
                AddGradient(SVGAtlasData.GetDefaultGradient());
            }
    	}
    	
        const int pixelOffset = 1;
        public static void RenderGradient (Texture2D texture, CCGradient gradient, int x, int y, int gradientWidth, int gradientHeight)
        {
            //Debug.Log(string.Format("x: {0}, y: {1}, gradient: {2}", x, y, gradient));
            if (texture == null || gradient == null || !gradient.initialised)
                return;
            
            float tempWidth = gradientWidth - 1 - pixelOffset * 2;
            Color[] pixels = new Color[gradientWidth * gradientHeight];
            
            Color pixel;
            
            for (int i = 0; i < gradientWidth; i++) {
                pixel = gradient.Evaluate ((float)(i - pixelOffset) / tempWidth);
                for(int j = 0; j < gradientHeight; j++) {
                    pixels [gradientWidth * j + i] = pixel;
                }
            }
            
            texture.SetPixels(x, y, gradientWidth, gradientHeight, pixels);
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

            AssignMaterialGradients(_opaqueGradient, texture, gradientShapeTexture, gradientWidth, gradientHeight);
            AssignMaterialGradients(_transparentGradient, texture, gradientShapeTexture, gradientWidth, gradientHeight);
            AssignMaterialGradients(_transparentGradientAntialiased, texture, gradientShapeTexture, gradientWidth, gradientHeight);
            AssignMaterialGradients(_ui, texture, gradientShapeTexture, gradientWidth, gradientHeight);
            AssignMaterialGradients(_uiAntialiased, texture, gradientShapeTexture, gradientWidth, gradientHeight);


            if (index >= atlasTextures.Count - 1) {               
                atlasTextures.Add (texture);
            } else if (index >= 0) {                         
                atlasTextures [index] = texture;
            }

			return texture;
        }

		public static Texture2D CreateTexture(int width, int height)
		{
			Texture2D texture = new Texture2D (width, height, TextureFormat.ARGB32, false);
			texture.filterMode = FilterMode.Bilinear;
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
                _atlasData.Init(atlasTextureWidth * atlasTextureHeight);
            }
            bool gradientExist;
            gradient = _atlasData.AddGradient(gradient, out gradientExist);
            if(gradientExist) return gradient;

            int x = 0, y = 0;
            GetCoords (out x, out y, gradient.index);

//            Debug.Log("AddGradient: x: "+x+", y: "+y);
            gradient.atlasIndex = atlasIndex;
            _atlasHasChanged = true;

            return  gradient;
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
                Debug.LogWarning("atlasData is null! "+GetInstanceID());
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
        public static Texture2D GenerateGradientShapeTexture (int textureSize)
        {
            Texture2D texture = new Texture2D (textureSize, textureSize, TextureFormat.ARGB32, false);
            texture.hideFlags = HideFlags.DontSave;
            texture.name = "Gradient Shape Texture";
            texture.anisoLevel = 0;
            texture.filterMode = FilterMode.Trilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            
            int totalPixels = gradientShapeTextureSize * gradientShapeTextureSize;
            Color32[] texturePixels = new Color32[totalPixels];
            float angle;
            
            float x = 0, y = 0, halfSize = gradientShapeTextureSize * 0.5f, sizeMinusOne = gradientShapeTextureSize - 1;
            for (int i = 0; i < totalPixels; i++) {
                x = i % gradientShapeTextureSize;
                y = Mathf.Floor ((float)i / (float)gradientShapeTextureSize);
                
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
                output.SetTexture (_GradientShapeKey, gradientShapeTexture);
                output.SetVector (_ParamsKey, new Vector4 (atlasTextureWidth, atlasTextureHeight, gradientWidth, gradientHeight));
            }
            
            if (output == null) {
                output = new Material (shader);
                Texture2D texture = atlasTextures [fill.gradientColors.atlasIndex];
                output.SetTexture (_GradientColorKey, texture);
                output.SetTexture (_GradientShapeKey, gradientShapeTexture);
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
