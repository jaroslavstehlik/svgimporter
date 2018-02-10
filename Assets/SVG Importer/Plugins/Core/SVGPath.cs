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
