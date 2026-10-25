

using UnityEngine;
using System;
using System.Collections.Generic;

namespace SVGImporter.Rendering
{
    using Data;
    using Geometry;
    using Utils;

    public class SVGGraphics
    {
        public static List<SVGPath> paths;
        public static List<SVGLayer> layers;

        public static void AddLayer(SVGLayer layer)
        {
            layers.Add(layer);
        }
        
        public static void Create(ISVGElement svgElement, string defaultName = null, ClosePathRule closePathRule = ClosePathRule.ALWAYS)
        {        
            if(svgElement == null) return;
            if(svgElement.paintable.visibility != SVGVisibility.Visible || svgElement.paintable.display == SVGDisplay.None) return;
            
            List<SVGShape> shapes = new List<SVGShape>();
            List<List<Vector2>> inputPaths = svgElement.GetPath();
            if(inputPaths.Count == 1)
            {
                if(svgElement.paintable.IsFill())
                {
                    List<List<Vector2>> path = inputPaths;
                    if(svgElement.paintable.clipPathList != null && svgElement.paintable.clipPathList.Count > 0)
                    {
                        path = SVGGeom.ClipPolygon(new List<List<Vector2>>(){inputPaths[0]}, svgElement.paintable.clipPathList);
                    }

                    SVGShape[] addShapes = SVGGraphics.GetShapes(path, svgElement.paintable, svgElement.transformMatrix);
                    if(addShapes != null && addShapes.Length > 0) shapes.AddRange(addShapes);
                }
                if(svgElement.paintable.IsStroke())
                {
                    List<List<Vector2>> path = SVGSimplePath.CreateStroke(inputPaths[0], svgElement.paintable, closePathRule);
                    if(svgElement.paintable.clipPathList != null && svgElement.paintable.clipPathList.Count > 0)
                    {
                        path = SVGGeom.ClipPolygon(path, svgElement.paintable.clipPathList);
                    }
                    
                    SVGShape[] addShapes = SVGGraphics.GetShapes(path, svgElement.paintable, svgElement.transformMatrix, true);
                    if(addShapes != null && addShapes.Length > 0) shapes.AddRange(addShapes);
                }
            } else {
                if(svgElement.paintable.IsFill())
                {
                    List<List<Vector2>> fillPaths = inputPaths;
                    if(svgElement.paintable.clipPathList != null && svgElement.paintable.clipPathList.Count > 0)
                    {
                        fillPaths = SVGGeom.ClipPolygon(inputPaths, svgElement.paintable.clipPathList);
                    } 

                    SVGShape[] addShapes = SVGGraphics.GetShapes(fillPaths, svgElement.paintable, svgElement.transformMatrix);
                    if(addShapes != null && addShapes.Length > 0) shapes.AddRange(addShapes);
                }
                if(svgElement.paintable.IsStroke())
                {
                    List<List<Vector2>> strokePath = SVGSimplePath.CreateStroke(inputPaths, svgElement.paintable, closePathRule);
                    if(svgElement.paintable.clipPathList != null && svgElement.paintable.clipPathList.Count > 0)
                    {
                        strokePath = SVGGeom.ClipPolygon(strokePath, svgElement.paintable.clipPathList);
                    }
                    
                    SVGShape[] addShapes = SVGGraphics.GetShapes(strokePath, svgElement.paintable, svgElement.transformMatrix, true);
                    if(addShapes != null && addShapes.Length > 0) shapes.AddRange(addShapes);
                }
            }

            if(shapes.Count > 0)
            {
                string name = svgElement.attrList.GetValue("id");
                if (string.IsNullOrEmpty(name)) name = defaultName;
                SVGLayer layer = new SVGLayer();
                layer.shapes = shapes.ToArray();
                layer.name = name;
                SVGGraphics.AddLayer(layer);
            }
        }

        public static void CorrectSVGLayers(List<SVGLayer> layers, Rect viewport, SVGAsset asset, out Vector2 offset)
        {
            CorrectSVGLayers(layers, viewport, asset.ignoreSVGCanvas, asset.pivotPoint, out offset);
        }

        /// <summary>
        /// Flips the shapes to y-up and moves them so the pivot (of the content, or of the canvas
        /// when ignoreSVGCanvas is off) is at the origin. viewport is the canvas in mesh units.
        /// </summary>
        public static void CorrectSVGLayers(List<SVGLayer> layers, Rect viewport, bool ignoreSVGCanvas, Vector2 pivotPoint, out Vector2 offset)
        {
            offset = Vector2.zero;
            if(layers == null) return;
            int layersCount = layers.Count, layersShapesLength = 0, layersShapesVerticesLength = 0;
            for(int i = 0; i < layersCount; i++)
            {
                CorrectSVGLayerShape(layers[i].shapes);
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            // calculate bounds
            for(int i = 0; i < layersCount; i++)
            {
                if(layers[i].shapes == null) continue;
                layersShapesLength = layers[i].shapes.Length;
                for(int j = 0; j < layersShapesLength; j++)
                {
                    Vector2 min = layers[i].shapes[j].bounds.min;
                    Vector2 max = layers[i].shapes[j].bounds.max;
                    
                    if(min.x < minX) minX = min.x;
                    if(max.x > maxX) maxX = max.x;
                    if(min.y < minY) minY = min.y;
                    if(max.y > maxY) maxY = max.y;
                }
            }

            Rect bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            if(ignoreSVGCanvas)
            {
                offset = new Vector2(bounds.min.x + bounds.size.x * pivotPoint.x,
                                     (bounds.max.y - bounds.size.y * pivotPoint.y));
            } else {
                // The vertices are flipped to y-up above, the viewport isn't: its top edge is at
                // -viewport.min.y.
                offset = new Vector2(viewport.min.x + viewport.size.x * pivotPoint.x,
                                     -(viewport.min.y + viewport.size.y * pivotPoint.y));
            }

            // update vertices
            for(int i = 0; i < layersCount; i++)
            {
                if(layers[i].shapes == null) continue;
                layersShapesLength = layers[i].shapes.Length;
                for(int j = 0; j < layersShapesLength; j++)
                {
                    if(layers[i].shapes[j].vertices == null) continue;
                    layersShapesVerticesLength = layers[i].shapes[j].vertices.Length;
                    for( int k = 0; k < layersShapesVerticesLength; k++)
                    {
                        layers[i].shapes[j].vertices[k] -= offset;
                    }
                    layers[i].shapes[j].bounds.center -= offset;

                    if(layers[i].shapes[j].fill != null)
                    {
                        layers[i].shapes[j].fill.transform = layers[i].shapes[j].fill.transform.Translate(offset);
                    }
                }
            }
        }

        protected static void CorrectSVGLayerShape(SVGShape[] shapes)
        {

            for(int i = 0; i < shapes.Length; i++)
            {
                int vertexCount = shapes[i].vertexCount;
                if(vertexCount == 0) continue;
                if(shapes[i].fill != null)
                {
                    shapes[i].fill.transform = shapes[i].fill.transform.Scale(1f, -1f);
                }

                for(int j = 0; j < vertexCount; j++)
                {
                    shapes[i].vertices[j].y *= -1f;
                }

                // The antialiasing extrusion vectors are directions in the same space.
                if(shapes[i].angles != null)
                {
                    for(int j = 0; j < shapes[i].angles.Length; j++)
                    {
                        shapes[i].angles[j].y *= -1f;
                    }
                }

                shapes[i].bounds.center = new Vector2(shapes[i].bounds.center.x, shapes[i].bounds.center.y * -1f);
            }
        }

        public static SVGShape[] GetShapes(List<List<Vector2>> inputShapes, SVGPaintable paintable, SVGMatrix matrix, bool isStroke = false)
        {
            SVGShape[] shapes = null;;
            SVGShape shape;
            SVGShape antialiasingShape;

            if(SVGSimplePath.CreatePolygon(inputShapes, paintable, matrix, out shape, out antialiasingShape, isStroke, _antialiasing))
            {
                if(_antialiasing)
                {
                    shapes = new SVGShape[]{shape, antialiasingShape};
                } else {
                    shapes = new SVGShape[]{shape};
                }
            }

            return shapes;
        }

        protected static float _vpm;
        public static float vpm
        {
            get {
                return _vpm;
            }
        }

        public static float _roundQuality = 0f;
        public static float roundQuality
        {
            get {
                return _roundQuality;
            }
        }

        private static float _vertexPerMeter = 1000f;
        public static float vertexPerMeter
        {
            get {           
                return _vertexPerMeter;
            }
        }

        private static bool _antialiasing = false;
        public static bool antialiasing
        {
            get {           
                return _antialiasing;
            }
        }

        public static void Clear()
        {
            if(layers != null)
            {
                layers.Clear();
                layers = null;
            }

            if(paths != null)
            {
                paths.Clear();
                paths = null;
            }
        }

        public static void Init()
        {
            if(layers == null)
                layers = new List<SVGLayer>();
            if(paths == null)
                paths = new List<SVGPath>();
        }

        private SVGStrokeLineCapMethod   _strokeLineCap = SVGStrokeLineCapMethod.Unknown;
        private SVGStrokeLineJoinMethod  _strokeLineJoin = SVGStrokeLineJoinMethod.Unknown;

        public SVGStrokeLineCapMethod strokeLineCap
        {
            get { return this._strokeLineCap; }
        }

        public SVGStrokeLineJoinMethod strokeLineJoin
        {
            get { return this._strokeLineJoin; }
        }

        public SVGGraphics(float vertexPerMeter = 1000f, bool antialiasing = false)
        {
            _vpm = 1f;
            if(vertexPerMeter > 0f)
			{
                _vpm = 1000f / vertexPerMeter;
			} else {
				_vpm = 1000f;
			}

            if(_vpm != 0f)
            {
                _roundQuality = (1f / _vpm) * 0.5f;
            } else {
                _roundQuality = 0f;
            }

            _vertexPerMeter = vertexPerMeter;
            _antialiasing = antialiasing;
        }

        public void SetStrokeLineCap(SVGStrokeLineCapMethod strokeLineCap)
        {
            this._strokeLineCap = strokeLineCap;
        }

        public void SetStrokeLineJoin(SVGStrokeLineJoinMethod strokeLineJoin)
        {
            this._strokeLineJoin = strokeLineJoin;
        }

    }
}
