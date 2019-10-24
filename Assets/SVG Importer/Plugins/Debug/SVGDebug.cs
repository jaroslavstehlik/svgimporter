// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace SVGImporter.Utils
{        
    public static class SVGDebug
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Window/SVG Importer/Debug/SVG Atlas", false, 10)]
        private static void SelectSVGAtlas()
        {
            UnityEditor.Selection.activeGameObject = SVGAtlas.Instance.gameObject;
        }
#endif

        public static void DebugArray(ICollection array)
        {
            if(array == null)
            {
                Debug.Log("Array is null!");
                return;
            }

            IEnumerator enumerator = array.GetEnumerator();
            int i = 0;
            while(enumerator.MoveNext())
            {
                Debug.Log("i: "+i+", "+enumerator.Current);
                i++;
            }
        }

        public static void DebugPoint(Vector3 point)
        {
            GameObject goRoot = new GameObject("Debug Points");
            goRoot.transform.position = point;
            goRoot.AddComponent<SVGDebugPoints>();
        }

        public static void DebugPoints(List<List<Vector2>> path)
        {
            GameObject goRoot = new GameObject("Debug Points");
            for(int i = 0; i < path.Count; i++)
            {
                GameObject go = new GameObject("Path");
                go.transform.SetParent(goRoot.transform);
                go.AddComponent<SVGDebugPoints>();
                for(int j = 0; j < path[i].Count; j++)
                {
                    GameObject childGo = new GameObject("Point");
                    childGo.transform.SetParent(go.transform);
                    Vector3 position = path[i][j];
                    position.y *= -1f;
                    childGo.transform.localPosition = position;
                }
            }
        }

        public static void DebugPoints(List<List<Vector3>> path)
        {
            GameObject goRoot = new GameObject("Debug Points");
            for(int i = 0; i < path.Count; i++)
            {
                GameObject go = new GameObject("Path");
                go.transform.SetParent(goRoot.transform);
                go.AddComponent<SVGDebugPoints>();
                for(int j = 0; j < path[i].Count; j++)
                {
                    GameObject childGo = new GameObject("Point");
                    childGo.transform.SetParent(go.transform);
                    Vector3 position = path[i][j];
                    position.y *= -1f;
                    childGo.transform.localPosition = position;
                }
            }
        }

        public static void DebugPoints(List<Vector2> path)
        {
            DebugPoints(new List<List<Vector2>>(){path});
        }

        public static void DebugPoints(List<Vector3> path)
        {
            DebugPoints(new List<List<Vector3>>(){path});
        }

        public static void DebugSegments(StrokeSegment[] segments)
        {
            GameObject goRoot = new GameObject("Debug Segments");
            for(int i = 0; i < segments.Length; i++)
            {
                GameObject go = new GameObject("Segment");
                go.transform.SetParent(goRoot.transform);
                go.AddComponent<SVGDebugPoints>();

                GameObject childGo1 = new GameObject("StartPoint");
                childGo1.transform.SetParent(go.transform);
                Vector3 startPoint = segments[i].startPoint;
                startPoint.y *= -1f;
                childGo1.transform.localPosition = startPoint;

                GameObject childGo2 = new GameObject("EndPoint");
                childGo2.transform.SetParent(go.transform);
                Vector3 endPoint = segments[i].endPoint;
                endPoint.y *= -1f;
                childGo2.transform.localPosition = endPoint;
            }
        }
    }
}
