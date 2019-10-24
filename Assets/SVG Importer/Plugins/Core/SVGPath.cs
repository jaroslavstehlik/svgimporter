// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;

[System.Serializable]
public class SVGPath {

    public Vector2[] points;
    public Rect bounds;

    public SVGPath(){}
    public SVGPath(Vector2[] points)
    {
        this.points = points;
        RecalculateBounds();
    }
    public SVGPath(Vector2[] points, Rect bounds)
    {
        this.points = points;
        this.bounds = bounds;
    }

    public int pointCount
    {
        get {
            if(points == null)
                return 0;

            return points.Length;
        }
    }

    public void RecalculateBounds()
    {
        if(points == null || points.Length == 0)
        {
            bounds = new Rect();
        } else {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            int pointsLength = points.Length;
            for(int i = 0; i < pointsLength; i++)
            {
                if(points[i].x < minX) minX = points[i].x;
                if(points[i].x > maxX) maxX = points[i].x;
                if(points[i].y < minY) minY = points[i].x;
                if(points[i].y > maxY) maxY = points[i].x;
            }

            bounds = new Rect(minX, maxY, maxX - minX, maxY - minY);
        }
    }
}
