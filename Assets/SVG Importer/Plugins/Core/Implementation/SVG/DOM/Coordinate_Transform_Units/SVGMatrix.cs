// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System;

namespace SVGImporter.Rendering
{
    using Document;

    [System.Serializable]
    public struct SVGMatrix
    {
        public float a, b, c, d, e, f;

        public static SVGMatrix identity
        {
            get {
                return new SVGMatrix(1, 0, 0, 1, 0, 0);
            }
        }

        public SVGMatrix(float a, float b, float c, float d, float e, float f)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.e = e;
            this.f = f;
        }

        public Vector2 position
        {
            get {
                return new Vector2(e, f);
            }
            set {
                e = value.x;
                f = value.y;
            }
        }

        public Vector2 scale
        {
            get {
                return new Vector2(Mathf.Sqrt(a * a + b * b), Mathf.Sqrt(c * c + d * d));
            }
        }

        public float skewX
        {
            get {
                Vector2 px = DeltaTransformPoint(new Vector2(0, 1));
                return (180 / Mathf.PI) * Mathf.Atan2(px.y, px.x) - 90;
            }
        }

        public float skewY
        {
            get {
                Vector2 py = DeltaTransformPoint(new Vector2(1, 0));
                return (180 / Mathf.PI) * Mathf.Atan2(py.y, py.x);
            }
        }

        public float rotation
        {
            get {
                return skewX;
            }
        }

        Vector2 DeltaTransformPoint(Vector2 point)
        {
            return new Vector2(point.x * a + point.y * c,
                        point.x * b + point.y * d);
        }

        //---------------------------------------
        public SVGMatrix Multiply(SVGMatrix secondMatrix)
        {
            float sa, sb, sc, sd, se, sf;
            sa = secondMatrix.a;
            sb = secondMatrix.b;
            sc = secondMatrix.c;
            sd = secondMatrix.d;
            se = secondMatrix.e;
            sf = secondMatrix.f;
            return new SVGMatrix(a * sa + c * sb, b * sa + d * sb,
                               a * sc + c * sd, b * sc + d * sd,
                               a * se + c * sf + e, b * se + d * sf + f);
        }

        public static SVGMatrix operator*(SVGMatrix left, SVGMatrix right) 
        {
            return new SVGMatrix(left.a * right.a + left.c * right.b, left.b * right.a + left.d * right.b,
                                 left.a * right.c + left.c * right.d, left.b * right.c + left.d * right.d,
                                 left.a * right.e + left.c * right.f + left.e, left.b * right.e + left.d * right.f + left.f);
        }

        public SVGMatrix Inverse()
        {
            double det = a * d - c * b;
            if (det == 0.0)
            {
                throw new SVGException(SVGExceptionType.MatrixNotInvertable);
            }
            return new SVGMatrix((float)(d / det), (float)(-b / det),
                               (float)(-c / det), (float)(a / det),
                               (float)((c * f - e * d) / det), (float)((e * b - a * f) / det));
        }

        public SVGMatrix Scale(float scaleFactor)
        {
            return new SVGMatrix(a * scaleFactor, b * scaleFactor,
                               c * scaleFactor, d * scaleFactor,
                               e, f);
        }

        public SVGMatrix Scale(float scaleFactorX, float scaleFactorY)
        {
            return new SVGMatrix(a * scaleFactorX, b * scaleFactorX,
                               c * scaleFactorY, d * scaleFactorY,
                               e, f);
        }

        public SVGMatrix Scale(Vector2 scaleFactor)
        {
            return new SVGMatrix(a * scaleFactor.x, b * scaleFactor.x,
                                 c * scaleFactor.y, d * scaleFactor.y,
                                 e, f);
        }

        public SVGMatrix Rotate(float angle)
        {
            float ca = Mathf.Cos(angle * Mathf.Deg2Rad);
            float sa = Mathf.Sin(angle * Mathf.Deg2Rad);

            return new SVGMatrix((float)(a * ca + c * sa), (float)(b * ca + d * sa),
                               (float)(c * ca - a * sa), (float)(d * ca - b * sa),
                               e, f);
        }

        public SVGMatrix Translate(float x, float y)
        {
            return new SVGMatrix(a, b,
                                 c, d,
                                 a * x + c * y + e, b * x + d * y + f);
        }

        public SVGMatrix Translate(Vector2 position)
        {
            return new SVGMatrix(a, b,
                                 c, d,
                                 a * position.x + c * position.y + e, b * position.x + d * position.y + f);
        }

        public SVGMatrix SkewX(float angle)
        {
            float ta = Mathf.Tan(angle * Mathf.Deg2Rad);
            return new SVGMatrix(a, b,
                               (float)(c + a * ta), (float)(d + b * ta),
                               e, f);
        }

        public SVGMatrix SkewY(float angle)
        {
            float ta = Mathf.Tan(angle * Mathf.Deg2Rad);
            return new SVGMatrix((float)(a + c * ta), (float)(b + d * ta),
                               c, d,
                               e, f);
        }

        public Vector2 Transform(Vector2 point)
        {
            return new Vector2(a * point.x + c * point.y + e, b * point.x + d * point.y + f);
        }

        public Vector3 Transform(Vector3 point)
        {
            return new Vector3(a * point.x + c * point.y + e, b * point.x + d * point.y + f, 0f);
        }

        public static SVGMatrix TRS(Vector2 position, float rotation , Vector2 scale)
        {
            const float a = 1, b = 0, c = 0, d = 1, e = 0, f = 0;
            float ca = Mathf.Cos(rotation * Mathf.Deg2Rad);
            float sa = Mathf.Sin(rotation * Mathf.Deg2Rad);
            return new SVGMatrix((a * ca + c * sa) * scale.x, (b * ca + d * sa) * scale.x,
                                 (c * ca - a * sa) * scale.y, (d * ca - b * sa) * scale.y,
                                 a * position.x + c * position.y + e, b * position.x + d * position.y + f);
        }

        public Matrix4x4 ToMatrix4x4()
        {
            Matrix4x4 matrix = Matrix4x4.identity;

            matrix [0, 0] = a;
            matrix [0, 1] = b;
            matrix [1, 0] = c;
            matrix [1, 1] = d;
            matrix [2, 0] = e;
            matrix [2, 1] = f;

            return matrix;
        }

        public void Reset()
        {
            a = 1; b = 0; c = 0; d = 1; e = 0; f = 0;
        }

        public override string ToString()
        {
            string output = string.Format("[SVGMatrix] a: {0}, b: {1}, c: {2}, d: {3}, e: {4}, f: {5}", a, b, c, d, e, f);
            output += string.Format("\nposition: {0}, rotation: {1}, skewX: {2}, skewY: {3}, scale: {4}", position, rotation, skewX, skewY, scale);
            return output;
        }
    }
}
