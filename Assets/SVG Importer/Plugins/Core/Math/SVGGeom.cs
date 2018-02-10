// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Utils
{
    using Rendering;
    using ClipperLib;

    public class SVGGeom
    {
        const int decimalPointInt = 1000;
        //const int decimalPointInt = 1000;
        const float decimalPointFloat = 1f / (float)decimalPointInt;

        public static List<IntPoint> ConvertFloatToInt(List<Vector2> polygon)
        {
            int polygonCount = polygon.Count, i;
            if(polygonCount > 1)
            {
                if(polygon[0] == polygon[polygon.Count - 1]) polygonCount--;
            }

            List<IntPoint> polygonInt = new List<IntPoint>(polygonCount);
            for (i = 0; i < polygonCount; i++)
            {
                polygonInt.Add(new IntPoint((int)(polygon [i].x * decimalPointInt), (int)(polygon [i].y * decimalPointInt)));
            }
            return polygonInt;
        }

        public static List<Vector2> ConvertIntToFloat(List<IntPoint> polygonInt)
        {
            int polygonCount = polygonInt.Count, i;
            if(polygonCount > 1)
            {
                if(polygonInt[0] == polygonInt[polygonInt.Count - 1]) polygonCount--;
            }

            List<Vector2> polygon = new List<Vector2>(polygonCount);
            for (i = 0; i < polygonCount; i++)
            {
                polygon.Add(new Vector2((float)polygonInt [i].X * decimalPointFloat, (float)polygonInt [i].Y * decimalPointFloat));
            }        

            return polygon;
        }

    	public static List<List<Vector2>> SimplifyPolygon(List<Vector2> polygon, PolyFillType polyFillType = PolyFillType.pftNonZero)
        {
            if (polygon == null || polygon.Count == 0)
                return null;

            List<IntPoint> polygonInt = ConvertFloatToInt(polygon);

    		List<List<IntPoint>> polygonsInt = Clipper.SimplifyPolygon(polygonInt, polyFillType);
			int polygonsIntCount = polygonsInt.Count, i;//, j, polygonCount;
            List<List<Vector2>> polygons = new List<List<Vector2>>(polygonsIntCount);
            for (i = 0; i < polygonsIntCount; i++)
            {
                //polygonCount = polygonsInt [i].Count;
                polygons.Add(ConvertIntToFloat(polygonsInt [i]));
            }

            if (polygons == null || polygons.Count == 0)
                return null;

            return polygons;
        }

        public static List<List<Vector2>> SimplifyPolygons(List<List<Vector2>> polygon, PolyFillType polyFillType = PolyFillType.pftNonZero)
        {
            if (polygon == null || polygon.Count == 0)
                return null;

            List<List<IntPoint>> polygonsInt = new List<List<IntPoint>>();
            int i;//, j, polygonCount;
            for(i = 0; i < polygon.Count; i++)
            {
                polygonsInt.Add(ConvertFloatToInt(polygon[i]));
            }

            polygonsInt = Clipper.SimplifyPolygons(polygonsInt, polyFillType);
            int polygonsIntCount = polygonsInt.Count;
            List<List<Vector2>> polygons = new List<List<Vector2>>(polygonsIntCount);
            for (i = 0; i < polygonsIntCount; i++)
            {
                polygons.Add(ConvertIntToFloat(polygonsInt [i]));
            }
            
            if (polygons == null || polygons.Count == 0)
                return null;
            
            return polygons;
        }

        public static List<List<Vector2>> MergePolygon(List<List<Vector2>> polygon)
        {
            if (polygon == null || polygon.Count == 0)
                return null;

            List<List<IntPoint>> solution = new List<List<IntPoint>>(){ConvertFloatToInt(polygon [0])};
            for (int i = 1; i < polygon.Count; i++)
            {
                solution = MergePolygon(solution, ConvertFloatToInt(polygon [i]));                
            }

            List<List<Vector2>> output = new List<List<Vector2>>();
            for(int i = 0; i < solution.Count; i++)
            {
                output.Add(ConvertIntToFloat(solution[i]));
            }

            return output;
        }

        public static List<List<IntPoint>> MergePolygon(List<List<IntPoint>> polygonA, List<IntPoint> polygonB)
        {
            Clipper clipper = new Clipper();
            clipper.AddPaths(polygonA, PolyType.ptSubject, true);
            clipper.AddPath(polygonB, PolyType.ptClip, true);            
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctUnion, solution);
            return solution;
        }

        public static List<List<Vector2>> ClipPolygon(List<List<Vector2>> polygon, List<List<Vector2>> clipPath)
        {
            if (polygon == null || polygon.Count == 0) return null;
            if(clipPath == null || clipPath.Count == 0) return polygon;

            List<List<IntPoint>> polygonsInt = new List<List<IntPoint>>();
            List<List<IntPoint>> clipPathInt = new List<List<IntPoint>>();
            int i;//, j, polygonCount;
            for(i = 0; i < polygon.Count; i++)
            {
                polygonsInt.Add(ConvertFloatToInt(polygon[i]));
            }
            for(i = 0; i < clipPath.Count; i++)
            {
                clipPathInt.Add(ConvertFloatToInt(clipPath[i]));
            }
            
            polygonsInt = ClipPolygon(polygonsInt, clipPathInt);
            int polygonsIntCount = polygonsInt.Count;
            List<List<Vector2>> polygons = new List<List<Vector2>>(polygonsIntCount);
            for (i = 0; i < polygonsIntCount; i++)
            {
                polygons.Add(ConvertIntToFloat(polygonsInt [i]));
            }
            
            if (polygons == null || polygons.Count == 0)
                return null;
            
            return polygons;
        }

        public static List<List<IntPoint>> ClipPolygon(List<IntPoint> polygon, List<IntPoint> clipPath)
        {
            Clipper clipper = new Clipper();
            clipper.AddPath(polygon, PolyType.ptSubject, true);
            clipper.AddPath(clipPath, PolyType.ptClip, true);            
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctIntersection, solution);
            return solution;
        }
        
        public static List<List<IntPoint>> ClipPolygon(List<List<IntPoint>> polygons, List<IntPoint> clipPath)
        {
            Clipper clipper = new Clipper();
            clipper.AddPaths(polygons, PolyType.ptSubject, true);
            clipper.AddPath(clipPath, PolyType.ptClip, true);            
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctIntersection, solution);
            return solution;
        } 

        public static List<List<IntPoint>> ClipPolygon(List<List<IntPoint>> polygons, List<List<IntPoint>> clipPaths)
        {
            Clipper clipper = new Clipper();
            clipper.AddPaths(polygons, PolyType.ptSubject, true);
            clipper.AddPaths(clipPaths, PolyType.ptClip, true);            
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctIntersection, solution);
            return solution;
        } 
    }
}
