

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Geometry
{
    using Utils;

    public class SVGMeshCutter {
       
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
