

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Utils
{
    using Rendering;

    internal class SVGGeomUtils {

        public static Vector2 TransformPoint(Vector2 point, SVGMatrix matrix)
        {
            point = matrix.Transform(point);
            return point;
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
