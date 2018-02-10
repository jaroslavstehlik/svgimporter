// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

//#define PATH_COMMAND_DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Rendering 
{
    using Geometry;
    using Utils;
    using Document;

    public class SVGPathElement : SVGParentable, ISVGDrawable, ISVGElement
    {
        private SVGPathSegList _segList;
        public SVGPathSegList segList
        {
            get {
                return _segList;
            }
        }

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

        public SVGPathElement(Node node, SVGTransformList inheritTransformList, SVGPaintable inheritPaintable = null) : base(inheritTransformList)
        {
            _attrList = node.attributes;
            _paintable = new SVGPaintable(inheritPaintable, node);
            currentTransformList = new SVGTransformList(_attrList.GetValue("transform"));
            Rect viewport = _paintable.viewport;
            this.currentTransformList.AppendItem(new SVGTransform(SVGTransformable.GetViewBoxTransform(_attrList, ref viewport, false)));
            paintable.SetViewport(viewport);
            Initial();
        }

        private void Initial()
        {
            string _d = _attrList.GetValue("d");
            SVGPathSeg lastSegment = null;
            SVGPathSeg firstPathSegment = null;

            List<char> _charList = new List<char>();
            List<string> _valueList = new List<string>();

            SVGStringExtractor.ExtractPathSegList(_d, ref _charList, ref _valueList);
            _segList = new SVGPathSegList(_charList.Count);
            int i, j, paramsLength;
            for (i = 0; i < _charList.Count; i++)
            {
                //lastSegment = _segList.GetLastItem();
                char _char = _charList [i];

                string _value = _valueList [i];

                float[] parms = SVGStringExtractor.ExtractTransformValueAsPX(_value);
                paramsLength = parms.Length;

                switch (_char)
                {
                    case 'Z':
                    case 'z':
                        if(_segList.Count > 0 && firstPathSegment != null)
                        {
                            lastSegment = _segList.AppendItem(new SVGPathSegLinetoAbs(firstPathSegment.currentPoint.x, firstPathSegment.currentPoint.y, lastSegment));
                        }
                        _segList.AppendItem(CreateSVGPathSegClosePath());
                        firstPathSegment = null;
                        break;
                    case 'M':
                        if(lastSegment != null && lastSegment.type != SVGPathSegTypes.Close && lastSegment.type != SVGPathSegTypes.MoveTo_Abs && lastSegment.type != SVGPathSegTypes.MoveTo_Rel)
                        {
                            firstPathSegment = null;
                        }
                        if(paramsLength < 2) continue;
                        for(j = 0; j < paramsLength; j+=2)
                        {
                            if(paramsLength - j < 2) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegMovetoAbs(parms [j], parms [j + 1], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'm':
                        if(lastSegment != null && lastSegment.type != SVGPathSegTypes.Close && lastSegment.type != SVGPathSegTypes.MoveTo_Abs && lastSegment.type != SVGPathSegTypes.MoveTo_Rel)
                        {
                            firstPathSegment = null;
                        }
                        if(paramsLength < 2) continue;
                        for(j = 0; j < paramsLength; j+=2)
                        {
                            if(paramsLength - j < 2) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegMovetoRel(parms [j], parms [j + 1], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'L':
                        if(paramsLength < 2) continue;
                        for(j = 0; j < paramsLength; j+=2)
                        {
                            if(paramsLength - j < 2) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegLinetoAbs(parms [j], parms [j + 1], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'l':
                        if(paramsLength < 2) continue;
                        for(j = 0; j < paramsLength; j+=2)
                        {
                            if(paramsLength - j < 2) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegLinetoRel(parms [j], parms [j + 1], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'C':
                        if(paramsLength < 6) continue;
                        for(j = 0; j < paramsLength; j+=6)
                        {
                            if(paramsLength - j < 6) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegCurvetoCubicAbs(parms [j], parms [j + 1], parms [j + 2], parms [j + 3], parms [j + 4], parms [j + 5], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'c':
                        if(paramsLength < 6) continue;
                        for(j = 0; j < paramsLength; j+=6)
                        {
                            if(paramsLength - j < 6) continue;
//                            Debug.Log(string.Format("CubicCurveRel: {0}, {1}, {2}, {3}, {4}, {5}", parms [j], parms [j + 1], parms [j + 2], parms [j + 3], parms [j + 4], parms [j + 5]));
                            lastSegment = _segList.AppendItem(new SVGPathSegCurvetoCubicRel(parms [j], parms [j + 1], parms [j + 2], parms [j + 3], parms [j + 4], parms [j + 5], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'S':
                        if(paramsLength < 4) continue;
                        for(j = 0; j < paramsLength; j+=4)
                        {
                            if(paramsLength - j < 4) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegCurvetoCubicSmoothAbs(parms [j], parms [j + 1], parms [j + 2], parms [j + 3], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 's':
                        if(paramsLength < 4) continue;
                        for(j = 0; j < paramsLength; j+=4)
                        {
                            if(paramsLength - j < 4) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegCurvetoCubicSmoothRel(parms [j], parms [j + 1], parms [j + 2], parms [j + 3], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'Q':
                        if(paramsLength < 4) continue;
                        for(j = 0; j < paramsLength; j+=4)
                        {
                            if(paramsLength - j < 4) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegCurvetoQuadraticAbs(parms [j], parms [j + 1], parms [j + 2], parms [j + 3], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'q':
                        if(paramsLength < 4) continue;
                        for(j = 0; j < paramsLength; j+=4)
                        {
                            if(paramsLength - j < 4) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegCurvetoQuadraticRel(parms [j], parms [j + 1], parms [j + 2], parms [j + 3], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'T':
                        if(paramsLength < 2) continue;
                        for(j = 0; j < paramsLength; j+=2)
                        {
                            if(paramsLength - j < 2) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegCurvetoQuadraticSmoothAbs(parms [j], parms [j + 1], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 't':
                        if(paramsLength < 2) continue;
                        for(j = 0; j < paramsLength; j+=2)
                        {
                            if(paramsLength - j < 2) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegCurvetoQuadraticSmoothRel(parms [j], parms [j + 1], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'A':
                        if(paramsLength < 7) continue;
                        for(j = 0; j < paramsLength; j+=7)
                        {
                            if(paramsLength - j < 7) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegArcAbs(parms [j], parms [j + 1], parms [j + 2], parms [j + 3] == 1f, parms [j + 4] == 1f, parms [j + 5], parms [j + 6], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'a':
                        if(paramsLength < 7) continue;
                        for(j = 0; j < paramsLength; j+=7)
                        {
                            if(paramsLength - j < 7) continue;
                            lastSegment = _segList.AppendItem(new SVGPathSegArcRel(parms [j], parms [j + 1], parms [j + 2], parms [j + 3] == 1f, parms [j + 4] == 1f, parms [j + 5], parms [j + 6], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'H':
                        for(j = 0; j < paramsLength; j++)
                        {
                            lastSegment = _segList.AppendItem(new SVGPathSegLinetoHorizontalAbs(parms [j], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'h':
                        for(j = 0; j < paramsLength; j++)
                        {
                            lastSegment = _segList.AppendItem(new SVGPathSegLinetoHorizontalRel(parms [j], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'V':
                        for(j = 0; j < paramsLength; j++)
                        {
                            lastSegment = _segList.AppendItem(new SVGPathSegLinetoVerticalAbs(parms [j], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                    case 'v':
                        for(j = 0; j < paramsLength; j++)
                        {
                            lastSegment = _segList.AppendItem(new SVGPathSegLinetoVerticalRel(parms [j], lastSegment));
                            if(firstPathSegment == null) { firstPathSegment = lastSegment; }
                        }
                        break;
                }
            }
        }

        private SVGPathSegClosePath CreateSVGPathSegClosePath()
        {
            SVGPathSeg lastSegment = _segList.GetLastItem();
            SVGPathSeg firstSegment = _segList.GetItem(0);

            if(firstSegment != null)
            {
                return new SVGPathSegClosePath(firstSegment.currentPoint, lastSegment);
            } else {
                return null;
            }
        }

        public void BeforeRender(SVGTransformList transformList)
        {
            inheritTransformList = transformList;
            for (int i = 0; i < _segList.Count; i++)
            {
                ISVGDrawable temp = _segList.GetItem(i) as ISVGDrawable;
                if (temp != null)
                    temp.BeforeRender(summaryTransformList);
            }
        }

        public List<List<Vector2>> GetPath()
        {            
            lastCommand = SVGPathSegTypes.Unknown;

            List<Vector2> positionBuffer = new List<Vector2>();
            List<List<Vector2>> output = new List<List<Vector2>>();
            for (int i = 0; i < segList.Count; i++)
            {
                GetSegment(this, segList.GetItem(i), output, positionBuffer, transformMatrix);
            }
            
            if(lastCommand != SVGPathSegTypes.Close && positionBuffer.Count > 0)
            {
                output.Add(new List<Vector2>(positionBuffer.ToArray()));
            }

            //Vector2 startPoint, endPoint;
            for(int i = 0; i < output.Count; i++)
            {
                // Ramer Douglas Peucker Reduction
                if(output[i] == null || output[i].Count < 3) continue;
                output[i] = SVGBezier.Optimise(output[i], SVGGraphics.vpm);
            }

            return output;
        }

        public List<List<Vector2>> GetClipPath()
        {
            List<List<Vector2>> path = GetPath();
            if(path == null || path.Count == 0) return null;

            List<List<Vector2>> clipPath = new List<List<Vector2>>();
            if(paintable.IsFill())
            {
                clipPath.AddRange(path);
            }

            if(paintable.IsStroke())
            {
                List<StrokeSegment[]> segments = new List<StrokeSegment[]>();
                for(int i = 0; i < path.Count; i++)
                {
                    if(path[i] == null || path[i].Count < 2) continue;
                    segments.Add(SVGSimplePath.GetSegments(path[i]));
                }

                List<List<Vector2>> strokePath = SVGLineUtils.StrokeShape(segments, paintable.strokeWidth, Color.black, SVGSimplePath.GetStrokeLineJoin(paintable.strokeLineJoin), SVGSimplePath.GetStrokeLineCap(paintable.strokeLineCap), paintable.miterLimit, paintable.dashArray, paintable.dashOffset, ClosePathRule.AUTO, SVGGraphics.roundQuality);
                if(strokePath != null && strokePath.Count > 0) clipPath.AddRange(strokePath);
            }

            return clipPath;
        }
        
        public void Render()
        {
            SVGGraphics.Create(this, "Path Element", ClosePathRule.AUTO);
        }

        static SVGPathSegTypes lastCommand;

        bool GetSegment(SVGPathElement svgElement, SVGPathSeg segment, List<List<Vector2>> output, List<Vector2> positionBuffer, SVGMatrix matrix)
        {
            if (segment == null)
                return false;

//            Debug.Log("command: "+segment.type+", lastCommand: "+lastCommand);

            switch (segment.type)
            {
                case SVGPathSegTypes.Arc_Abs:
                    SVGPathSegArcAbs arcAbs = segment as SVGPathSegArcAbs;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("arcAbs");
                    #endif
                    positionBuffer.AddRange(SVGGeomUtils.Arc(SVGGeomUtils.TransformPoint(arcAbs.previousPoint, matrix), arcAbs.r1, arcAbs.r2, arcAbs.angle, arcAbs.largeArcFlag, arcAbs.sweepFlag, SVGGeomUtils.TransformPoint(arcAbs.currentPoint, matrix)));
                    break;
                case SVGPathSegTypes.Arc_Rel:
                    SVGPathSegArcRel arcRel = segment as SVGPathSegArcRel;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("arcRel");
                    #endif
                    positionBuffer.AddRange(SVGGeomUtils.Arc(SVGGeomUtils.TransformPoint(arcRel.previousPoint, matrix), arcRel.r1, arcRel.r2, arcRel.angle, arcRel.largeArcFlag, arcRel.sweepFlag, SVGGeomUtils.TransformPoint(arcRel.currentPoint, matrix)));
                    break;
                case SVGPathSegTypes.Close:
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("Close");
                    #endif
                    //Debug.Log("Close");
                    if(positionBuffer.Count > 0)
                    {
                        output.Add(new List<Vector2>(positionBuffer.ToArray()));
                    }
                    positionBuffer.Clear();
                    break;
                case SVGPathSegTypes.CurveTo_Cubic_Abs:
                    SVGPathSegCurvetoCubicAbs curvetoCubicAbs = segment as SVGPathSegCurvetoCubicAbs;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("curvetoCubicAbs");
                    #endif
                    positionBuffer.AddRange(SVGGeomUtils.CubicCurve(SVGGeomUtils.TransformPoint(curvetoCubicAbs.previousPoint, matrix), SVGGeomUtils.TransformPoint(curvetoCubicAbs.controlPoint1, matrix), SVGGeomUtils.TransformPoint(curvetoCubicAbs.controlPoint2, matrix), SVGGeomUtils.TransformPoint(curvetoCubicAbs.currentPoint, matrix)));
                    break;
                case SVGPathSegTypes.CurveTo_Cubic_Rel:
                    SVGPathSegCurvetoCubicRel curvetoCubicRel = segment as SVGPathSegCurvetoCubicRel;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("curvetoCubicRel");
                    #endif          
                    positionBuffer.AddRange(SVGGeomUtils.CubicCurve(SVGGeomUtils.TransformPoint(curvetoCubicRel.previousPoint, matrix), SVGGeomUtils.TransformPoint(curvetoCubicRel.controlPoint1, matrix), SVGGeomUtils.TransformPoint(curvetoCubicRel.controlPoint2, matrix), SVGGeomUtils.TransformPoint(curvetoCubicRel.currentPoint, matrix)));
                    break;
                case SVGPathSegTypes.CurveTo_Cubic_Smooth_Abs:
                    SVGPathSegCurvetoCubicSmoothAbs curvetoCubicSmoothAbs = segment as SVGPathSegCurvetoCubicSmoothAbs;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("curvetoCubicSmoothAbs");
                    #endif               
                    positionBuffer.AddRange(SVGGeomUtils.CubicCurve(SVGGeomUtils.TransformPoint(curvetoCubicSmoothAbs.previousPoint, matrix), SVGGeomUtils.TransformPoint(curvetoCubicSmoothAbs.controlPoint1, matrix), SVGGeomUtils.TransformPoint(curvetoCubicSmoothAbs.controlPoint2, matrix), SVGGeomUtils.TransformPoint(curvetoCubicSmoothAbs.currentPoint, matrix)));
                    break;
                case SVGPathSegTypes.CurveTo_Cubic_Smooth_Rel:
                    SVGPathSegCurvetoCubicSmoothRel curvetoCubicSmoothRel = segment as SVGPathSegCurvetoCubicSmoothRel;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("curvetoCubicSmoothRel");
                    #endif
                    positionBuffer.AddRange(SVGGeomUtils.CubicCurve(SVGGeomUtils.TransformPoint(curvetoCubicSmoothRel.previousPoint, matrix), SVGGeomUtils.TransformPoint(curvetoCubicSmoothRel.controlPoint1, matrix), SVGGeomUtils.TransformPoint(curvetoCubicSmoothRel.controlPoint2, matrix), SVGGeomUtils.TransformPoint(curvetoCubicSmoothRel.currentPoint, matrix)));
                    
                    break;
                case SVGPathSegTypes.CurveTo_Quadratic_Abs:
                    SVGPathSegCurvetoQuadraticAbs curvetoQuadraticAbs = segment as SVGPathSegCurvetoQuadraticAbs;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("curvetoQuadraticAbs");
                    #endif
                    positionBuffer.AddRange(SVGGeomUtils.QuadraticCurve(SVGGeomUtils.TransformPoint(curvetoQuadraticAbs.previousPoint, matrix), SVGGeomUtils.TransformPoint(curvetoQuadraticAbs.controlPoint1, matrix), SVGGeomUtils.TransformPoint(curvetoQuadraticAbs.currentPoint, matrix)));
                    break;
                case SVGPathSegTypes.CurveTo_Quadratic_Rel:
                    SVGPathSegCurvetoQuadraticRel curvetoQuadraticRel = segment as SVGPathSegCurvetoQuadraticRel;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("curvetoQuadraticRel");
                    #endif
                    positionBuffer.AddRange(SVGGeomUtils.QuadraticCurve(SVGGeomUtils.TransformPoint(curvetoQuadraticRel.previousPoint, matrix), SVGGeomUtils.TransformPoint(curvetoQuadraticRel.controlPoint1, matrix), SVGGeomUtils.TransformPoint(curvetoQuadraticRel.currentPoint, matrix)));
                    
                    break;
                case SVGPathSegTypes.CurveTo_Quadratic_Smooth_Abs:
                    SVGPathSegCurvetoQuadraticSmoothAbs curvetoQuadraticSmoothAbs = segment as SVGPathSegCurvetoQuadraticSmoothAbs;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("curvetoQuadraticSmoothAbs");
                    #endif
                    positionBuffer.AddRange(SVGGeomUtils.QuadraticCurve(SVGGeomUtils.TransformPoint(curvetoQuadraticSmoothAbs.previousPoint, matrix), SVGGeomUtils.TransformPoint(curvetoQuadraticSmoothAbs.controlPoint1, matrix), SVGGeomUtils.TransformPoint(curvetoQuadraticSmoothAbs.currentPoint, matrix)));
                    
                    break;
                case SVGPathSegTypes.CurveTo_Quadratic_Smooth_Rel:
                    SVGPathSegCurvetoQuadraticSmoothRel curvetoQuadraticSmoothRel = segment as SVGPathSegCurvetoQuadraticSmoothRel;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("curvetoQuadraticSmoothRel");
                    #endif
                    positionBuffer.AddRange(SVGGeomUtils.QuadraticCurve(SVGGeomUtils.TransformPoint(curvetoQuadraticSmoothRel.previousPoint, matrix), SVGGeomUtils.TransformPoint(curvetoQuadraticSmoothRel.controlPoint1, matrix), SVGGeomUtils.TransformPoint(curvetoQuadraticSmoothRel.currentPoint, matrix)));
                    
                    break;
                case SVGPathSegTypes.LineTo_Abs:
                    SVGPathSegLinetoAbs linetoAbs = segment as SVGPathSegLinetoAbs;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("linetoAbs");
                    #endif
                    positionBuffer.Add(SVGGeomUtils.TransformPoint(linetoAbs.currentPoint, matrix));
                    break;
                case SVGPathSegTypes.LineTo_Horizontal_Abs:
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("linetoHorizontalAbs");
                    #endif
                    SVGPathSegLinetoHorizontalAbs linetoHorizontalAbs = segment as SVGPathSegLinetoHorizontalAbs;
                    positionBuffer.Add(SVGGeomUtils.TransformPoint(linetoHorizontalAbs.currentPoint, matrix));
                    break;
                case SVGPathSegTypes.LineTo_Horizontal_Rel:
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("linetoHorizontalRel");
                    #endif
                    SVGPathSegLinetoHorizontalRel linetoHorizontalRel = segment as SVGPathSegLinetoHorizontalRel;
                    positionBuffer.Add(SVGGeomUtils.TransformPoint(linetoHorizontalRel.currentPoint, matrix));
                    break;
                case SVGPathSegTypes.LineTo_Rel:
                    SVGPathSegLinetoRel linetoRel = segment as SVGPathSegLinetoRel;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("linetoRel");
                    #endif
                    positionBuffer.Add(SVGGeomUtils.TransformPoint(linetoRel.currentPoint, matrix));
                    break;
                case SVGPathSegTypes.LineTo_Vertical_Abs:
                    SVGPathSegLinetoVerticalAbs linetoVerticalAbs = segment as SVGPathSegLinetoVerticalAbs;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("linetoVerticalAbs");
                    #endif
                    positionBuffer.Add(SVGGeomUtils.TransformPoint(linetoVerticalAbs.currentPoint, matrix));
                    break;
                case SVGPathSegTypes.LineTo_Vertical_Rel:
                    SVGPathSegLinetoVerticalRel linetoVerticalRel = segment as SVGPathSegLinetoVerticalRel;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("linetoVerticalRel");
                    #endif
                    positionBuffer.Add(SVGGeomUtils.TransformPoint(linetoVerticalRel.currentPoint, matrix));
                    break;
                case SVGPathSegTypes.MoveTo_Abs:
                    if(lastCommand != SVGPathSegTypes.Close && lastCommand != SVGPathSegTypes.MoveTo_Abs && lastCommand != SVGPathSegTypes.MoveTo_Rel)
                    {
                        if(positionBuffer.Count > 0)
                        {
                            output.Add(new List<Vector2>(positionBuffer.ToArray()));
                            positionBuffer.Clear();
                        }
                    }
                    SVGPathSegMovetoAbs movetoAbs = segment as SVGPathSegMovetoAbs;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("movetoAbs");
                    #endif
                    positionBuffer.Add(SVGGeomUtils.TransformPoint(movetoAbs.currentPoint, matrix));
                    break;
                case SVGPathSegTypes.MoveTo_Rel:
                    if(lastCommand != SVGPathSegTypes.Close && lastCommand != SVGPathSegTypes.MoveTo_Abs && lastCommand != SVGPathSegTypes.MoveTo_Rel)
                    {
                        if(positionBuffer.Count > 0)
                        {
                            output.Add(new List<Vector2>(positionBuffer.ToArray()));
                            positionBuffer.Clear();
                        }
                    }
                    SVGPathSegMovetoRel movetoRel = segment as SVGPathSegMovetoRel;
                    #if PATH_COMMAND_DEBUG
                    Debug.Log("movetoRel");
                    #endif
                    positionBuffer.Add(SVGGeomUtils.TransformPoint(movetoRel.currentPoint, matrix));
                    break;
            }
            
            lastCommand = segment.type;
            return true;
        }
    }
}
