// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;
using UnityEngine;

namespace SVGImporter.Document 
{
    using Rendering;
    using Utils;

    public class SVGElement : SVGParentable, ISVGDrawable
    {
        protected string _name;
        public string name
        {
            get {
                return _name;
            }
        }

        private AttributeList _attrList;
        public AttributeList attributeList
        {
            get
            {
                return _attrList;
            }
        }

        private List<object> _elementList;
        public List<object> elementList
        {
            get
            {
                return _elementList;
            }
        }

        private SVGParser _xmlImp;
        private SVGPaintable _paintable;
        public SVGPaintable paintable
        {
            get {
                return _paintable;
            }
        }

        protected bool _rootElement;
        public bool rootElement
        {
            get {
                return _rootElement;
            }
        }

        public SVGElement(SVGParser xmlImp,
                    SVGTransformList inheritTransformList,
                    SVGPaintable inheritPaintable,
                    bool root = false) : base(inheritTransformList)
        {
            _rootElement = root;
            _name = _attrList.GetValue("id");
            _xmlImp = xmlImp;
            _attrList = _xmlImp.node.attributes;

            if(inheritPaintable != null)
            {
                _paintable = new SVGPaintable(inheritPaintable, _xmlImp.node);
            } else {
                _paintable = new SVGPaintable(_xmlImp.node);
            }

            Init();
        }

        protected void Init()
        {
            _elementList = new List<object>();

            ViewBoxTransform();

            SVGTransform temp = new SVGTransform(_cachedViewBoxTransform);
            SVGTransformList t_currentTransformList = new SVGTransformList(_attrList.GetValue("transform"));
            t_currentTransformList.AppendItem(temp);

            this.currentTransformList = t_currentTransformList;

            if(_rootElement)
            {
                // TODO Clip Paths does not works properly
                /*
                if(!SVGAssetImport.ignoreSVGCanvas)
                {
                    Rect viewport = paintable.viewport;
                    paintable.clipPathList.Add(new List<Vector2>{
                        new Vector2(viewport.x, viewport.y),
                        new Vector2(viewport.x + viewport.width, viewport.y),
                        new Vector2(viewport.x + viewport.width, viewport.y + viewport.height),
                        new Vector2(viewport.x, viewport.y + viewport.height)
                    });
                }
                */
            }

            GetElementList();
        }

        private void GetElementList()
        {
            _xmlImp.GetElementList(_elementList, _paintable, summaryTransformList);
        }

        public void BeforeRender(SVGTransformList transformList)
        {
            this.inheritTransformList = transformList;

            for (int i = 0; i < _elementList.Count; i++)
            {
                ISVGDrawable temp = _elementList [i] as ISVGDrawable;
                if (temp != null)
                {
                    temp.BeforeRender(this.summaryTransformList);
                }
            }
        }

        public void Render()
        {
            for (int i = 0; i < _elementList.Count; i++)
            {
                ISVGDrawable temp = _elementList [i] as ISVGDrawable;
                if (temp != null)
                {
                    temp.Render();
                }
            }
        }

        private SVGMatrix _cachedViewBoxTransform = SVGMatrix.identity;
        private bool cachedViewBox;

        public SVGMatrix ViewBoxTransform()
        {
            if (!cachedViewBox)
            {               
                cachedViewBox  = true;
                Rect viewport = _paintable.viewport;
                if(_rootElement)
                {
                    _cachedViewBoxTransform = SVGTransformable.GetRootViewBoxTransform(_attrList, ref viewport);
                } else {
                    _cachedViewBoxTransform = SVGTransformable.GetViewBoxTransform(_attrList, ref viewport, true);
                }

                //Debug.Log(viewport);
                //Debug.Log(_xmlImp.node.name);
                paintable.SetViewport(viewport);
            }
            return this._cachedViewBoxTransform;
        }
    }
}
