// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Geometry
{
    using Utils;

    public class SVGMeshCutter {

        /*
        public static void MeshSplit(SVGMesh mesh, Vector2 origin, Vector2 direction)
        {
            Mesh inputMesh = new Mesh();
            inputMesh.vertices = mesh.vertices;
            inputMesh.triangles = mesh.triangles;
            inputMesh.colors32 = mesh.colors;
            inputMesh.uv = (Vector2[])mesh.uvs;
            inputMesh.uv2 = (Vector2[])mesh.uvs2;

            MeshSplit(inputMesh, origin, direction);
            SVGMeshUtils.AutoWeldVertices(inputMesh, 0f);

            mesh.vertices = inputMesh.vertices;
            mesh.triangles = inputMesh.triangles;
            mesh.colors = inputMesh.colors32;
            mesh.uvs = inputMesh.uv;
            mesh.uvs2 = inputMesh.uv2;
        }

        // Cut a mesh by the line origin->d and split it into two objects if necessary
        public static void MeshSplit(Mesh mesh, Vector2 origin, Vector2 direction)
        {
            Vector3[] vertices = mesh.vertices;
            MeshBuilder leftSide = new MeshBuilder(mesh, vertices);
            MeshBuilder rightSide = new MeshBuilder(mesh, vertices);
            
            // which side my vertices at?
            bool[] side = new bool[vertices.Length];
            bool allOnTheLeft = true, allOnTheRight = true;
            for (int i = 0; i < vertices.Length; i++)
            {
                side [i] = direction.x * (vertices [i].y - origin.y) > direction.y * (vertices [i].x - origin.x);
                if (side [i])
                    allOnTheRight = false;
                else
                    allOnTheLeft = false;
            }

            if (allOnTheLeft || allOnTheRight)
                return;
            
            // build the mesh by adding triangles (possibly cut)
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                int i1 = mesh.triangles [i], i2 = mesh.triangles [i + 1], i3 = mesh.triangles [i + 2];
                
                switch ((side [i1] ? 1 : 0) | (side [i2] ? 2 : 0) | (side [i3] ? 4 : 0))
                {
                    case 0 | 0 | 0:
                        leftSide.AddTri(i1, i2, i3);
                        break;
                    case 0 | 0 | 1:
                        MeshBuilder.AddCutTri(rightSide, leftSide, i1, i2, i3, origin, direction);
                        break;
                    case 0 | 2 | 0:
                        MeshBuilder.AddCutTri(rightSide, leftSide, i2, i3, i1, origin, direction);
                        break;
                    case 4 | 0 | 0:
                        MeshBuilder.AddCutTri(rightSide, leftSide, i3, i1, i2, origin, direction);
                        break;
                    case 4 | 2 | 0:
                        MeshBuilder.AddCutTri(leftSide, rightSide, i1, i2, i3, origin, direction);
                        break;
                    case 4 | 0 | 1:
                        MeshBuilder.AddCutTri(leftSide, rightSide, i2, i3, i1, origin, direction);
                        break;
                    case 0 | 2 | 1:
                        MeshBuilder.AddCutTri(leftSide, rightSide, i3, i1, i2, origin, direction);
                        break;
                    case 4 | 2 | 1:
                        rightSide.AddTri(i1, i2, i3);
                        break;
                }
            }
            
            // degenerate?
            if (leftSide.IsDegenerate(origin, direction))
                return;
            if (rightSide.IsDegenerate(origin, direction))
                return;

            mesh.Clear();

            int finLength = leftSide.pos.Count + rightSide.pos.Count;
            int finTrianglesLength = leftSide.tri.Count + rightSide.tri.Count;
            int leftSideLength = leftSide.pos.Count;
            int rightSideLength = rightSide.pos.Count;
            int leftSideTrianglesLength = leftSide.tri.Count;
            int rightSideTrianglesLength = rightSide.tri.Count;

            Vector3[] finVertices = new Vector3[finLength];
            Color32[] finColors = new Color32[finLength];
            Vector2[] finUv = new Vector2[finLength];
            Vector2[] finUv2 = new Vector2[finLength];
            int[] finTriangles = new int[finTrianglesLength];

            for(int i = 0; i < leftSideLength; i++)
            {
                finVertices[i] = leftSide.pos[i];
                finColors[i] = leftSide.col[i];
                finUv[i] = leftSide.uv[i];
                finUv2[i] = leftSide.uv2[i];
            }

            int offsetIndex;
            for(int i = 0; i < rightSideLength; i++)
            {
                offsetIndex = leftSideLength + i;
                finVertices[offsetIndex] = rightSide.pos[i];
                finColors[offsetIndex] = rightSide.col[i];
                finUv[offsetIndex] = rightSide.uv[i];
                finUv2[offsetIndex] = rightSide.uv2[i];
            }

            for(int i = 0; i < leftSideTrianglesLength; i++)
            {
                finTriangles[i] = leftSide.tri[i];
            }

            for(int i = 0; i < rightSideTrianglesLength; i++)
            {
                finTriangles[leftSideTrianglesLength + i] = leftSideLength + rightSide.tri[i];
            }

            mesh.vertices = finVertices;
            mesh.colors32 = finColors;
            mesh.uv = finUv;
            mesh.uv2 = finUv2;
            mesh.triangles = finTriangles;
            mesh.RecalculateBounds();
        }
        */

        struct MeshBuilder
        {
            public List<Vector3> pos;
            public List<Color32> col;
            public List<Vector2> uv;
            public List<Vector2> uv2;
            public List<int> tri;   // triangles
            Dictionary<IntPair, int> map;  // dictionary from old mesh vertex pairs to new mesh
            Mesh mesh;  // the original mesh
            Vector3[] origVertices;
            
            // internal: ordered pair of two ints (used as map key)
            struct IntPair
            {
                public int first, second;
                public IntPair(int first, int second)
                {
                    if (first < second)
                    {
                        this.first = first;
                        this.second = second;
                    } else
                    {
                        this.first = second;
                        this.second = first;
                    }
                }
            }
            
            public MeshBuilder(Mesh m, Vector3[] vertices)
            {
                pos = new List<Vector3>();
                col = new List<Color32>();
                uv = new List<Vector2>();
                uv2 = new List<Vector2>();
                tri = new List<int>();
                map = new Dictionary<IntPair, int>();
                mesh = m;
                origVertices = vertices;
            }
            
            // Return the id of the vertex created from oldM.i. If it didn't exist yet, assign it a new id.
            int MergeVertex(int i)
            {
                int j;
                var q = new IntPair(i, i);
                if (!map.TryGetValue(q, out j))
                {
                    map.Add(q, j = pos.Count);
                    pos.Add(origVertices [i]);
                    col.Add(mesh.colors32 [i]);
                    uv.Add(mesh.uv [i]);
                    uv2.Add(mesh.uv2 [i]);
                }
                return j;
            }
            
            // Return the id of the vertex created from oldM.i1 and oldM.i2 with the interpolation parameter t.
            // If it didn't exist yet, assign it a new id.
            static void MergeCutVertex(MeshBuilder leftSide, MeshBuilder rightSide, int i1, int i2, Vector2 origin, Vector2 direction, out int jl, out int jr)
            {
                var q = new IntPair(i1, i2);
                if (!leftSide.map.TryGetValue(q, out jl))
                {
                    jl = leftSide.pos.Count;
                    jr = rightSide.pos.Count;
                    leftSide.map.Add(q, jl);
                    rightSide.map.Add(q, jr);
                    
                    // interpolate
                    float t = CutEdge(leftSide.origVertices [i1], leftSide.origVertices [i2], origin, direction);
                    
                    var pos_t = leftSide.origVertices [i1] + (leftSide.origVertices [i2] - leftSide.origVertices [i1]) * t;
                    leftSide.pos.Add(pos_t);
                    rightSide.pos.Add(pos_t);

                    var col_t = Color32.Lerp(leftSide.mesh.colors32 [i1], leftSide.mesh.colors32 [i2], t);
                    leftSide.col.Add(col_t);
                    rightSide.col.Add(col_t);
                    
                    var uv_t = leftSide.mesh.uv [i1] + (leftSide.mesh.uv [i2] - leftSide.mesh.uv [i1]) * t;
                    leftSide.uv.Add(uv_t);
                    rightSide.uv.Add(uv_t);
                    
                    var uv2_t = leftSide.mesh.uv2 [i1] + (leftSide.mesh.uv2 [i2] - leftSide.mesh.uv2 [i1]) * t;
                    leftSide.uv2.Add(uv2_t);
                    rightSide.uv2.Add(uv2_t);
                } else
                    jr = rightSide.map [q];
            }
            
            public void AddTri(int i1, int i2, int i3)
            {
                tri.Add(MergeVertex(i1));
                tri.Add(MergeVertex(i2));
                tri.Add(MergeVertex(i3));
            }
            
            // The cut goes always between 1-2 and 1-3. The resulting triangle goes left and the quad goes right.
            static public void AddCutTri(MeshBuilder leftSide, MeshBuilder rightSide, int i1, int i2, int i3, Vector2 origin, Vector2 direction)
            {
                int l1 = leftSide.MergeVertex(i1);
                int r2 = rightSide.MergeVertex(i2);
                int r3 = rightSide.MergeVertex(i3);
                int l12, r12;
                MergeCutVertex(leftSide, rightSide, i1, i2, origin, direction, out l12, out r12);
                int l13, r13;
                MergeCutVertex(leftSide, rightSide, i1, i3, origin, direction, out l13, out r13);
                
                leftSide.tri.Add(l1);
                leftSide.tri.Add(l12);
                leftSide.tri.Add(l13);
                rightSide.tri.Add(r12);
                rightSide.tri.Add(r2);
                rightSide.tri.Add(r3);
                rightSide.tri.Add(r12);
                rightSide.tri.Add(r3);
                rightSide.tri.Add(r13);
            }

            public Mesh ToMesh()
            {
                var res = new Mesh();
                res.vertices = pos.ToArray();
                res.colors32 = col.ToArray();
                res.uv = uv.ToArray();
                res.uv2 = uv2.ToArray();
                res.triangles = tri.ToArray();
                res.RecalculateBounds();
                ;
                return res;
            }
            
            // Compute the parameter of the line segment v1-v2 where it intersects the plane defined by the point origin, the vector direction and the z axis.
            static float CutEdge(Vector3 v1, Vector3 v2, Vector2 origin, Vector2 direction)
            {
                return Mathf.Clamp01((direction.y * v1.x - direction.x * v1.y + direction.x * origin.y - direction.y * origin.x) / (direction.x * (v2.y - v1.y) - direction.y * (v2.x - v1.x)));
            }
            
            public bool IsDegenerate(Vector2 origin, Vector2 direction)
            {
                float distSum = pos.Count * (-direction.x * origin.y + direction.y * origin.x);
                for (var k = 0; k < pos.Count; k++)
                    distSum += direction.x * pos [k].y - direction.y * pos [k].x;
                return Mathf.Abs(distSum) < 0.01 * direction.magnitude;
            }
            
        };
    }
}
