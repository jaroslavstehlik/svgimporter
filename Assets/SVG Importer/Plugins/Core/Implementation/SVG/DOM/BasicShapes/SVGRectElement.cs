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

    public class SVGRectElement : SVGParentable, ISVGDrawable, ISVGElement
    {
        private SVGLength _x;
        private SVGLength _y;
        private SVGLength _width;
        private SVGLength _height;
        private SVGLength _rx;
        private SVGLength _ry;

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

        public SVGLength x
        {
            get
            {
                return this._x;
            }
        }

        public SVGLength y
        {
            get
            {
                return this._y;
            }
        }

        public SVGLength width
        {
            get
            {
                return this._width;
            }
        }

        public SVGLength height
        {
            get
            {
                return this._height;
            }
        }

        public SVGLength rx
        {
            get
            {
                return this._rx;
            }
        }

        public SVGLength ry
        {
            get
            {
                return this._ry;
            }
        }

        public SVGRectElement(Node node, SVGTransformList inheritTransformList, SVGPaintable inheritPaintable = null) : base(inheritTransformList)
        {
            this._attrList = node.attributes;
            this._paintable = new SVGPaintable(inheritPaintable, node);
            this._x = new SVGLength(attrList.GetValue("x"));
            this._y = new SVGLength(attrList.GetValue("y"));
            this._width = new SVGLength(attrList.GetValue("width"));
            this._height = new SVGLength(attrList.GetValue("height"));
            this._rx = new SVGLength(attrList.GetValue("rx"));
            this._ry = new SVGLength(attrList.GetValue("ry"));
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
            List<Vector2> output = new List<Vector2>();

            float widthValue = width.value, heightValue = height.value;
            float xValue = x.value, yValue = y.value, rxValue = rx.value, ryValue = ry.value;
            
            Vector2 p1 = new Vector2(xValue, yValue),
            p2 = new Vector2(xValue + widthValue, yValue),
            p3 = new Vector2(xValue + widthValue, yValue + heightValue),
            p4 = new Vector2(xValue, yValue + heightValue);
            
            if(rxValue == 0.0f && ryValue == 0.0f) {
                output = new List<Vector2>(new Vector2[]{
                    transformMatrix.Transform(p1),
                    transformMatrix.Transform(p2),
                    transformMatrix.Transform(p3),
                    transformMatrix.Transform(p4)
                });
            } else {
                float t_rx = (rxValue == 0.0f) ? ryValue : rxValue;
                float t_ry = (ryValue == 0.0f) ? rxValue : ryValue;
                
                t_rx = (t_rx > (widthValue * 0.5f - 2f)) ? (widthValue * 0.5f - 2f) : t_rx;
                t_ry = (t_ry > (heightValue * 0.5f - 2f)) ? (heightValue * 0.5f - 2f) : t_ry;
                
                float angle = transformAngle;
                
                Vector2 t_p1 = transformMatrix.Transform(new Vector2(p1.x + t_rx, p1.y));
                Vector2 t_p2 = transformMatrix.Transform(new Vector2(p2.x - t_rx, p2.y));
                Vector2 t_p3 = transformMatrix.Transform(new Vector2(p2.x, p2.y + t_ry));
                Vector2 t_p4 = transformMatrix.Transform(new Vector2(p3.x, p3.y - t_ry));
                
                Vector2 t_p5 = transformMatrix.Transform(new Vector2(p3.x - t_rx, p3.y));
                Vector2 t_p6 = transformMatrix.Transform(new Vector2(p4.x + t_rx, p4.y));
                Vector2 t_p7 = transformMatrix.Transform(new Vector2(p4.x, p4.y - t_ry));
                Vector2 t_p8 = transformMatrix.Transform(new Vector2(p1.x, p1.y + t_ry));
                
                output = SVGGeomUtils.RoundedRect(t_p1, t_p2, t_p3, t_p4, t_p5, t_p6, t_p7, t_p8, t_rx, t_ry, angle);
            }
            
            output.Add(output[0]);

            return new List<List<Vector2>>(){output};
        }
        
        public List<List<Vector2>> GetClipPath()
        {
            List<List<Vector2>> path = GetPath();
            if(path == null || path.Count == 0 || path[0] == null || path[0].Count == 0) return null;
            
            List<List<Vector2>> clipPath = new List<List<Vector2>>();
            if(paintable.IsFill())
            {
                clipPath.Add(path[0]);
            }
            
            if(paintable.IsStroke())
            {
                List<StrokeSegment[]> segments = new List<StrokeSegment[]>(){SVGSimplePath.GetSegments(path[0])};
                List<List<Vector2>> strokePath = SVGLineUtils.StrokeShape(segments, paintable.strokeWidth, Color.black, SVGSimplePath.GetStrokeLineJoin(paintable.strokeLineJoin), SVGSimplePath.GetStrokeLineCap(paintable.strokeLineCap), paintable.miterLimit, paintable.dashArray, paintable.dashOffset, ClosePathRule.ALWAYS, SVGGraphics.roundQuality);
                if(strokePath != null && strokePath.Count > 0) clipPath.AddRange(strokePath);
            }
            
            return clipPath;
        }
        
        public void Render()
        {
            SVGGraphics.Create(this, "Rectangle Element");
        }
    }
}
