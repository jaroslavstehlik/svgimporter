

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Rendering
{
    using Utils;

    internal class SVGBezier
    {
        const int maxAdaptiveBezierIteration = 200;
        static int currentAdaptiveBezierIteration;

        const float PI = Mathf.PI;
        const float PI2 = Mathf.PI * 2f;

    //    static int adaptiveIteration;
        
        public static float ClosestPointToLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point, out Vector3 pointOnLine)
        {
            Vector3 point2 = new Vector3(lineEnd.x - lineStart.x, lineEnd.y - lineStart.y, lineEnd.z - lineStart.z);
            float dot = point2.x * point2.x + point2.y * point2.y + point2.z * point2.z;
            float magnitude = ((point.x - lineStart.x) * point2.x + (point.y - lineStart.y) * point2.y+ (point.z - lineStart.z) * point2.z);
            if(dot != 0f)
                magnitude = magnitude / dot;
            
            if (magnitude > 1)
                magnitude = 1;
            else if (magnitude < 0)
                magnitude = 0;
            
            pointOnLine.x = lineStart.x + magnitude * point2.x;
            pointOnLine.y = lineStart.y + magnitude * point2.y;
            pointOnLine.z = lineStart.z + magnitude * point2.z;
            
            float dx = pointOnLine.x - point.x;
            float dy = pointOnLine.y - point.y;
            float dz = pointOnLine.z - point.z;
            
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

    	
		public static List<Vector2> Optimise(List<Vector2> pointList, float precision, int startPosition = 0, int endPosition = -1) 
		{
			precision *= 0.1f;
			return RamerDouglasPeucker(pointList, precision, startPosition, endPosition);
		}

        public static Vector2[] Optimise(Vector2[] pointList, float precision, int startPosition = 0, int endPosition = -1) 
        {
            precision *= 0.1f;
            return RamerDouglasPeucker(new List<Vector2>(pointList), precision, startPosition, endPosition).ToArray();
        }

		protected static List<Vector2> RamerDouglasPeucker(List<Vector2> pointList, float precision, int startPosition = 0, int endPosition = -1) 
		{
			float dmax = 0.0f;
			int index = -1;
			
			List<Vector2> resultList = new List<Vector2>();
			
			if (endPosition < 0) {
				endPosition = pointList.Count-1;
			}
			
			for (int i = startPosition; i < endPosition; i++) {
				float d = PerpendicularDistance(pointList[i], pointList[startPosition], pointList[endPosition]);
				if (d > dmax) {
					index = i;
					dmax = d;
				}
			}
			
			if (dmax >= precision) {
				List<Vector2> result2 = RamerDouglasPeucker(pointList, precision, index, endPosition);
				resultList = RamerDouglasPeucker(pointList, precision, startPosition, index);
				
				result2.RemoveAt(0);
				resultList.AddRange(result2);
			} else {
				resultList.Add(pointList[startPosition]);
				resultList.Add(pointList[endPosition]);
			}
			
			return resultList;
		}

		private static float AngularCoefficient(Vector2 point1, Vector2 point2) {
			float yDiff = (point1.y - point2.y);
			float xDiff = (point1.x - point2.x);
			if (xDiff == 0.0f) xDiff = 0.0001f;
			
			return (yDiff/xDiff);
		}

		private static float YIntercept(Vector2 point, float angularCoef) {
			return (point.y - (angularCoef*point.x));
		}

        private static float PerpendicularDistance(Vector2 point, Vector2 lineStartPoint, Vector2 lineEndPoint) {
            
            float d = 0.0f;
            float denom = 0.0001f;
            
            if (lineStartPoint.x == lineEndPoint.x) {
                return (Mathf.Abs(point.x - lineStartPoint.x));
            } else {
                float angCoef = AngularCoefficient(lineStartPoint, lineEndPoint);
                float yInter = YIntercept(lineStartPoint, angCoef);
                
                d = Mathf.Abs((angCoef*point.x)-point.y+yInter);
				denom = (float)Mathf.Sqrt((angCoef*angCoef)+1);
			}
			
			return (d/denom);
		}
    	


        public static List<float> GetExtremes(Vector2 start, Vector2 handle0, Vector2 handle1, Vector2 end)
        {
            List<float> tvalues = new List<float>();
            float a, b, c, t, t1, t2, b2ac, sqrtb2ac;
            for (int i = 0; i < 2; ++i)
            {
                if (i == 0)
                {
                    b = 6f * start.x - 12f * handle0.x + 6f * handle1.x;
                    a = -3f * start.x + 9f * handle0.x - 9f * handle1.x + 3f * end.x;
                    c = 3f * handle0.x - 3f * start.x;
                } else
                {
                    b = 6f * start.y - 12f * handle0.y + 6f * handle1.y;
                    a = -3f * start.y + 9f * handle0.y - 9f * handle1.y + 3f * end.y;
                    c = 3f * handle0.y - 3f * start.y;
                }
                
                if (Mathf.Abs(a) < 1e-12) // Numerical robustness
                {
                    if (Mathf.Abs(b) < 1e-12) // Numerical robustness
                    {
                        continue;
                    }
                    t = -c / b;
                    if (0f < t && t < 1f)
                    {
                        tvalues.Add(t);
                    }
                    continue;
                }
                
                b2ac = b * b - 4f * c * a;
                sqrtb2ac = Mathf.Sqrt(b2ac);
                if (b2ac < 0f)
                {
                    continue;
                }
                t1 = (-b + sqrtb2ac) / (2f * a);
                if (0f < t1 && t1 < 1f)
                {
                    tvalues.Add(t1);
                }
                t2 = (-b - sqrtb2ac) / (2f * a);
                if (0f < t2 && t2 < 1f)
                {
                    tvalues.Add(t2);
                }
            }

            tvalues.Sort();
            return tvalues;
        }

        public static SVGBounds GetBounds(Vector2 start, Vector2 handle0, Vector2 handle1, Vector2 end)
        {
            float minX = Mathf.Min(start.x, end.x);
            float maxX = Mathf.Max(start.x, end.x);
            float minY = Mathf.Min(start.y, end.y);
            float maxY = Mathf.Max(start.y, end.y);
            List<float> extremes = GetExtremes(start, handle0, handle1, end);
            int extremesCount = extremes.Count;

            float x, y, mt, mtmtmt, mtmt, t, ttt, tt;
            for (int i = 0; i < extremesCount; i++)
            {
                t = extremes [i];
                mt = 1f - t;
                
                tt = t * t;
                ttt = tt * t;
                
                mtmt = mt * mt;
                mtmtmt = mtmt * mt;
                
                x = (mtmtmt * start.x) + (3f * mtmt * t * handle0.x) + (3f * mt * tt * handle1.x) + (ttt * end.x);
                y = (mtmtmt * start.y) + (3f * mtmt * t * handle0.y) + (3f * mt * tt * handle1.y) + (ttt * end.y);

                if (x < minX)
                    minX = x;
                if (x > maxX)
                    maxX = x;
                if (y < minY)
                    minY = y;
                if (y > maxY)
                    maxY = y;
            }

            return new SVGBounds(minX, minY, maxX, maxY);
        }
    }
}
