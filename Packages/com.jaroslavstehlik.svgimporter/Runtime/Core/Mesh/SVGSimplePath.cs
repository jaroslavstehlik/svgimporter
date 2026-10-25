

using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Rendering
{
    using Document;
    using Utils;
    using ClipperLib;
    using Geometry;

    public class SVGSimplePath {

        public static StrokeLineCap GetStrokeLineCap(SVGStrokeLineCapMethod capMethod)
        {
            switch(capMethod)
            {
                case SVGStrokeLineCapMethod.Butt:
                    return StrokeLineCap.butt;
                case SVGStrokeLineCapMethod.Round:
                    return StrokeLineCap.round;
                case SVGStrokeLineCapMethod.Square:
                    return StrokeLineCap.square;
            }

            return StrokeLineCap.butt;
        }

        public static StrokeLineJoin GetStrokeLineJoin(SVGStrokeLineJoinMethod capMethod)
        {
            switch(capMethod)
            {
                case SVGStrokeLineJoinMethod.Miter:
                    return StrokeLineJoin.miter;
                case SVGStrokeLineJoinMethod.MiterClip:
                    return StrokeLineJoin.miterClip;
                case SVGStrokeLineJoinMethod.Round:
                    return StrokeLineJoin.round;
                case SVGStrokeLineJoinMethod.Bevel:
                    return StrokeLineJoin.bevel;
            }
            
            return StrokeLineJoin.bevel;
        }

        public static StrokeSegment[] GetSegments(List<Vector2> points)
        {
            if(points == null || points.Count < 2)
                return null;

            for(int i = 1; i < points.Count; i++)
            {
                if(points[i - 1] == points[i])
                {
                    points.RemoveAt(i - 1);
                    i--;
                }
            }

            List<StrokeSegment> segments = new List<StrokeSegment>();
            for(int i = 1; i < points.Count; i++)
            {
                segments.Add(new StrokeSegment(points[i - 1], points[i]));
            }

            return segments.ToArray();
        }      

        public static List<List<Vector2>> CreateStroke(List<Vector2> inputShapes, SVGPaintable paintable, ClosePathRule closePath = ClosePathRule.NEVER)
        {
            if(inputShapes == null || inputShapes.Count == 0 || paintable == null || paintable.strokeWidth <= 0f)
                return null;
            
            return CreateStroke(new List<List<Vector2>>(){inputShapes}, paintable, closePath);
        }
        
        public static List<List<Vector2>> CreateStroke(List<List<Vector2>> inputShapes, SVGPaintable paintable, ClosePathRule closePath = ClosePathRule.NEVER)
        {
            if(inputShapes == null || inputShapes.Count == 0 || paintable == null || paintable.strokeWidth <= 0f)
                return null;

            List<StrokeSegment[]> segments = new List<StrokeSegment[]>();
            for(int i = 0; i < inputShapes.Count; i++)
            {
                if(inputShapes[i] == null || inputShapes[i].Count < 2)
                    continue;
                
                segments.Add(GetSegments(inputShapes[i]));
            }

            return SVGLineUtils.StrokeShape(segments, paintable.strokeWidth, Color.black, GetStrokeLineJoin(paintable.strokeLineJoin), GetStrokeLineCap(paintable.strokeLineCap), paintable.miterLimit, paintable.dashArray, paintable.dashOffset, closePath, SVGGraphics.roundQuality);
        }

        /// <summary>
        /// The antialiasing fringe of a shape (SVGFringe), in mesh units, and the fill's vertices on
        /// its outline moved inward to meet it. contours are in SVG units; the fill is in mesh units.
        /// </summary>
        public static bool CreateAntialiasing(List<List<Vector2>> contours, float meshScale, ref SVGShape fill, out SVGShape fringe)
        {
            Dictionary<Vector2, Vector2> boundary;
            if(!SVGFringe.Build(contours, Color.white, out fringe, out boundary)) return false;
            for(int i = 0; i < fringe.vertices.Length; i++)
                fringe.vertices[i] *= meshScale;
            var scaledBoundary = new Dictionary<Vector2, Vector2>(boundary.Count);
            foreach(KeyValuePair<Vector2, Vector2> point in boundary)
                scaledBoundary[point.Key * meshScale] = point.Value;
            SVGFringe.ExtrudeFill(ref fill, scaledBoundary);

            fringe.type = SVGShapeType.ANTIALIASING;
            fringe.RecalculateBounds();
            fringe.fill = fill.fill.Clone();
            fringe.fill.blend = FILL_BLEND.ALPHA_BLENDED;
            return true;
        }

        struct VertexData
        {
            public int shapeIndex;
            public int vertexIndex;
            public int totalIndex;

            public VertexData(int shapeIndex, int vertexIndex, int totalIndex)
            {
                this.shapeIndex = shapeIndex;
                this.vertexIndex = vertexIndex;
                this.totalIndex = totalIndex;
            }
        }

        public static bool CreatePolygon(List<List<Vector2>> inputShapes, SVGPaintable paintable, SVGMatrix matrix, out SVGShape layer, out SVGShape antialiasingLayer, bool isStroke = false, bool antialiasing = false)
        {   
            layer = new SVGShape();
            antialiasingLayer = new SVGShape();

            if(inputShapes == null || inputShapes.Count == 0)
            {
                return false;
            }

            List<List<Vector2>> simplifiedShapes = new List<List<Vector2>>();
            PolyFillType fillType = PolyFillType.pftNonZero;
            if(paintable.fillRule == SVGFillRule.EvenOdd) { fillType = PolyFillType.pftEvenOdd; }
            simplifiedShapes = SVGGeom.SimplifyPolygons(inputShapes, fillType);
            if(simplifiedShapes == null || simplifiedShapes.Count == 0) return false;

            AddInputShape(simplifiedShapes);

            Rect bounds = GetRect(simplifiedShapes);            
            Rect viewport = paintable.viewport;

            if(!isStroke)
            {
                switch (paintable.GetPaintType())
                {
                    case SVGPaintMethod.SolidFill:
                    {
                        layer.type = SVGShapeType.FILL;
                        Color color = Color.black;
                        SVGColorType colorType = paintable.fillColor.Value.colorType;
                        if(colorType == SVGColorType.Unknown || colorType == SVGColorType.None)
                        {
                            color.a *= paintable.fillOpacity;
                            paintable.svgFill = new SVGFill(color);
                        } else {
                            color = paintable.fillColor.Value.color;
                            color.a *= paintable.fillOpacity; 
                            paintable.svgFill = new SVGFill(color);
                        }

                        paintable.svgFill.fillType = FILL_TYPE.SOLID;
                        if(color.a == 1)
                        {
                            paintable.svgFill.blend = FILL_BLEND.OPAQUE;
                        } else {
                            paintable.svgFill.blend = FILL_BLEND.ALPHA_BLENDED;
                        }
                    }
                        break;
                    case SVGPaintMethod.LinearGradientFill:      
                    {
                        layer.type = SVGShapeType.FILL;
                        SVGLinearGradientBrush linearGradBrush = paintable.GetLinearGradientBrush(bounds, GetLocalBounds(simplifiedShapes, matrix), matrix, viewport);
                        paintable.svgFill = linearGradBrush.fill;
                    }
                        break;
                    case SVGPaintMethod.RadialGradientFill:
                    {
                        layer.type = SVGShapeType.FILL;
                        SVGRadialGradientBrush radialGradBrush = paintable.GetRadialGradientBrush(bounds, GetLocalBounds(simplifiedShapes, matrix), matrix, viewport);
                        paintable.svgFill = radialGradBrush.fill;
                    }
                        break;
                    case SVGPaintMethod.ConicalGradientFill:
                    {
                        layer.type = SVGShapeType.FILL;
                        SVGConicalGradientBrush conicalGradBrush = paintable.GetConicalGradientBrush(bounds, GetLocalBounds(simplifiedShapes, matrix), matrix, viewport);
                        paintable.svgFill = conicalGradBrush.fill;
                    }
                        break;
                    case SVGPaintMethod.PathDraw:  
                    {
                        layer.type = SVGShapeType.STROKE;
                        Color color = Color.black;
                        SVGColorType colorType = paintable.fillColor.Value.colorType;
                        if(colorType == SVGColorType.Unknown || colorType == SVGColorType.None)
                        {
                            color.a *= paintable.strokeOpacity;
                            paintable.svgFill = new SVGFill(color);
                        } else {
                            color = paintable.fillColor.Value.color;
                            color.a *= paintable.strokeOpacity;
                            paintable.svgFill = new SVGFill(color);
                        }

                        paintable.svgFill.fillType = FILL_TYPE.SOLID;
                        if(color.a == 1)
                        {
                            paintable.svgFill.blend = FILL_BLEND.OPAQUE;
                        } else {
                            paintable.svgFill.blend = FILL_BLEND.ALPHA_BLENDED;
                        }
                    }
                        break;
                    default:
                        break;
                }
            } else {
                layer.type = SVGShapeType.STROKE;
                Color color = paintable.strokeColor.Value.color;
                color.a *= paintable.strokeOpacity;
                paintable.svgFill = new SVGFill(color, FILL_BLEND.OPAQUE, FILL_TYPE.SOLID);
                if(color.a != 1f) paintable.svgFill.blend = FILL_BLEND.ALPHA_BLENDED;
                paintable.svgFill.color = color;
            }

            // Use LibTessDotNet
            if(true)
            {
                LibTessDotNet.Tess tesselation = new LibTessDotNet.Tess();
                LibTessDotNet.ContourVertex[] path;
                int pathLength;
                for(int i = 0; i < simplifiedShapes.Count; i++)
                {
                    if(simplifiedShapes[i] == null)
                        continue;
                    
                    pathLength = simplifiedShapes[i].Count;
                    path = new LibTessDotNet.ContourVertex[pathLength];
                    Vector2 position;
                    for(int j = 0; j < pathLength; j++)
                    {
                        position = simplifiedShapes[i][j];
                        path[j].Position = new LibTessDotNet.Vec3{X = position.x, Y = position.y, Z = 0f};
                    }
                    tesselation.AddContour(path, SVGImporter.LibTessDotNet.ContourOrientation.Clockwise);
                }

                tesselation.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3);
                int meshVertexCount = tesselation.Vertices.Length;
                if(meshVertexCount == 0) return false;

                int numTriangles = tesselation.ElementCount;
                layer.triangles = new int[numTriangles * 3];
                layer.vertices = new Vector2[meshVertexCount];

                for (int i = 0; i < numTriangles; i++)
                {
                    layer.triangles[i * 3] = tesselation.Elements[i * 3];
                    layer.triangles[i * 3 + 1] = tesselation.Elements[i * 3 + 1];                   
                    layer.triangles[i * 3 + 2] = tesselation.Elements[i * 3 + 2];
                }
                
                for(int i = 0; i < meshVertexCount; i++)
                {
                    layer.vertices[i] = new Vector2(tesselation.Vertices[i].Position.X, tesselation.Vertices[i].Position.Y) * SVGAssetImport.meshScale;
                }
            }

            layer.fill = paintable.svgFill;
            layer.fill.opacity = paintable.opacity;
            if(layer.fill.opacity < 1f && layer.fill.blend == FILL_BLEND.OPAQUE)
            {
                layer.fill.blend = FILL_BLEND.ALPHA_BLENDED;
            }

            if(layer.fill.fillType == FILL_TYPE.GRADIENT && layer.fill.gradientColors != null)
            {
                layer.fill.color = Color.white;
            } else if(layer.fill.fillType == FILL_TYPE.TEXTURE)
            {
                layer.fill.color = Color.white;
            }

            viewport.x *= SVGAssetImport.meshScale;
            viewport.y *= SVGAssetImport.meshScale;
            viewport.size *= SVGAssetImport.meshScale;
            layer.fill.viewport = viewport;

            SVGMatrix scaleMatrix = SVGMatrix.identity.Scale(SVGAssetImport.meshScale);
            layer.fill.transform = scaleMatrix.Multiply(layer.fill.transform);
            layer.fill.transform = layer.fill.transform.Multiply(scaleMatrix.Inverse());

            Vector2 boundsMin = bounds.min * SVGAssetImport.meshScale;
            Vector2 boundsMax = bounds.max * SVGAssetImport.meshScale;
            layer.bounds = new Rect(boundsMin.x, 
                                    boundsMin.y, 
                                    boundsMax.x - boundsMin.x, 
                                    boundsMax.y - boundsMin.y);

            if(antialiasing)
            {
                CreateAntialiasing(simplifiedShapes, SVGAssetImport.meshScale, ref layer, out antialiasingLayer);
            }

            return true;
        }
        private static void UpdateMesh(Mesh mesh, SVGFill svgFill)
        {
            if (svgFill.fillType == FILL_TYPE.GRADIENT && svgFill.gradientColors != null)
            {
                SVGMeshUtils.ChangeMeshUV2(mesh, new Vector2(svgFill.gradientColors.index, (int)svgFill.gradientType));
            } else {
                SVGMeshUtils.ChangeMeshColor(mesh, svgFill.color);
            }
        }

        private static Bounds GetBounds(List<Vector2> array)
        {
            if(array == null || array.Count == 0)
                return new Bounds();

            Bounds bounds = new Bounds();
            bounds.SetMinMax(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), new Vector3(float.MinValue, float.MinValue, float.MinValue));
            int arrayLength = array.Count;
            for(int i = 0; i < arrayLength; i++)
            {
                bounds.Encapsulate(array[i]);
            }

            return bounds;
        }
        
        private static Rect GetRect(List<Vector2> array)
        {
            if(array == null || array.Count == 0)
                return new Rect();

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            int arrayLength = array.Count;
            for(int i = 0; i < arrayLength; i++)
            {
                if(array[i].x < min.x)
                    min.x = array[i].x;
                if(array[i].y < min.y)
                    min.y = array[i].y;
                if(array[i].x > max.x)
                    max.x = array[i].x;
                if(array[i].y > max.y)
                    max.y = array[i].y;
            }
            
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        private static Rect GetRect(List<List<Vector2>> array)
        {
            if(array == null || array.Count == 0)
                return new Rect();
            
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            int arrayLength = array.Count;
            int nestedArrayLength;
            for(int i = 0; i < arrayLength; i++)
            {
                if(array[i] == null)
                    continue;

                nestedArrayLength = array[i].Count;
                for(int j = 0; j < nestedArrayLength; j++)
                {
                    if(array[i][j].x < min.x)
                        min.x = array[i][j].x;
                    if(array[i][j].y < min.y)
                        min.y = array[i][j].y;
                    if(array[i][j].x > max.x)
                        max.x = array[i][j].x;
                    if(array[i][j].y > max.y)
                        max.y = array[i][j].y;
                }
            }
            
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        // The bounding box of the shapes in the element's own coordinate system, before its transform.
        // objectBoundingBox gradients are defined relative to this box.
        static Rect GetLocalBounds(List<List<Vector2>> shapes, SVGMatrix matrix)
        {
            SVGMatrix inverse;
            try {
                inverse = matrix.Inverse();
            } catch(SVGException) {
                return GetRect(shapes);
            }

            List<List<Vector2>> localShapes = new List<List<Vector2>>(shapes.Count);
            for(int i = 0; i < shapes.Count; i++)
            {
                List<Vector2> localShape = new List<Vector2>(shapes[i].Count);
                for(int j = 0; j < shapes[i].Count; j++)
                {
                    localShape.Add(inverse.Transform(shapes[i][j]));
                }
                localShapes.Add(localShape);
            }
            return GetRect(localShapes);
        }

        // A coordinate in objectBoundingBox units: 0..1, where percentages map 100% to 1.
        static float GetUnitValue(SVGLength length)
        {
            return length.unitType == SVGLengthType.Percentage ? length.value / 100f : length.value;
        }

        protected static Vector2 GetGradientVector(SVGLength posX, SVGLength posY, Rect bounds)
        {
            Vector2 point = Vector2.zero;
            if(posX.unitType != SVGLengthType.Percentage)
            { 
                point.x = posX.value; 
            } else { 
                point.x = bounds.x + bounds.width * (posX.value / 100f); 
            }

            if(posY.unitType != SVGLengthType.Percentage) 
            { 
                point.y = posY.value; 
            } else 
            { 
                point.y = bounds.y + bounds.height * (posY.value / 100f); 
            }

            return point;
        }

        //const float defaultViewportScale = 800f;
        /// <summary>
        /// A radial gradient's focal point relative to its centre, and its focal radius, in the
        /// space GetFillTransform maps the gradient to (centre at 0.5, radius 0.5). The lengths are
        /// resolved as GetFillTransform resolves the centre and radius.
        /// </summary>
        internal static void GetRadialFocus(Rect bounds, Rect localBounds, bool objectBoundingBox, SVGLength cx, SVGLength cy, SVGLength r,
            SVGLength fx, SVGLength fy, SVGLength fr, out Vector2 focalOffset, out float focalRadius)
        {
            focalOffset = Vector2.zero;
            focalRadius = 0f;
            Vector2 centre, focus;
            float radius, focusRadius;
            if(objectBoundingBox && localBounds.width > 0f && localBounds.height > 0f)
            {
                centre = new Vector2(GetUnitValue(cx), GetUnitValue(cy));
                focus = new Vector2(GetUnitValue(fx), GetUnitValue(fy));
                radius = GetUnitValue(r);
                focusRadius = GetUnitValue(fr);
            } else {
                centre = GetGradientVector(cx, cy, bounds);
                focus = GetGradientVector(fx, fy, bounds);
                radius = GetGradientVector(r, r, bounds).x;
                if(r.unitType == SVGLengthType.Percentage) radius *= 0.5f;
                focusRadius = GetGradientVector(fr, fr, bounds).x;
                if(fr.unitType == SVGLengthType.Percentage) focusRadius *= 0.5f;
            }
            if(radius <= 0f) return;
            focalOffset = (focus - centre) / (radius * 2f);
            focalRadius = Mathf.Max(focusRadius, 0f) / (radius * 2f);
        }

        public static SVGMatrix GetFillTransform(SVGFill svgFill, Rect bounds, Rect localBounds, bool objectBoundingBox, SVGLength[] gradientStart, SVGLength[] gradientEnd, SVGMatrix fillTransform, SVGMatrix gradientTransform)
        {
            SVGMatrix transform = SVGMatrix.identity;

            // objectBoundingBox (the SVG default): gradient coordinates are fractions of the element's
            // bounding box, and gradientTransform applies inside that box. A box with no width or
            // height cannot be used, so such shapes fall back to user space.
            bool useBoundingBox = objectBoundingBox && localBounds.width > 0f && localBounds.height > 0f;
            SVGMatrix boundingBoxInverse = SVGMatrix.identity;
            if(useBoundingBox)
            {
                boundingBoxInverse = SVGMatrix.identity.Translate(localBounds.x, localBounds.y).Scale(localBounds.width, localBounds.height).Inverse();
            }

            SVGLength gradientStartX = gradientStart[0];
            SVGLength gradientStartY = gradientStart[1];

            SVGLength gradientEndX = gradientEnd[0];
            SVGLength gradientEndY = gradientEnd[1];

            Rect viewport = svgFill.viewport;
            //Debug.Log(viewport);

            if (svgFill.fillType == FILL_TYPE.GRADIENT)
            {
                switch (svgFill.gradientType)
                {
                    case GRADIENT_TYPE.LINEAR:
                    {
                        Vector2 startPoint, endPoint;
                        if(useBoundingBox)
                        {
                            startPoint = new Vector2(GetUnitValue(gradientStartX), GetUnitValue(gradientStartY));
                            endPoint = new Vector2(GetUnitValue(gradientEndX), GetUnitValue(gradientEndY));
                        } else {
                            startPoint = GetGradientVector(gradientStartX, gradientStartY, bounds);
                            endPoint = GetGradientVector(gradientEndX, gradientEndY, bounds);
                        }

                        Vector2 gradientVector = endPoint - startPoint;        
                        Vector2 normalizedVector = Vector2.zero;

                        float angle = Mathf.Atan2(gradientVector.y, gradientVector.x) * Mathf.Rad2Deg;
                        Vector2 posDiff = Vector2.Lerp(startPoint, endPoint, 0.5f);

                        float magnitude = gradientVector.magnitude;

                        if(magnitude != 0f)
                        {
                            normalizedVector.x = viewport.width / magnitude;
                            normalizedVector.y = viewport.height / magnitude;
                        }

                        transform = transform.Translate(viewport.center);
                        transform = transform.Scale(normalizedVector.x, normalizedVector.y);
                        transform = transform.Rotate(-angle);
                        transform = transform.Translate(-posDiff);

                        transform = transform.Multiply(gradientTransform.Inverse());
                        transform = transform.Multiply(boundingBoxInverse);
                        transform = transform.Multiply(fillTransform.Inverse());

                        break;
                    }
                    case GRADIENT_TYPE.RADIAL:
                    {
                        Vector2 point;
                        float radius;
                        if(useBoundingBox)
                        {
                            point = new Vector2(GetUnitValue(gradientStartX), GetUnitValue(gradientStartY));
                            radius = GetUnitValue(gradientEndX);
                        } else {
                            point = GetGradientVector(gradientStartX, gradientStartY, bounds);
                            radius = GetGradientVector(gradientEndX, gradientEndY, bounds).x;
                            if(gradientEndX.unitType == SVGLengthType.Percentage) radius *= 0.5f;
                        }

                        float radiusTimesTwo = radius * 2f;

                        Vector2 normalizedVector = Vector2.zero;

                        if(radiusTimesTwo != 0f)
                        {
                            normalizedVector.x = viewport.width / radiusTimesTwo;
                            normalizedVector.y = viewport.height / radiusTimesTwo;
                        }

                        transform = transform.Translate(viewport.center);
                        transform = transform.Scale(normalizedVector.x, normalizedVector.y);
                        transform = transform.Translate(-point);
                        
                        transform = transform.Multiply(gradientTransform.Inverse());
                        transform = transform.Multiply(boundingBoxInverse);
                        transform = transform.Multiply(fillTransform.Inverse());

                        break;
                    }
                    case GRADIENT_TYPE.CONICAL:
                    {
                        Vector2 point;
                        float radius;
                        if(useBoundingBox)
                        {
                            point = new Vector2(GetUnitValue(gradientStartX), GetUnitValue(gradientStartY));
                            radius = GetUnitValue(gradientEndX);
                        } else {
                            point = GetGradientVector(gradientStartX, gradientStartY, bounds);
                            radius = GetGradientVector(gradientEndX, gradientEndY, bounds).x;
                            if(gradientEndX.unitType == SVGLengthType.Percentage) radius *= 0.5f;
                        }

                        float radiusTimesTwo = radius * 2f;
                        
                        Vector2 normalizedVector = Vector2.zero;
                        
                        if(radiusTimesTwo != 0f)
                        {
                            normalizedVector.x = viewport.width / radiusTimesTwo;
                            normalizedVector.y = viewport.height / radiusTimesTwo;
                        }
                        
                        transform = transform.Translate(viewport.center);
                        transform = transform.Scale(normalizedVector.x, normalizedVector.y);
                        transform = transform.Translate(-point);
                        
                        transform = transform.Multiply(gradientTransform.Inverse());
                        transform = transform.Multiply(boundingBoxInverse);
                        transform = transform.Multiply(fillTransform.Inverse());
                        
                        break;
                    }
                }
            }
            
            return transform;
        }

        protected static void AddInputShape(List<List<Vector2>> inputShapes)
        {            
            if(inputShapes == null) return;
            for(int i = 0; i < inputShapes.Count; i++)
            {
                if(inputShapes[i] == null || inputShapes[i].Count == 0) continue;
                SVGGraphics.paths.Add(new SVGPath(inputShapes[i].ToArray()));
            }
            
        }
    }
}
