// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Rendering 
{
    using Geometry;
    using Utils;
    using Document;

    public class SVGLineElement : SVGParentable, ISVGDrawable, ISVGElement
    {
        private SVGLength _x1;
        private SVGLength _y1;
        private SVGLength _x2;
        private SVGLength _y2;

        private AttributeList _attrList;
        public AttributeList attrList
        {
            get {
                return _attrList;
            }
        }
        private SVGPaintable _paintable;
        public SVGPaintable paintable
        {
            get {
                return _paintable;
            }
        }

        public SVGLength x1
        {
            get
            {
                return this._x1;
            }
        }

        public SVGLength y1
        {
            get
            {
                return this._y1;
            }
        }

        public SVGLength x2
        {
            get
            {
                return this._x2;
            }
        }

        public SVGLength y2
        {
            get
            {
                return this._y2;
            }
        }

        public SVGLineElement(Node node, SVGTransformList inheritTransformList, SVGPaintable inheritPaintable = null) : base(inheritTransformList)
        {
            this._attrList = node.attributes;
            this._paintable = new SVGPaintable(inheritPaintable, node);
            this._x1 = new SVGLength(attrList.GetValue("x1"));
            this._y1 = new SVGLength(attrList.GetValue("y1"));
            this._x2 = new SVGLength(attrList.GetValue("x2"));
            this._y2 = new SVGLength(attrList.GetValue("y2"));
            this.currentTransformList = new SVGTransformList(attrList.GetValue("transform"));
            
            Rect viewport = _paintable.viewport;
            this.currentTransformList.AppendItem(new SVGTransform(SVGTransformable.GetViewBoxTransform(_attrList, ref viewport, false)));
            paintable.SetViewport(viewport);
        }

        public void BeforeRender(SVGTransformList transformList)
        {
            this.inheritTransformList = transformList;
        }

        public List<List<Vector2>> GetPath()
        {
            List<Vector2> output = new List<Vector2>(){
                transformMatrix.Transform(new Vector2(x1.value, y1.value)),
                transformMatrix.Transform(new Vector2(x2.value, y2.value))
            };
            return new List<List<Vector2>>(){output};
        }
        
        public List<List<Vector2>> GetClipPath()
        {
            List<List<Vector2>> path = GetPath();
            if(path == null || path.Count == 0 || path[0] == null || path[0].Count == 0) return null;
            
            List<List<Vector2>> clipPath = new List<List<Vector2>>();

            List<StrokeSegment[]> segments = new List<StrokeSegment[]>(){SVGSimplePath.GetSegments(path[0])};
            List<List<Vector2>> strokePath = SVGLineUtils.StrokeShape(segments, paintable.strokeWidth, Color.black, SVGSimplePath.GetStrokeLineJoin(paintable.strokeLineJoin), SVGSimplePath.GetStrokeLineCap(paintable.strokeLineCap), paintable.miterLimit, paintable.dashArray, paintable.dashOffset, ClosePathRule.NEVER, SVGGraphics.roundQuality);
            if(strokePath != null && strokePath.Count > 0) clipPath.AddRange(strokePath);
            
            return clipPath;
        }
        
        public void Render()
        {
            SVGGraphics.Create(this, "Line Element", ClosePathRule.NEVER);
        }
    }
}
