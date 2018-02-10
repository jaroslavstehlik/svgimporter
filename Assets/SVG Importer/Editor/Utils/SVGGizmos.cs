// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Utils
{
    using Rendering;

    public class SVGGizmos {

        const float PI_2 = Mathf.PI * 2f;
        const float PI = Mathf.PI;
        const float PI_05 = Mathf.PI * 0.5f;
        const float PI_025 = Mathf.PI * 0.25f;

        public static void DrawGizmoLine(Vector2[] line, bool showIndexes = false)
        {
            if (line != null && line.Length <= 1)
                return;

            Vector3 lastPoint = new Vector3(line[0].x , line[0].y, 0f);
            Vector3 currentPoint;
            for(int i = 0; i < line.Length; i++)
            {
                currentPoint = new Vector3(line[i].x , line[i].y, 0f);

                if(i!= 0)
                {
                    Gizmos.DrawLine(lastPoint, currentPoint);
                }

                lastPoint = currentPoint;

                if(showIndexes)
                    Handles.Label(currentPoint, "   "+i.ToString());
            }
        }

        public static void DrawDebugLine(List<Vector2> line, bool showIndexes = false)
        {
            if (line != null && line.Count <= 1)
                return;
            
            Vector3 lastPoint = new Vector3(line[0].x , line[0].y, 0f);
            Vector3 currentPoint;
            for(int i = 0; i < line.Count; i++)
            {
                currentPoint = new Vector3(line[i].x , line[i].y, 0f);
                
                if(i!= 0)
                {
                    Debug.DrawLine(lastPoint, currentPoint);
                }
                
                lastPoint = currentPoint;
                
                if(showIndexes)
                    Handles.Label(currentPoint, "   "+i.ToString());
            }
        }

        public static void DottedLine(Vector2 start, Vector2 end, float size)
        {
            if(size == 0f)
                return;

            Vector2 direction = end - start;
    //        Vector2 directionNormalized = direction.normalized;
            float distance = direction.magnitude;

            float segmentsFloat = Mathf.Abs(distance / size);
            int segments = Mathf.Clamp(Mathf.CeilToInt(segmentsFloat), 1, int.MaxValue);
    //        int segmetsMinusOne = segments - 1;
            float progress; 
            float progressOffset = 1f - (1f - (segmentsFloat / (float)segments));

            if(segments == 1)
            {
                Handles.DrawLine(start, end);
            } else {
                Vector2 lastPoint = start, currentPoint;
                for(int i = 1; i <= segments; i++)
                {
                    progress = (float)(i) / ((float)segments * progressOffset);
                    currentPoint = Vector2.Lerp(start, end, progress);
                    if(i % 2 == 1)
                    {
                        Handles.DrawLine(currentPoint, lastPoint);
                    }                    
                    lastPoint = currentPoint;
                }
            }
        }

        public static void Line(Vector2[] line, bool showIndexes = false)
        {
            if (line != null && line.Length <= 1)
                return;
            
            Vector3 lastPoint = new Vector3(line[0].x , line[0].y, 0f);
            Vector3 currentPoint;
            for(int i = 0; i < line.Length; i++)
            {
                currentPoint = new Vector3(line[i].x , line[i].y, 0f);
                
                if(i!= 0)
                {
                    Handles.DrawLine(lastPoint, currentPoint);
                }
                
                lastPoint = currentPoint;
                
                if(showIndexes)
                    Handles.Label(currentPoint, "   "+i.ToString());
            }
        }

        public static void Line(List<Vector2> line, bool showIndexes = false)
        {
            if (line != null && line.Count <= 1)
                return;
            
            Vector3 lastPoint = new Vector3(line[0].x , line[0].y, 0f);
            Vector3 currentPoint;
            for(int i = 0; i < line.Count; i++)
            {
                currentPoint = new Vector3(line[i].x , line[i].y, 0f);
                
                if(i!= 0)
                {
                    Handles.DrawLine(lastPoint, currentPoint);
                }
                
                lastPoint = currentPoint;
                
                if(showIndexes)
                    Handles.Label(currentPoint, "   "+i.ToString());
            }
        }
        
        public static void Bezier(float precision, Vector3 start, Vector3 handle0, Vector3 handle1, Vector3 end, bool showIndexes = false)
        {
            Bezier(precision, new Vector2(start.x, start.y), new Vector2(handle0.x, handle0.y), new Vector2(handle1.x, handle1.y), new Vector2(end.x, end.y), showIndexes);
        }

        public static void Bezier(float precision, Vector2 start, Vector2 handle0, Vector2 handle1, Vector2 end, bool showIndexes = false)
        {
            List<Vector2> points;
            points = SVGBezier.AdaptiveCubicCurve(precision, start, handle0, handle1, end);

            int pointsCount = points.Count;

            if(pointsCount <= 1)
                return;

            Vector3 lastPoint = Vector2.zero, currentPoint = Vector2.zero;
            lastPoint = points[0];

            for(int i = 1; i < pointsCount; i++)
            {
                currentPoint.x = points[i].x;
                currentPoint.y = points[i].y;
                Handles.DrawLine(currentPoint, lastPoint);
                lastPoint = currentPoint;

                if(showIndexes)
                    Handles.Label(currentPoint, "   "+i.ToString());
            }
        }

        public static void Bounds(SVGBounds bounds)
        {
            Vector3 p0 = new Vector3(bounds.minX, bounds.minY, 0f);        
            Vector3 p1 = new Vector3(bounds.maxX, bounds.minY, 0f);
            Vector3 p2 = new Vector3(bounds.minX, bounds.maxY, 0f);
            Vector3 p3 = new Vector3(bounds.maxX, bounds.maxY, 0f);

            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p1, p3);
            Handles.DrawLine(p3, p2);
            Handles.DrawLine(p2, p0);
        }

		public static void Rectangle(Rect rect)
		{
			Vector3 p0 = new Vector3(rect.min.x, rect.min.y, 0f);        
			Vector3 p1 = new Vector3(rect.max.x, rect.min.y, 0f);
			Vector3 p2 = new Vector3(rect.min.x, rect.max.y, 0f);
			Vector3 p3 = new Vector3(rect.max.x, rect.max.y, 0f);
			
			Handles.DrawLine(p0, p1);
			Handles.DrawLine(p1, p3);
			Handles.DrawLine(p3, p2);
			Handles.DrawLine(p2, p0);
		}

        public static Rect GetScreenRect(Vector3 worldPosition, float pixelRadius)
        {
            Camera cam = Camera.current;
            if (cam == null)
                cam = Camera.main;

            float radiusHalf = pixelRadius * 0.5f;        
            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);
            return new Rect(screenPosition.x - radiusHalf, cam.pixelHeight - (screenPosition.y + radiusHalf), pixelRadius, pixelRadius);
        }

        public static bool MouseTestScreenRect(Vector3 worldPosition, float pixelRadius)
        {
            Camera cam = Camera.current;
            if (cam == null)
                cam = Camera.main;

            float radiusHalf = pixelRadius * 0.5f;
            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);
            return new Rect(screenPosition.x - radiusHalf, cam.pixelHeight - (screenPosition.y + radiusHalf), pixelRadius, pixelRadius).Contains(Event.current.mousePosition);
        }

        public static bool ShowCursor(Vector3 worldPosition, float pixelRadius, MouseCursor cursor)
        {
            Rect screenRect = GetScreenRect(worldPosition, pixelRadius);
            EditorGUIUtility.AddCursorRect(screenRect, cursor);
            return screenRect.Contains(Event.current.mousePosition);
        }

        public static bool ShowScaleCursor(Vector3 worldPosition, Vector3 orientation, float pixelRadius)
        {
            Rect screenRect = GetScreenRect(worldPosition, pixelRadius);
            orientation = orientation.normalized;
            float tempAngle = Mathf.Atan2(orientation.y, orientation.x);
            if(tempAngle != 0f)
                tempAngle = Mathf.Round(tempAngle / PI_025) * PI_025;
            
            int angle = Mathf.RoundToInt(tempAngle * Mathf.Rad2Deg);        
            if (angle == 0 || angle == 180 || angle == -180)
            {
                EditorGUIUtility.AddCursorRect(screenRect, MouseCursor.ResizeHorizontal);
            } else if (angle == 90 || angle == -90)
            {
                EditorGUIUtility.AddCursorRect(screenRect, MouseCursor.ResizeVertical);
            } else if (angle == 45 || angle == -135)
            {
                EditorGUIUtility.AddCursorRect(screenRect, MouseCursor.ResizeUpRight);
            } else if(angle == 135 || angle == -45)
            {
                EditorGUIUtility.AddCursorRect(screenRect, MouseCursor.ResizeUpLeft);
            }
            return screenRect.Contains(Event.current.mousePosition);
        }
    }
}
