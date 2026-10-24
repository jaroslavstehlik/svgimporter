

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Rendering 
{
    using Geometry;
    using Utils;
    using Document;

    public class SVGCircleElement : SVGParentable, ISVGDrawable, ISVGElement
    {
        private SVGLength _cx;
        private SVGLength _cy;
        private SVGLength _r;

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

        public SVGLength cx
        {
            get
            {
                return this._cx;
            }
        }

        public SVGLength cy
        {
            get
            {
                return this._cy;
            }
        }

        public SVGLength r
        {
            get
            {
                return this._r;
            }
        }

        public SVGCircleElement(Node node, SVGTransformList inheritTransformList, SVGPaintable inheritPaintable = null) : base(inheritTransformList)
        {
            this._attrList = node.attributes;
            this._paintable = new SVGPaintable(inheritPaintable, node);
            this._cx = new SVGLength(attrList.GetValue("cx"));
            this._cy = new SVGLength(attrList.GetValue("cy"));
            this._r = new SVGLength(attrList.GetValue("r"));
            this.currentTransformList = new SVGTransformList(attrList.GetValue("transform"));

            Rect viewport = _paintable.viewport;
            this.currentTransformList.AppendItem(new SVGTransform(SVGTransformable.GetViewBoxTransform(this._attrList, ref viewport, false)));
            paintable.SetViewport(viewport);
        }

        public void BeforeRender(SVGTransformList transformList)
        {
            this.inheritTransformList = transformList;
        }

        public List<List<Vector2>> GetPath()
        {
            List<Vector2> output = Circle(cx.value, cy.value, r.value, transformMatrix);
            if(output.Count == 0) return new List<List<Vector2>>();
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
            SVGGraphics.Create(this, "Circle Element");
        }

        const float circleConstant = 0.551915024494f;
        public static List<Vector2> Circle(float x0, float y0, float radius, SVGMatrix matrix) {
            List<Vector2> output = new List<Vector2>();
            
            x0 -= radius;
            y0 -= radius;
            
            float handleDistance = circleConstant * radius;
            Vector2 handleRight = new Vector2(handleDistance, 0f);
            Vector2 handleLeft = new Vector2(-handleDistance, 0f);
            Vector2 handleUp = new Vector2(0f, -handleDistance);
            Vector2 handleDown = new Vector2(0f, handleDistance);
            
            Vector2 topCenter = new Vector2(x0 + radius, y0);
            Vector2 left = new Vector2(x0, y0 + radius);
            Vector2 right = new Vector2(x0 + radius * 2f, y0 + radius );
            Vector2 bottomCenter = new Vector2(x0 + radius, y0 + radius * 2f);
            
            output.AddRange(SVGGeomUtils.CubicCurve(matrix.Transform(topCenter), 
                                                    matrix.Transform(topCenter + handleRight), 
                                                    matrix.Transform(right + handleUp),
                                                    matrix.Transform(right)));
            
            output.AddRange(SVGGeomUtils.CubicCurve(matrix.Transform(right), 
                                                    matrix.Transform(right + handleDown), 
                                                    matrix.Transform(bottomCenter + handleRight),
                                                    matrix.Transform(bottomCenter)));
            
            output.AddRange(SVGGeomUtils.CubicCurve(matrix.Transform(bottomCenter), 
                                                    matrix.Transform(bottomCenter + handleLeft), 
                                                    matrix.Transform(left + handleDown),
                                                    matrix.Transform(left)));
            
            output.AddRange(SVGGeomUtils.CubicCurve(matrix.Transform(left), 
                                                    matrix.Transform(left + handleUp), 
                                                    matrix.Transform(topCenter + handleLeft),
                                                    matrix.Transform(topCenter)));
            return output;
        }
    }
}
