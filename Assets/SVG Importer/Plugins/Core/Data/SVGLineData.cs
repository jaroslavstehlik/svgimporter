using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SVGLineData 
{
	public struct Edge
    {
        public Vector2 start;
        public Vector2 end;

        public Edge(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
        }

        public Vector2 Direction()
        {
            return start - end;
        }

        public Vector2 DirectionNormalized()
        {
            Vector2 direction = start - end;
            float magnitude = Mathf.Sqrt(direction.x * direction.x + direction.y * direction.y);
            if(magnitude != 0f)
            {
                direction.x /= magnitude;
                direction.y /= magnitude;
            }
            return direction;
        }

        public float Magnitude()
        {
            Vector2 direction = start - end;
            return Mathf.Sqrt(direction.x * direction.x + direction.y * direction.y);
        }
    }

    public List<Vector2> _points = new List<Vector2>();
    protected float[] _magnitudes;
    protected Vector2[] _normals;
    protected float _totalMagnitude;

    public SVGLineData(){}
    public SVGLineData(List<Vector2> points){
        _points = points;
    }

    public SVGLineData(Vector2[] points){
        _points = new List<Vector2>(points);
    }

    public void Add(Vector2 point)
    {
        _points.Add(point);
    }

    public void Insert(int index, Vector2 item)
    {
        _points.Insert(index, item);
    }

    public void Remove(Vector2 point)
    {
        _points.Remove(point);
    }

    public void RemoveAt(int index)
    {
        _points.RemoveAt(index);
    }

    public Edge GetEdge(int index)
    {
        return new Edge(_points[index], _points[index + 1]);
    }

    public int GetEdgeCount()
    {
        if(_points == null || _points.Count < 2) return 0;
        return _points.Count - 1;
    }

    public void UpdateMagnitudes()
    {
        _totalMagnitude = 0f;
        int edgeCount = GetEdgeCount();
        _magnitudes = new float[edgeCount];
        for(int i = 0; i < edgeCount; i++)
        {
            _magnitudes[i] = GetEdge(i).Magnitude();
            _totalMagnitude += _magnitudes[i];
        }
    }

    public void UpdateNormals()
    {
        int edgeCount = GetEdgeCount();
        _normals = new Vector2[edgeCount];
        for(int i = 0; i < edgeCount; i++)
        {
            _normals[i] = GetEdge(i).DirectionNormalized();
        }
    }

    public void UpdateAll()
    {
        int edgeCount = GetEdgeCount();
        _magnitudes = new float[edgeCount];
        _normals = new Vector2[edgeCount];
        Edge currentEdge;
        for(int i = 0; i < edgeCount; i++)
        {
            currentEdge = GetEdge(i);
            _normals[i] = currentEdge.Direction();
            _magnitudes[i] = Mathf.Sqrt(_normals[i].x * _normals[i].x + _normals[i].y * _normals[i].y);

            if(_magnitudes[i] != 0f)
            {
                _normals[i].x /= _magnitudes[i];
                _normals[i].y /= _magnitudes[i];
            }
        }
    }

    public void Clear()
    {
        _points.Clear();
        _normals = null;
        _magnitudes = null;
    }

    public Vector2 GetNormal(int index)
    {
        return _normals[index];
    }

    public float GetMagnitude(int index)
    {
        return _magnitudes[index];
    }

    public float totalMagnitude
    {
        get {
            return _totalMagnitude;
        }
    }
}
