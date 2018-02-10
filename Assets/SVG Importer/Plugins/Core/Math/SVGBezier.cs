// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Rendering
{
    using Utils;

    public class SVGBezier
    {
        const int maxAdaptiveBezierIteration = 200;
        static int currentAdaptiveBezierIteration;

        const float PI = Mathf.PI;
        const float PI2 = Mathf.PI * 2f;

    //    static int adaptiveIteration;

        public static Vector3[] QuadraticBezierCurve(int segments, Vector3 start, Vector3 handle, Vector3 end)
        {
            Vector3[] output = new Vector3[segments];
            float t, invT, invTxInvT, invTxTx2, tXt;
            float segmentsf = (float)segments - 1f;
    		
            for (int i = 0; i < segments; i++)
            {
                t = (float)i / segmentsf;
                invT = 1f - t;
    			
                invTxInvT = invT * invT;
                invTxTx2 = invT * t * 2f;
                tXt = t * t;

                output [i].x = invTxInvT * start.x + invTxTx2 * handle.x + tXt * end.x;
                output [i].y = invTxInvT * start.y + invTxTx2 * handle.y + tXt * end.y;
                output [i].z = invTxInvT * start.z + invTxTx2 * handle.z + tXt * end.z;
            }
    		
            return output;
        }
    	
        public static Vector2[] QuadraticBezierCurve(int segments, Vector2 start, Vector2 handle, Vector2 end)
        {
            Vector2[] output = new Vector2[segments];
            float t, invT, invTxInvT, invTxTx2, tXt;
            float segmentsf = (float)segments - 1f;
    		
            for (int i = 0; i < segments; i++)
            {
                t = (float)i / segmentsf;
                invT = 1f - t;
    			
                invTxInvT = invT * invT;
                invTxTx2 = invT * t * 2f;
                tXt = t * t;

                output [i].x = invTxInvT * start.x + invTxTx2 * handle.x + tXt * end.x;
                output [i].y = invTxInvT * start.y + invTxTx2 * handle.y + tXt * end.y;
            }
    		
            return output;
        }

        public static Vector3[] CubicBezierCurve(int segments, Vector3 start, Vector3 handle0, Vector3 handle1, Vector3 end)
        {
            Vector3[] output = new Vector3[segments];
			float t, tXt, tXtXt, tx3, txTx3, invT, invTxInvT, invTxInvTxInvT;//, invTxTx2;
            float segmentsf = (float)segments - 1f;
    		
            for (int i = 0; i < segments; i++)
            {
                t = (float)i / segmentsf;
                tXt = t * t;
                tXtXt = tXt * t;
    			
                tx3 = t * 3f;
                invT = 1f - t;
                txTx3 = tXt * 3f;
    			
                invTxInvT = invT * invT;
                invTxInvTxInvT = invTxInvT * invT;			
                //invTxTx2 = invT * t * 2f;
    			
                output [i].x = invTxInvTxInvT * start.x + tx3 * invTxInvT * handle0.x + txTx3 * invT * handle1.x + tXtXt * end.x;
                output [i].y = invTxInvTxInvT * start.y + tx3 * invTxInvT * handle0.y + txTx3 * invT * handle1.y + tXtXt * end.y;
                output [i].z = invTxInvTxInvT * start.z + tx3 * invTxInvT * handle0.z + txTx3 * invT * handle1.z + tXtXt * end.z;
            }
    		
            return output;
        }

        public static float ClosestPointOnCubicBezierCurve(int segments, Vector3 start, Vector3 handle0, Vector3 handle1, Vector3 end, Vector3 point, out Vector3 pointOnLine)
        {
			float t, tXt, tXtXt, tx3, txTx3, invT, invTxInvT, invTxInvTxInvT;//, invTxTx2;
            float segmentsf = (float)segments - 1f;

            Vector3 currentPoint = Vector3.zero, lastPoint = Vector3.zero;
            Vector3 tempPoint;
            float minDistance = float.MaxValue;
            float tempDistance;

            pointOnLine = Vector3.zero;

            for (int i = 0; i < segments; i++)
            {
                t = (float)i / segmentsf;
                tXt = t * t;
                tXtXt = tXt * t;
                
                tx3 = t * 3f;
                invT = 1f - t;
                txTx3 = tXt * 3f;
                
                invTxInvT = invT * invT;
                invTxInvTxInvT = invTxInvT * invT;          
                //invTxTx2 = invT * t * 2f;

                currentPoint.x = invTxInvTxInvT * start.x + tx3 * invTxInvT * handle0.x + txTx3 * invT * handle1.x + tXtXt * end.x;
                currentPoint.y = invTxInvTxInvT * start.y + tx3 * invTxInvT * handle0.y + txTx3 * invT * handle1.y + tXtXt * end.y;
                currentPoint.z = invTxInvTxInvT * start.z + tx3 * invTxInvT * handle0.z + txTx3 * invT * handle1.z + tXtXt * end.z;

                if(i != 0)
                {
                    tempDistance = ClosestPointToLine(lastPoint, currentPoint, point, out tempPoint);
                    if(tempDistance < minDistance)
                    {
                        minDistance = tempDistance;
                        pointOnLine = tempPoint;
                    }
                }

                lastPoint = currentPoint;
            }
            
            return minDistance;
        }
        
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

    	
        public static Vector2[] CubicBezierCurve(int segments, Vector2 start, Vector2 handle0, Vector2 handle1, Vector2 end)
        {
            Vector2[] output = new Vector2[segments];
			float t, tXt, tXtXt, tx3, txTx3, invT, invTxInvT, invTxInvTxInvT;//, invTxTx2;
            float segmentsf = (float)segments - 1f;
    		
            for (int i = 0; i < segments; i++)
            {
                t = (float)i / segmentsf;
                tXt = t * t;
                tXtXt = tXt * t;
    			
                tx3 = t * 3f;
                invT = 1f - t;
                txTx3 = tXt * 3f;
    			
                invTxInvT = invT * invT;
                invTxInvTxInvT = invTxInvT * invT;			
                //invTxTx2 = invT * t * 2f;
    			
                output [i].x = invTxInvTxInvT * start.x + tx3 * invTxInvT * handle0.x + txTx3 * invT * handle1.x + tXtXt * end.x;
                output [i].y = invTxInvTxInvT * start.y + tx3 * invTxInvT * handle0.y + txTx3 * invT * handle1.y + tXtXt * end.y;
            }
    		
            return output;
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
        
        public static List<int> RamerDouglasPeuckerIndex(Vector2[] pointArray, float precision, int startPosition = 0, int endPosition = -1) 
        {
            float dmax = 0.0f;
            int index = -1;
            
            List<int> resultList = new List<int>();
            
            if (endPosition < 0) {
                endPosition = pointArray.Length-1;
            }
            
            for (int i = startPosition; i < endPosition; i++) {
                float d = PerpendicularDistance(pointArray[i], pointArray[startPosition], pointArray[endPosition]);
                if (d > dmax) {
                    index = i;
                    dmax = d;
                }
            }
            
            if (dmax >= precision) {
                List<int> result2 = RamerDouglasPeuckerIndex(pointArray, precision, index, endPosition);
                resultList = RamerDouglasPeuckerIndex(pointArray, precision, startPosition, index);
                
                result2.RemoveAt(0);
                resultList.AddRange(result2);
            } else {
                resultList.Add(startPosition);
                resultList.Add(endPosition);
            }
            
            return resultList;
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

		private static float PointDistance(Vector2 point1, Vector2 point2) {
			float dx = point1.x - point2.x;
			float dy = point1.y - point2.y;
			return (float)Mathf.Sqrt( dx*dx + dy*dy );
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
    	


        public static float CurveLength(Vector3[] points)
        {
            float length = 0f;
    		
            if (points == null || points.Length <= 1)
                return 0f;
    		
            Vector3 lastPoint = points [0];
            Vector3 diff;
            for (int i = 1; i < points.Length; i++)
            {
    			
                diff.x = points [i].x - lastPoint.x;
                diff.y = points [i].y - lastPoint.y;
                diff.z = points [i].z - lastPoint.z;
    			
                length += Mathf.Sqrt(diff.x * diff.x + diff.y * diff.y + diff.z * diff.z);
    			
                lastPoint.x = points [i].x;
                lastPoint.y = points [i].y;
                lastPoint.z = points [i].z;
            }
    		
            return length;
        }
    	
        public static float CurveLength(Vector2[] points)
        {
            float length = 0f;
    		
            if (points == null || points.Length <= 1)
                return 0f;
    		
            Vector2 lastPoint = points [0];
            Vector2 diff;
            for (int i = 1; i < points.Length; i++)
            {
    			
                diff.x = points [i].x - lastPoint.x;
                diff.y = points [i].y - lastPoint.y;
    			
                length += Mathf.Sqrt(diff.x * diff.x + diff.y * diff.y);
    			
                lastPoint.x = points [i].x;
                lastPoint.y = points [i].y;
            }
    		
            return length;
        }

        public static SVGBounds GetLooseBounds(Vector2 start, Vector2 handle0, Vector2 handle1, Vector2 end)
        {
            SVGBounds output = SVGBounds.InfiniteInverse;
            output.Encapsulate(start);
            output.Encapsulate(handle0);
            output.Encapsulate(handle1);
            output.Encapsulate(end);
            return output;
        }

        public static SVGBounds GetLooseBounds(Vector2 start, Vector2 handle, Vector2 end)
        {
            SVGBounds output = SVGBounds.InfiniteInverse;
            output.Encapsulate(start);
            output.Encapsulate(handle);
            output.Encapsulate(end);
            return output;
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

        public static List<Vector2> AdaptiveCubicCurve(float distanceTolerance, Vector2 start, Vector2 handle0, Vector2 handle1, Vector2 end)
        {
            currentAdaptiveBezierIteration = 0;
            if(start == handle0 && handle0 == handle1 && handle1 == end)
                return new List<Vector2>();

            if (distanceTolerance < 0.01f)
                distanceTolerance = 0.01f;

            List<Vector2> points = new List<Vector2>(){start};

    //        adaptiveIteration = 0;
            RecursiveBezier(points, distanceTolerance * distanceTolerance, 
                            start.x, start.y,
                            handle0.x, handle0.y,
                            handle1.x, handle1.y,
                            end.x, end.y);

            points.Add(end);
            return points;
        }

        static void RecursiveBezier(List<Vector2> points, float distanceTolerance, float x1, float y1, 
                                    float x2, float y2, 
                                    float x3, float y3, 
                                    float x4, float y4)
        {
            if(currentAdaptiveBezierIteration++ >= maxAdaptiveBezierIteration)
                return;

            // Calculate all the mid-points of the line segments
            //----------------------
            float x12 = (x1 + x2) * 0.5f;
            float y12 = (y1 + y2) * 0.5f;
            float x23 = (x2 + x3) * 0.5f;
            float y23 = (y2 + y3) * 0.5f;
            float x34 = (x3 + x4) * 0.5f;
            float y34 = (y3 + y4) * 0.5f;
            float x123 = (x12 + x23) * 0.5f;
            float y123 = (y12 + y23) * 0.5f;
            float x234 = (x23 + x34) * 0.5f;
            float y234 = (y23 + y34) * 0.5f;
            float x1234 = (x123 + x234) * 0.5f;
            float y1234 = (y123 + y234) * 0.5f;
            
            // Try to approximate the full cubic curve by a single straight line
            //------------------
            float dx = x4 - x1;
            float dy = y4 - y1;
            
            float d2 = Mathf.Abs(((x2 - x4) * dy - (y2 - y4) * dx));
            float d3 = Mathf.Abs(((x3 - x4) * dy - (y3 - y4) * dx));
            
            if ((d2 + d3) * (d2 + d3) < distanceTolerance * (dx * dx + dy * dy))
            {
                points.Add(new Vector2(x1234, y1234));
                return;
            }

            // Continue subdivision
            //----------------------
            RecursiveBezier(points, distanceTolerance, x1, y1, x12, y12, x123, y123, x1234, y1234); 
            RecursiveBezier(points, distanceTolerance, x1234, y1234, x234, y234, x34, y34, x4, y4); 
        }

        public static Vector2[] GetExtremePoints(Vector2 start, Vector2 handle0, Vector2 handle1, Vector2 end)
        {
            List<float> extremes = GetExtremes(start, handle0, handle1, end);
            int extremesCount = extremes.Count;
            Vector2[] points = new Vector2[extremesCount];

            float mt, mtmtmt, mtmt, t, ttt, tt;
            for (int i = 0; i < extremesCount; i++)
            {
                t = extremes [i];
                mt = 1f - t;
                
                tt = t * t;
                ttt = tt * t;
                
                mtmt = mt * mt;
                mtmtmt = mtmt * mt;
                
                points [i].x = (mtmtmt * start.x) + (3f * mtmt * t * handle0.x) + (3f * mt * tt * handle1.x) + (ttt * end.x);
                points [i].y = (mtmtmt * start.y) + (3f * mtmt * t * handle0.y) + (3f * mt * tt * handle1.y) + (ttt * end.y);
            }

            return points;
        }

        public static float GetApproxLength(Vector2 start, Vector2 handle0, Vector2 handle1, Vector2 end)
        {
            List<float> interval = new List<float>{0f, 1f};
            interval.InsertRange(1, GetExtremes(start, handle0, handle1, end));
            int intervalCount = interval.Count;
            Vector2 currentPoint, lastPoint;

            float mt, mtmtmt, mtmt, t, ttt, tt;
            float distance = 0f;

            t = interval [0];
            mt = 1f - t;
            
            tt = t * t;
            ttt = tt * t;
            
            mtmt = mt * mt;
            mtmtmt = mtmt * mt;
            
            lastPoint.x = (mtmtmt * start.x) + (3f * mtmt * t * handle0.x) + (3f * mt * tt * handle1.x) + (ttt * end.x);
            lastPoint.y = (mtmtmt * start.y) + (3f * mtmt * t * handle0.y) + (3f * mt * tt * handle1.y) + (ttt * end.y);

            for (int i = 1; i < intervalCount; i++)
            {
                t = interval [i];
                mt = 1f - t;
                
                tt = t * t;
                ttt = tt * t;
                
                mtmt = mt * mt;
                mtmtmt = mtmt * mt;
                
                currentPoint.x = (mtmtmt * start.x) + (3f * mtmt * t * handle0.x) + (3f * mt * tt * handle1.x) + (ttt * end.x);
                currentPoint.y = (mtmtmt * start.y) + (3f * mtmt * t * handle0.y) + (3f * mt * tt * handle1.y) + (ttt * end.y);

                distance += Vector2.Distance(currentPoint, lastPoint);

                currentPoint = lastPoint;
            }

            return distance;
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

        public static Vector3[] PathControlPointGenerator(Vector3[] path)
        {
            Vector3[] suppliedPath;
            Vector3[] vector3s;
            
            //create and store path points:
            suppliedPath = path;
            
            //populate calculate path;
            int offset = 2;
            vector3s = new Vector3[suppliedPath.Length + offset];
            System.Array.Copy(suppliedPath, 0, vector3s, 1, suppliedPath.Length);
            
            //populate start and end control points:
            //vector3s[0] = vector3s[1] - vector3s[2];
            vector3s [0] = vector3s [1] + (vector3s [1] - vector3s [2]);
            vector3s [vector3s.Length - 1] = vector3s [vector3s.Length - 2] + (vector3s [vector3s.Length - 2] - vector3s [vector3s.Length - 3]);
            
            //is this a closed, continuous loop? yes? well then so let's make a continuous Catmull-Rom spline!
            if (vector3s [1] == vector3s [vector3s.Length - 2])
            {
                Vector3[] tmpLoopSpline = new Vector3[vector3s.Length];
                System.Array.Copy(vector3s, tmpLoopSpline, vector3s.Length);
                tmpLoopSpline [0] = tmpLoopSpline [tmpLoopSpline.Length - 3];
                tmpLoopSpline [tmpLoopSpline.Length - 1] = tmpLoopSpline [2];
                vector3s = new Vector3[tmpLoopSpline.Length];
                System.Array.Copy(tmpLoopSpline, vector3s, tmpLoopSpline.Length);
            }        
            return(vector3s);
        }

        public static Vector2[] PathControlPointGenerator(Vector2[] path)
        {
            Vector2[] suppliedPath;
            Vector2[] vector2s;
            
            //create and store path points:
            suppliedPath = path;
            
            //populate calculate path;
            int offset = 2;
            vector2s = new Vector2[suppliedPath.Length + offset];
            System.Array.Copy(suppliedPath, 0, vector2s, 1, suppliedPath.Length);
            
            //populate start and end control points:
            //vector2s[0] = vector2s[1] - vector2s[2];
            vector2s [0] = vector2s [1] + (vector2s [1] - vector2s [2]);
            vector2s [vector2s.Length - 1] = vector2s [vector2s.Length - 2] + (vector2s [vector2s.Length - 2] - vector2s [vector2s.Length - 3]);
            
            //is this a closed, continuous loop? yes? well then so let's make a continuous Catmull-Rom spline!
            if (vector2s [1] == vector2s [vector2s.Length - 2])
            {
                Vector2[] tmpLoopSpline = new Vector2[vector2s.Length];
                System.Array.Copy(vector2s, tmpLoopSpline, vector2s.Length);
                tmpLoopSpline [0] = tmpLoopSpline [tmpLoopSpline.Length - 3];
                tmpLoopSpline [tmpLoopSpline.Length - 1] = tmpLoopSpline [2];
                vector2s = new Vector2[tmpLoopSpline.Length];
                System.Array.Copy(tmpLoopSpline, vector2s, tmpLoopSpline.Length);
            }        
            return(vector2s);
        }

        public static Vector3 Interpolate(Vector3[] points, float t)
        {
            float numSections = (float)(points.Length - 3);
            int currPt = (int)Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);
            float u = t * numSections - (float)currPt;
            
            Vector3 a = points [currPt];
            Vector3 b = points [currPt + 1];
            Vector3 c = points [currPt + 2];
            Vector3 d = points [currPt + 3];
            
            return .5f * (
                (-a + 3f * b - 3f * c + d) * (u * u * u)
                + (2f * a - 5f * b + 4f * c - d) * (u * u)
                + (-a + c) * u
                + 2f * b
                );
        }

        public static Vector2 Interpolate(Vector2[] points, float t)
        {
            float numSections = (float)(points.Length - 3);
            int currPt = (int)Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);
            float u = t * numSections - (float)currPt;
            
            Vector3 a = points [currPt];
            Vector3 b = points [currPt + 1];
            Vector3 c = points [currPt + 2];
            Vector3 d = points [currPt + 3];
            
            return .5f * (
                (-a + 3f * b - 3f * c + d) * (u * u * u)
                + (2f * a - 5f * b + 4f * c - d) * (u * u)
                + (-a + c) * u
                + 2f * b
                );
        }
    }
}
