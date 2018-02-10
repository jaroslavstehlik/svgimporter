// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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

        public static Color GetStrokeColor(SVGPaintable paintable)
        {   
            Color color = paintable.strokeColor.Value.color;
            color.a *= paintable.strokeOpacity * paintable.opacity;
            paintable.svgFill = new SVGFill(color, FILL_BLEND.OPAQUE, FILL_TYPE.SOLID);
            if(color.a != 1f) paintable.svgFill.blend = FILL_BLEND.ALPHA_BLENDED;
            return color;
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
        /*
        public static Mesh CreateStrokeMesh(List<Vector2> inputShapes, SVGPaintable paintable, ClosePathRule closePath = ClosePathRule.NEVER)
        {
            if(inputShapes == null || inputShapes.Count == 0 || paintable == null || paintable.strokeWidth <= 0f)
                return null;

            return CreateStrokeMesh(new List<List<Vector2>>(){inputShapes}, paintable, closePath);
        }
        
        public static Mesh CreateStrokeMesh(List<List<Vector2>> inputShapes, SVGPaintable paintable, ClosePathRule closePath = ClosePathRule.NEVER)
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

            return SVGLineUtils.StrokeMesh(segments, paintable.strokeWidth, GetStrokeColor(paintable), GetStrokeLineJoin(paintable.strokeLineJoin), GetStrokeLineCap(paintable.strokeLineCap), paintable.miterLimit, paintable.dashArray, paintable.dashOffset, closePath, SVGGraphics.roundQuality);
        }
        */
        /*
        public static Mesh CreateStrokeSimple(SVGPaintable paintable, ClosePathRule closePath = ClosePathRule.NEVER)
        {
            return CreateStrokeSimple(new List<List<Vector2>>(){new List<Vector2>(SVGGraphics.position_buffer.ToArray())}, paintable, closePath);
        }
        */
        public static Mesh CreateStrokeSimple(List<List<Vector2>> inputShapes, SVGPaintable paintable, ClosePathRule closePath = ClosePathRule.NEVER)
        {
            if(inputShapes == null || inputShapes.Count == 0 || paintable == null || paintable.strokeWidth <= 0f)
                return null;

            AddInputShape(inputShapes);

            Color color = GetStrokeColor(paintable);            

            float strokeWidth = paintable.strokeWidth;
            if(inputShapes.Count > 1)
            {
                CombineInstance[] combineInstances = new CombineInstance[inputShapes.Count];
                for(int i = 0; i < inputShapes.Count; i++)
                {
                    combineInstances[i] = new CombineInstance();
                    SVGShape svgLayer = new SVGShape();
                    if(SVGMeshUtils.VectorLine(inputShapes[i].ToArray(), out svgLayer, color, color, strokeWidth, 0f, closePath))
                    {
                        Mesh localMesh = new Mesh();
                        int totalVertices = svgLayer.vertices.Length;
                        Vector3[] vertices = new Vector3[totalVertices];
                        for(int j = 0; j < totalVertices; j++)
                        {
                            vertices[j] = svgLayer.vertices[j];
                        }
                        localMesh.vertices = vertices;
                        localMesh.triangles = svgLayer.triangles;
                        localMesh.colors32 = svgLayer.colors;
                        combineInstances[i].mesh = localMesh;
                    }
                }

                Mesh mesh = new Mesh();
                mesh.CombineMeshes(combineInstances, true, false);
                return mesh;
            } else {
                SVGShape svgLayer = new SVGShape();
                if(SVGMeshUtils.VectorLine(inputShapes[0].ToArray(), out svgLayer, color, color, strokeWidth, 0f, closePath))
                {
                    Mesh localMesh = new Mesh();
                    int totalVertices = svgLayer.vertices.Length;
                    Vector3[] vertices = new Vector3[totalVertices];
                    for(int j = 0; j < totalVertices; j++)
                    {
                        vertices[j] = svgLayer.vertices[j];
                    }
                    localMesh.vertices = vertices;
                    localMesh.triangles = svgLayer.triangles;
                    localMesh.colors32 = svgLayer.colors;

                    return localMesh;
                }
                return null;
            }
        }

        public static bool CreateAntialiasing(List<List<Vector2>> inputShapes, out SVGShape svgShape, Color colorA, float width, ClosePathRule closePath = ClosePathRule.NEVER)
        {
            svgShape = new SVGShape();
            if(inputShapes == null || inputShapes.Count == 0) return false;

            Color colorB = new Color(colorA.r, colorA.g, colorA.b, 0f);
            if(inputShapes.Count > 1)
            {
                List<SVGShape> shapes = new List<SVGShape>();
                SVGShape currentLayer;
                for(int i = 0; i < inputShapes.Count; i++)
                {
                    if(SVGMeshUtils.VectorLine(inputShapes[i].ToArray(), out currentLayer, colorA, colorB, width, width * 0.5f, closePath))
                    {
                        shapes.Add(currentLayer);
                    }
                }
                if(shapes.Count > 0)
                {
                    svgShape = SVGShape.MergeShapes(shapes);
                    return true;
                } else {
                    return false;
                }
            } else {
                return SVGMeshUtils.VectorLine(inputShapes[0].ToArray(), out svgShape, colorA, colorB, width, width * 0.5f, closePath);                
            }
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
                        SVGLinearGradientBrush linearGradBrush = paintable.GetLinearGradientBrush(bounds, matrix, viewport);
                        paintable.svgFill = linearGradBrush.fill;
                    }
                        break;
                    case SVGPaintMethod.RadialGradientFill:
                    {
                        layer.type = SVGShapeType.FILL;
                        SVGRadialGradientBrush radialGradBrush = paintable.GetRadialGradientBrush(bounds, matrix, viewport);
                        paintable.svgFill = radialGradBrush.fill;
                    }
                        break;
                    case SVGPaintMethod.ConicalGradientFill:
                    {
                        layer.type = SVGShapeType.FILL;
                        SVGConicalGradientBrush conicalGradBrush = paintable.GetConicalGradientBrush(bounds, matrix, viewport);
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
            /*
            else {
                // Use Triangle.net library
                SVGImporter.TriangleNet.Mesh triangleMesh = new SVGImporter.TriangleNet.Mesh();
                SVGImporter.TriangleNet.Geometry.InputGeometry triangleInput = new SVGImporter.TriangleNet.Geometry.InputGeometry();

                int pathLength, m = 0;
                for(int i = 0; i < simplifiedShapes.Count; i++)
                {
                    if(simplifiedShapes[i] == null)
                        continue;
                    
                    pathLength = simplifiedShapes[i].Count;
                    Vector2 position;
                    for(int j = 0; j < pathLength; j++)
                    {
                        triangleInput.AddPoint(simplifiedShapes[i][j].x, simplifiedShapes[i][j].y);
                    }
                }

                triangleMesh.Triangulate(triangleInput);

                int totalVertices = triangleMesh.vertices.Count;
                layer.vertices = new Vector2[totalVertices];
                for(int i = 0; i < totalVertices; i++)
                {
                    layer.vertices[i].x = (float)triangleMesh.vertices[i].x * SVGAssetImport.meshScale;
                    layer.vertices[i].y = (float)triangleMesh.vertices[i].y * SVGAssetImport.meshScale;
                }

                int totalTriangles = triangleMesh.triangles.Count;
                layer.triangles = new int[totalTriangles * 3];
                int ti = 0;
                for(int i = 0; i < totalTriangles; i++)
                {
                    ti = i * 3;
                    layer.triangles[ti] = triangleMesh.triangles[i].P0;
                    layer.triangles[ti + 1] = triangleMesh.triangles[i].P1;
                    layer.triangles[ti + 2] = triangleMesh.triangles[i].P2;
                }
            }
            */

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
                if(CreateAntialiasing(simplifiedShapes, out antialiasingLayer, Color.white, -1f, ClosePathRule.ALWAYS))
                {
                    int verticesLength = antialiasingLayer.vertices.Length;
                    for(int i = 0; i < verticesLength; i++)
                    {
                        antialiasingLayer.vertices[i] *= SVGAssetImport.meshScale;
                    }
                    antialiasingLayer.type = SVGShapeType.ANTIALIASING;
                    antialiasingLayer.RecalculateBounds();
                    antialiasingLayer.fill = layer.fill.Clone();
                    antialiasingLayer.fill.blend = FILL_BLEND.ALPHA_BLENDED;
                }
            }

            return true;
        }

        protected static void WriteUVGradientIndexType(ref Vector2[] uv, int meshVertexCount, SVGPaintable svgPaintable)
        {
            SVGFill svgFill = svgPaintable.svgFill;
            if (svgFill.fillType == FILL_TYPE.GRADIENT && svgFill.gradientColors != null)
            {
                Vector2 gradientUV = new Vector2(svgFill.gradientColors.index, (int)svgFill.gradientType);
                uv = new Vector2[meshVertexCount];
                for (int i = 0; i < meshVertexCount; i++) {
                    uv [i].x = gradientUV.x;
                    uv [i].y = gradientUV.y;
                }
            }
        }
        /*
        public static Mesh CreateAntialiasing(List<List<Vector2>> paths, Color color, float antialiasingWidth, bool isStroke = false, ClosePathRule closePath = ClosePathRule.NEVER)
        {
            if(SVGAssetImport.antialiasingWidth <= 0f) return null;
            return SVGSimplePath.CreateAntialiasing(paths, color, antialiasingWidth, closePath);
        }
        */
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

        private static void OffsetPositions(Bounds bounds, List<Vector2> array)
        {
            if(array == null || array.Count == 0)
                return;

            int arrayLength = array.Count;
            Vector2 boundsCenter = bounds.center;
            for(int i = 0; i < arrayLength; i++)
            {
                array[i] -= boundsCenter;
            }

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
        public static SVGMatrix GetFillTransform(SVGFill svgFill, Rect bounds, SVGLength[] gradientStart, SVGLength[] gradientEnd, SVGMatrix fillTransform, SVGMatrix gradientTransform)
        {
            SVGMatrix transform = SVGMatrix.identity;

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
                        Vector2 startPoint = GetGradientVector(gradientStartX, gradientStartY, bounds);
                        Vector2 endPoint = GetGradientVector(gradientEndX, gradientEndY, bounds);

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
                        transform = transform.Multiply(fillTransform.Inverse());

                        break;
                    }
                    case GRADIENT_TYPE.RADIAL:
                    {
                        Vector2 point = GetGradientVector(gradientStartX, gradientStartY, bounds);
                        float radius = GetGradientVector(gradientEndX, gradientEndY, bounds).x;
                        if(gradientEndX.unitType == SVGLengthType.Percentage) radius *= 0.5f;

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
                        transform = transform.Multiply(fillTransform.Inverse());

                        break;
                    }
                    case GRADIENT_TYPE.CONICAL:
                    {
                        Vector2 point = GetGradientVector(gradientStartX, gradientStartY, bounds);
                        float radius = GetGradientVector(gradientEndX, gradientEndY, bounds).x;
                        if(gradientEndX.unitType == SVGLengthType.Percentage) radius *= 0.5f;

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
