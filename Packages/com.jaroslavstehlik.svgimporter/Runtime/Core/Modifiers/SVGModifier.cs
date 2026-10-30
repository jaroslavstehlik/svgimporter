

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{
    using Rendering;
    using Utils;

    [System.Serializable]
    public sealed class LayerSelection
    {
        [HideInInspector]
        [SerializeField]
        private List<int> _layers;
        public List<int> layers
        {
            get {
                if(_layers == null) _layers = new List<int>();
                return _layers;
            }
        }
        
        private HashSet<int> _cache;
        internal HashSet<int> cache
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

        internal void UpdateCache()
        {
            if(_cache == null) _cache = new HashSet<int>();
            for(int i = 0; i < layers.Count; i++)
            {
                _cache.Add(layers[i]);
            }
        }
        
        internal void ClearCache()
        {
            if(_cache != null) _cache.Clear();
        }

        public void Clear()
        {
            ClearCache();
            if(_layers != null) _layers.Clear();
        }
    }

    /// <summary>
    /// Base class of components that change an SVG Renderer's layers before it builds its mesh,
    /// every frame or when asked (manualUpdate). Works with assets imported with Use Layers.
    /// </summary>
    [RequireComponent(typeof(SVGRenderer))]
    public abstract class SVGModifier : MonoBehaviour
    {
#if UNITY_EDITOR
        internal static SVGModifier _internal_selectingModifier;
#endif

        /// <summary>Off: the renderer rebuilds with the modifier only when asked (SVGRenderer.SetAllDirty).</summary>
        [HideInInspector]
        public bool manualUpdate = false;
        /// <summary>On: only the layers in layerSelection are modified.</summary>
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

        SVGRenderer _svgRenderer;
        /// <summary>The renderer the modifier changes.</summary>
        public SVGRenderer svgRenderer
        {
            get {
                if(_svgRenderer == null) _svgRenderer = GetComponent<SVGRenderer>();
                return _svgRenderer;
            }
        }

        protected virtual void Init()
        {
            if(GetComponent<SVGImage>() != null)
                Debug.LogWarning(GetType().Name + " on " + name + " has no effect: modifiers work with SVG Renderer, not SVG Image.", this);
            if(svgRenderer != null)
            {
                svgRenderer.AddModifier(this);
                svgRenderer.willBuildMesh += OnWillBuildMesh;
            }
        }

        protected virtual void Clear()
        {
            if(svgRenderer != null)
            {
                svgRenderer.willBuildMesh -= OnWillBuildMesh;
                svgRenderer.RemoveModifier(this);
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
            SVGRenderer renderer = svgRenderer;
            if(renderer == null || manualUpdate || !RebuildsForAll(renderer)) return;
            if(Application.isPlaying && renderer.lastFrameChanged == Time.frameCount) return;
            renderer.RebuildModifiedMesh();
        }

        bool RebuildsForAll(SVGRenderer renderer)
        {
            List<SVGModifier> modifiers = renderer.modifiers;
            for(int i = 0; i < modifiers.Count; i++)
            {
                if(modifiers[i] != null && !modifiers[i].manualUpdate) return modifiers[i] == this;
            }
            return false;
        }

        /// <summary>Whether the modifier applies now: it's enabled, and its layers aren't being picked in the Editor.</summary>
        protected bool active
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

        void OnWillBuildMesh(SVGLayer[] layers, SVGAsset asset)
        {
            ModifyLayers(layers, asset);
        }

        /// <summary>Changes the layers, a copy of the asset's, which the renderer then builds its mesh from.</summary>
        protected abstract void ModifyLayers(SVGLayer[] layers, SVGAsset asset);
    }
}
