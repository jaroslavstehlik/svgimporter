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

    public class SVGPolygonElement : SVGParentable, ISVGDrawable, ISVGElement
    {
        private List<Vector2> _listPoints;

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

        public List<Vector2> listPoints
        {
            get { return this._listPoints; }
        }

        public SVGPolygonElement(Node node, SVGTransformList inheritTransformList, SVGPaintable inheritPaintable = null) : base(inheritTransformList)
        {
            this._attrList = node.attributes;
            this._paintable = new SVGPaintable(inheritPaintable, node);
            this._listPoints = ExtractPoints(this._attrList.GetValue("points"));
            this.currentTransformList = new SVGTransformList(attrList.GetValue("transform"));
            
            Rect viewport = _paintable.viewport;
            this.currentTransformList.AppendItem(new SVGTransform(SVGTransformable.GetViewBoxTransform(_attrList, ref viewport, false)));
            paintable.SetViewport(viewport);
        }

        private List<Vector2> ExtractPoints(string inputText)
        {
            List<Vector2> _return = new List<Vector2>();
            string[] _lstStr = SVGStringExtractor.ExtractTransformValue(inputText);

            int len = _lstStr.Length;

            for (int i = 0; i < len -1; i++)
            {
                string value1, value2;
                value1 = _lstStr [i];
                value2 = _lstStr [i + 1];
                SVGLength _length1 = new SVGLength(value1);
                SVGLength _length2 = new SVGLength(value2);
                Vector2 _point = new Vector2(_length1.value, _length2.value);
                _return.Add(_point);
                i++;
            }
            return _return;
        }

        public void BeforeRender(SVGTransformList transformList)
        {
            this.inheritTransformList = transformList;
        }

        public List<List<Vector2>> GetPath()
        {
            List<Vector2> output = new List<Vector2>(listPoints.Count + 1);
            for (int i = 0; i < listPoints.Count; i++)
            {
                output.Add(transformMatrix.Transform(listPoints[i]));
            }
            output.Add(output[0]);

            // Douglas Peucker Reduction
            return new List<List<Vector2>>(){SVGBezier.Optimise(output, SVGGraphics.vpm)};
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
            SVGGraphics.Create(this, "Polygon Element");
        }
    }
}