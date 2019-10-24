// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
#if UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
				if(_svgRenderer == null) _svgRenderer = GetComponent(typeof(ISVGRenderer)) as ISVGRenderer;
#else
                if(_svgRenderer == null) _svgRenderer = GetComponent<ISVGRenderer>();
#endif
                return _svgRenderer;
            }
        }

        protected virtual void Init()
        {
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

        // This method is invoked by Unity when rendering to Camera
        protected virtual void OnWillRenderObject()
        {
            if(svgRenderer == null || manualUpdate) return;
            if(Application.isPlaying) if(svgRenderer.lastFrameChanged == Time.frameCount) return;
            svgRenderer.UpdateRenderer();
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
