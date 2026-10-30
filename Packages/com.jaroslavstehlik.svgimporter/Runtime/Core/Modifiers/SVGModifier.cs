

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{
    using Rendering;
    using Utils;

    [System.Serializable]
    public class LayerSelection
    {
        [HideInInspector]
        [SerializeField]
        protected List<int> _layers;
        public List<int> layers
        {
            get {
                if(_layers == null) _layers = new List<int>();
                return _layers;
            }
        }
        
        protected HashSet<int> _cache;
        public HashSet<int> cache
        {
            get {
                UpdateCache();
                return _cache;
            }
        }

        public void Add(int index)
        {
            if(Contains(index)) return;
            layers.Add(index);
            cache.Add(index);
        }

        public void Remove(int index)
        {
            if(!Contains(index)) return;
            layers.Remove(index);
            cache.Remove(index);
        }
        
        public bool Contains(int index)
        {
            return cache.Contains(index);
        }

        public void UpdateCache()
        {
            if(_cache == null) _cache = new HashSet<int>();
            for(int i = 0; i < layers.Count; i++)
            {
                _cache.Add(layers[i]);
            }
        }
        
        public void ClearCache()
        {
            if(_cache != null) _cache.Clear();
        }

        public void Clear()
        {
            ClearCache();
            if(_layers != null) _layers.Clear();
        }
    }

    public abstract class SVGModifier : MonoBehaviour, ISVGModify
    {
#if UNITY_EDITOR
        public static SVGModifier _internal_selectingModifier;
#endif

        [HideInInspector]
        public bool manualUpdate = false;
        [HideInInspector]
        public bool useSelection = false;
        [HideInInspector]
        public LayerSelection layerSelection;

        public bool hasSelection
        {
            get {
                if(!useSelection) return false;
                if(layerSelection == null || layerSelection.layers.Count == 0) return false;
                return true;
            }
        }

        protected ISVGRenderer _svgRenderer;
        public ISVGRenderer svgRenderer
        {
            get {
                if(_svgRenderer == null) _svgRenderer = GetComponent<ISVGRenderer>();
                return _svgRenderer;
            }
        }

        protected virtual void Init()
        {
            if(svgRenderer is SVGImage)
                Debug.LogWarning(GetType().Name + " on " + name + " has no effect: modifiers work with SVG Renderer, not SVG Image.", this);
            if(svgRenderer != null) 
            {
                svgRenderer.AddModifier(this);
                svgRenderer.OnPrepareForRendering += PrepareForRendering;
            }
        }
        
        protected virtual void Clear()
        {
            if(svgRenderer != null) 
            {
                svgRenderer.OnPrepareForRendering -= PrepareForRendering;
                svgRenderer.RemoveModifier(this);
                _svgRenderer = null;
            }
        }
        
        protected virtual void OnEnable()
        {
            Init();
        }
        
        protected virtual void OnDisable()
        {
            Clear();
            if(svgRenderer != null) svgRenderer.UpdateRenderer();
        }

        // Invoked by Unity before a camera renders the object: the mesh is rebuilt with the
        // modifiers once a frame, by the first modifier that updates automatically (the rebuild
        // applies all of them).
        protected virtual void OnWillRenderObject()
        {
            var renderer = svgRenderer as SVGRenderer;
            if(renderer == null || manualUpdate || !RebuildsForAll(renderer)) return;
            if(Application.isPlaying && renderer.lastFrameChanged == Time.frameCount) return;
            renderer.RebuildModifiedMesh();
        }

        bool RebuildsForAll(SVGRenderer renderer)
        {
            List<ISVGModify> modifiers = renderer.modifiers;
            for(int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i] as SVGModifier;
                if(modifier != null && !modifier.manualUpdate) return modifier == this;
            }
            return false;
        }

        public virtual bool active
        {
            get
            {
#if UNITY_EDITOR
                if (_internal_selectingModifier != null)
                {
                    return false;
                }
#endif
                return enabled;
            }
        }
        
        protected abstract void PrepareForRendering (SVGLayer[] layers, SVGAsset svgAsset, bool force);
    }
}
