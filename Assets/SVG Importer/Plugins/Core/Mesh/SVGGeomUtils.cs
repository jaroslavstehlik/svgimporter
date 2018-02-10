// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Utils
{
    using Rendering;

    public class SVGGeomUtils {

        private struct Vector2Ext {
            private float _delta;
            private Vector2 _point;
            public float t {
                get { return this._delta; }
            }
            public Vector2 point {
                get { return this._point; }
            }
            public Vector2Ext(Vector2 point, float t) {
                this._point = point;
                this._delta = t;
            }
        }

        public static List<Vector2> RoundedRect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 p5, Vector2 p6, Vector2 p7, Vector2 p8, float r1, float r2,
                                                float angle) {
            
            List<Vector2> output = new List<Vector2>();
            output.Add(p1);
            output.Add(p2);
            output.AddRange(Arc(p2, r1, r2, angle, false, true, p3));
            
            output.Add(p3);
            output.Add(p4);
            output.AddRange(Arc(p4, r1, r2, angle, false, true, p5));
            
            output.Add(p5);
            output.Add(p6);
            output.AddRange(Arc(p6, r1, r2, angle, false, true, p7));
            
            output.Add(p7);
            output.Add(p8);
            output.AddRange(Arc(p8, r1, r2, angle, false, true, p1));
            
            return output;
        }

        public static List<Vector2> Arc(Vector2 p1, float rx, float ry, float angle, bool largeArcFlag, bool sweepFlag, Vector2 p2) {
            
            List<Vector2> output = new List<Vector2>();
            
            float tx, ty;
            double trx2, try2, tx2, ty2;
            float temp1, temp2;
            float _radian = (angle * Mathf.PI / 180.0f);
            float _CosRadian = (float)Mathf.Cos(_radian);
            float _SinRadian = (float)Mathf.Sin(_radian);
            temp1 = (p1.x - p2.x) / 2.0f;
            temp2 = (p1.y - p2.y) / 2.0f;
            tx = (_CosRadian * temp1) + (_SinRadian * temp2);
            ty = (-_SinRadian * temp1) + (_CosRadian * temp2);
            
            trx2 = rx * rx;
            try2 = ry * ry;
            tx2 = tx * tx;
            ty2 = ty * ty;
                    
            double radiiCheck = tx2 / trx2 + ty2 / try2;
            if(radiiCheck > 1) {
                rx = (float)(float)Mathf.Sqrt((float)radiiCheck) * rx;
                ry = (float)Mathf.Sqrt((float)radiiCheck) * ry;
                trx2 = rx * rx;
                try2 = ry * ry;
            }
            
            double tm1;
            tm1 = (trx2 * try2 - trx2 * ty2 - try2 * tx2) / (trx2 * ty2 + try2 * tx2);
            tm1 = (tm1 < 0) ? 0 : tm1;
            
            float tm2;
            tm2 = (largeArcFlag == sweepFlag) ? -(float)Mathf.Sqrt((float)tm1) : (float)Mathf.Sqrt((float)tm1);
                    
            float tcx, tcy;
            tcx = tm2 * ((rx * ty) / ry);
            tcy = tm2 * (-(ry * tx) / rx);
            
            float cx, cy;
            cx = _CosRadian * tcx - _SinRadian * tcy + ((p1.x + p2.x) / 2.0f);
            cy = _SinRadian * tcx + _CosRadian * tcy + ((p1.y + p2.y) / 2.0f);
            
            float ux = (tx - tcx) / rx;
            float uy = (ty - tcy) / ry;
            float vx = (-tx - tcx) / rx;
            float vy = (-ty - tcy) / ry;
            float _angle, _delta;
            
            float p, n, t;
            n = (float)Mathf.Sqrt((ux * ux) + (uy * uy));
            p = ux;
            _angle = (uy < 0) ? -(float)Mathf.Acos(p / n) : (float)Mathf.Acos(p / n);
            _angle = _angle * 180.0f / Mathf.PI;
            _angle %= 360f;
            
            n = (float)Mathf.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            p = ux * vx + uy * vy;
            t = p / n;
            if((Mathf.Abs(t) >= 0.99999f) && (Mathf.Abs(t) < 1.000009f)) {
                if(t > 0)
                    t = 1f;
                else
                    t = -1f;
            }
            _delta = (ux * vy - uy * vx < 0) ? -(float)Mathf.Acos(t) : (float)Mathf.Acos(t);
            
            _delta = _delta * 180.0f / Mathf.PI;
            
            if(!sweepFlag && _delta > 0) {
                _delta -= 360f;
            } else if(sweepFlag && _delta < 0)
                _delta += 360f;
            
            _delta %= 360f;

            // * SVGGraphics.vpm

            int number = Mathf.RoundToInt( Mathf.Clamp((100f / SVGGraphics.vpm) * Mathf.Abs(_delta) / 360f, 2, 100));
            float deltaT = _delta / number;
            
            Vector2 _point = new Vector2(0, 0);
            float t_angle;
            for(int i = 0; i <= number; i++) {
                t_angle = (deltaT * i + _angle) * Mathf.PI / 180.0f;
                _point.x = _CosRadian * rx * (float)Mathf.Cos(t_angle) - _SinRadian * ry * (float)Mathf.Sin(t_angle) + cx;
                _point.y = _SinRadian * rx * (float)Mathf.Cos(t_angle) + _CosRadian * ry * (float)Mathf.Sin(t_angle) + cy;
                output.Add(_point);
            }
            
            return output;
        }

        public static Vector2 TransformPoint(Vector2 point, SVGMatrix matrix)
        {
            point = matrix.Transform(point);
            return point;
        }
        
        private static float BelongPosition(Vector2 a, Vector2 b, Vector2 c) {
            float _up, _under, _r;
            _up = ((a.y - c.y) * (b.x - a.x)) - ((a.x - c.x) * (b.y - a.y));
            _under = ((b.x - a.x) * (b.x - a.x)) + ((b.y - a.y) * (b.y - a.y));
            _r = _up / _under;
            return _r;
        }
        //Caculate Distance from c point to line segment [a,b]
        //return d point is the point on that line segment.
        private static int NumberOfLimitForCubic(Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
            float _r1 = BelongPosition(a, d, b);
            float _r2 = BelongPosition(a, d, c);
            if((_r1 * _r2) > 0)
                return 0;
            else
                return 1;
        }
        private static float Distance(Vector2 a, Vector2 b, Vector2 c) {
            float _up, _under, _distance;
            _up = ((a.y - c.y) * (b.x - a.x)) - ((a.x - c.x) * (b.y - a.y));
            _under = ((b.x - a.x) * (b.x - a.x)) + ((b.y - a.y) * (b.y - a.y));
            _distance = Mathf.Abs(_up / _under) * (float)Mathf.Sqrt(_under);
            return _distance;
        }
        
        private static Vector2 EvaluateForCubic(float t, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) {
            Vector2 _return = new Vector2(0, 0);
            float b0, b1, b2, b3, b4;
            b0 = (1.0f - t);
            b1 = b0 * b0 * b0;
            b2 = 3 * t * b0 * b0;
            b3 = 3 * t * t * b0;
            b4 = t * t * t;
            _return.x = b1 * p1.x + b2 * p2.x + b3 * p3.x + b4 * p4.x;
            _return.y = b1 * p1.y + b2 * p2.y + b3 * p3.y + b4 * p4.y;
            return _return;
        }
        
        private static Vector2 EvaluateForQuadratic(float t, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) {
            Vector2 _return = new Vector2(0, 0);
            float b0, b1, b2, b3;
            b0 = (1.0f - t);
            b1 = b0 * b0;
            b2 = 2 * t * b0;
            b3 = t * t;
            _return.x = b1 * p1.x + b2 * p2.x + b3 * p3.x;
            _return.y = b1 * p1.y + b2 * p2.y + b3 * p3.y;
            return _return;
        }


        private static LiteStack<Vector2Ext> _stack = new LiteStack<Vector2Ext>();
        private static List<Vector2Ext> _limitList = new List<Vector2Ext>();
        private static List<Vector2> CubicCurve(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, int numberOfLimit, bool cubic) {
            
            List<Vector2> output = new List<Vector2>();
            
            //MoveTo the first Point;
            //How many times the curve change form innegative -> negative or vice versa
            int _limit = numberOfLimit;
            float t1, t2, _flatness;
            t1 = 0.0f;
            //t1 is the start point of [0..1].
            t2 = 1.0f;
            //t2 is the end point of [0..1]
            _flatness = 1.0f;
            
            Vector2Ext _pStart, _pEnd, _pMid;
            _pStart = new Vector2Ext(cubic ? EvaluateForCubic(t1, p1, p2, p3, p4) : EvaluateForQuadratic(t1, p1, p2, p3, p4), t1);
            
            _pEnd = new Vector2Ext(cubic ? EvaluateForCubic(t2, p1, p2, p3, p4) : EvaluateForQuadratic(t2, p1, p2, p3, p4), t2);
            
            // The point on Line Segment[_pStart, _pEnd] correlate with _t
            
            _stack.Clear();
            _stack.Push(_pEnd);
            //Push End Point into Stack
            //Array of Change Point
            _limitList.Clear();
            if(_limitList.Capacity < _limit + 1)
                _limitList.Capacity = _limit + 1;
            
            int _count = 0;
            while(true) {
                _count++;
                float _tm = (t1 + t2) / 2;
                //tm is a middle of t1 .. t2. [t1 .. tm .. t2]
                //The point on the Curve correlate with tm
                _pMid = new Vector2Ext(cubic ? EvaluateForCubic(_tm, p1, p2, p3, p4) : EvaluateForQuadratic(_tm, p1, p2, p3, p4), _tm);
                
                //Calculate Distance from Middle Point to the Flatnet
                float dist = Distance(_pStart.point, ((Vector2Ext)_stack.Peek()).point, _pMid.point);
                
                //flag = true, Curve Segment must be drawn, else continue calculate other middle point.
                bool flag = false;
                if(dist < _flatness) {
                    int i = 0;
                    float mm = 0.0f;
                    
                    for(i = 0; i < _limit; i++) {
                        mm = (t1 + _tm) / 2;
                        
                        Vector2Ext _q = new Vector2Ext(cubic ? EvaluateForCubic(mm, p1, p2, p3, p4) : EvaluateForQuadratic(mm, p1, p2, p3, p4), mm);
                        if(_limitList.Count - 1 < i)
                            _limitList.Add(_q);
                        else
                            _limitList[i] = _q;
                        dist = Distance(_pStart.point, _pMid.point, _q.point);
                        if(dist >= _flatness) {
                            break;
                        } else {
                            _tm = mm;
                        }
                    }
                    
                    if(i == _limit) {
                        flag = true;
                    } else {
                        //Continue calculate the first point has Distance > Flatness
                        _stack.Push(_pMid);
                        
                        for(int j = 0; j <= i; j++)
                            _stack.Push(_limitList[j]);
                        t2 = mm;
                    }
                }
                
                if(flag) {
                    output.Add(_pStart.point);
                    output.Add(_pMid.point);
                    
                    _pStart = _stack.Pop();
                    
                    if(_stack.Count == 0)
                        break;
                    
                    _pMid = _stack.Peek();
                    t1 = t2;
                    t2 = _pMid.t;
                } else if(t2 > _tm) {
                    //If Distance > Flatness and t1 < tm < t2 then new t2 is tm.
                    _stack.Push(_pMid);
                    t2 = _tm;
                }
            }
            output.Add(_pStart.point);
            return output;
        }

        public static List<Vector2> CubicCurve(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) {
            return new List<Vector2>(SVGBezier.AdaptiveCubicCurve(SVGGraphics.vpm, p1, p2, p3, p4));
        }

        /*
        public static List<Vector2> QuadraticCurve(Vector2 p1, Vector2 p2, Vector2 p3) {
            return new List<Vector2>(SVGBezier.AdaptiveCubicCurve(SVGGraphics.vpm, p1, p2, p2, p3));
        }
        */

        public static List<Vector2> QuadraticCurve(Vector2 p1, Vector2 p2, Vector2 p3) {
            var cP2 = p1 + (2f / 3f) * (p2 - p1);
            var cP3 = p3 + (2f / 3f) * (p2 - p3);
            
            return new List<Vector2>(SVGBezier.AdaptiveCubicCurve(SVGGraphics.vpm, p1, cP2, cP3, p3));
        }

        public static bool IsWindingClockWise(List<Vector2> points)
        {
            if(points == null || points.Count < 2) return false;
            int pointsCount = points.Count;
            Vector2 lastPoint = points[0];
            float sum = 0f;
            for(int i = 1; i < pointsCount; i++)
            {
                sum += (points[i].x - lastPoint.x) * (points[i].y + lastPoint.y);
                lastPoint = points[i];
            }

            return sum >= 0;
        }

        public static bool IsWindingClockWise(Vector2[] points)
        {
            if(points == null || points.Length < 2) return false;
            int pointsCount = points.Length;
            Vector2 lastPoint = points[0];
            float sum = 0f;
            for(int i = 1; i < pointsCount; i++)
            {
                sum += (points[i].x - lastPoint.x) * (points[i].y + lastPoint.y);
                lastPoint = points[i];
            }
            
            return sum >= 0;
        }

        public static Vector2[] GetPathNormals(List<Vector2> points)
        {
            if(points == null || points.Count < 2) return null;
            Vector2[] normals = new Vector2[points.Count];
            int pointsCount = points.Count;
            Vector2 lastPoint = points[0];
            Vector2 normal;
            for(int i = 1; i < pointsCount; i++)
            {
                normal = (points[i] - lastPoint).normalized;
                normals[i].x = normal.y;
                normals[i].y = -normal.x;
                lastPoint = points[i];
            }
            normal = (points[0] - lastPoint).normalized;
            normals[0] = new Vector2(normal.y, -normal.x);
            return normals;
        }

        public static Vector2[] GetPathNormals(Vector2[] points)
        {
            if(points == null || points.Length < 2) return null;
            Vector2[] normals = new Vector2[points.Length];
            int pointsCount = points.Length;
            Vector2 lastPoint = points[0];
            Vector2 normal;
            for(int i = 1; i < pointsCount; i++)
            {
                normal = (points[i] - lastPoint).normalized;
                normals[i].x = normal.y;
                normals[i].y = -normal.x;
                lastPoint = points[i];
            }
            normal = (points[0] - lastPoint).normalized;
            normals[0] = new Vector2(normal.y, -normal.x);
            return normals;
        }

        public static Vector2[] OffsetVerts(Vector2[] aSegment, float scale) {
            Vector2[] result = (Vector2[])aSegment.Clone();
            int length  = aSegment.Length;
            for (int i = length - 1; i >= 0; i--) {
                result[i] += GetNormal(aSegment, i, false) * scale;
            }
            return result;
        }

        public static Vector2 GetNormal (Vector2[] aSegment, int i, bool  aClosed) {
            if (aSegment.Length < 2) return Vector2.up;
            Vector2 curr = aClosed && i == aSegment.Length - 1 ? aSegment[0] : aSegment[i];
            
            // get the vertex before the current vertex
            Vector2 prev = Vector2.zero;
            if (i-1 < 0) {
                if (aClosed) {
                    prev = aSegment[aSegment.Length-2];
                } else {
                    prev = curr - (aSegment[i+1]-curr);
                }
            } else {
                prev = aSegment[i-1];
            }
            
            // get the vertex after the current vertex
            Vector2 next = Vector2.zero;
            if (i+1 > aSegment.Length-1) {
                if (aClosed) {
                    next = aSegment[1];
                } else {
                    next = curr - (aSegment[i-1]-curr);
                }
            } else {
                next = aSegment[i+1];
            }
            
            prev = prev - curr;
            next = next - curr;
            
            prev.Normalize ();
            next.Normalize ();
            
            prev = new Vector2(-prev.y, prev.x);
            next = new Vector2(next.y, -next.x);
            
            Vector2 norm = (prev + next) / 2;
            norm.Normalize();
            
            return norm;
        }
    }
}
