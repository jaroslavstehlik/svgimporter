

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Utils
{    
    public class Edge
    {
        // The indiex to each vertex
        public int[] vertexIndex = new int[2];
        // The index into the face.
        // (faceindex[0] == faceindex[1] means the edge connects to only one triangle)
        public int[] faceIndex = new int[2];
    }

    public class SVGMeshUtils {
    	
    	public enum ColorChannel
    	{
    		RED,
    		GREEN,
    		BLUE,
    		ALPHA
    	}
    	
    	const float PI2 = Mathf.PI * 2;

    	public static Mesh Circle (int circuitSegments, Matrix4x4 meshTransform, Matrix4x4 uvTransform)
    	{
    		circuitSegments = Mathf.Clamp (circuitSegments, 4, int.MaxValue) + 1;
    		int circuitSegmentsMinusOne = circuitSegments - 1;
    		
    		Mesh m = new Mesh ();
    		int totalSegments = circuitSegments + 1;
    		int totalTriangles = circuitSegments * 3;
    		Vector3[] vertices = new Vector3[totalSegments];
    		Vector2[] uv = new Vector2[totalSegments];
    		int[] triangles = new int[totalTriangles];
    		
    		float cs, x, y;
    		Vector2 uvwPoint;
    		
    		for (int i = 0; i < circuitSegments; i++) {
    			cs = (float)i / (float)circuitSegmentsMinusOne;
    			x = Mathf.Cos (cs * PI2) * 0.5f;
    			y = Mathf.Sin (cs * PI2) * 0.5f;
    			
    			vertices [i].x = x;
    			vertices [i].y = y;
    			vertices [i] = meshTransform.MultiplyPoint (vertices [i]);
    			vertices [i].z = 0f;
    			
    			uvwPoint.x = x;
    			uvwPoint.y = y;
    		
                uvwPoint = uvTransform.MultiplyPoint(uvwPoint);
    			uv [i].x = uvwPoint.x + 0.5f;
    			uv [i].y = uvwPoint.y + 0.5f;
    		}

    		vertices [circuitSegments] = meshTransform.MultiplyPoint (vertices [circuitSegments]);
    				
    		uvwPoint.x = uvwPoint.y = 0f;		
            uvwPoint = uvTransform.MultiplyPoint(uvwPoint);
    		uv [circuitSegments].x = uvwPoint.x + 0.5f;
    		uv [circuitSegments].y = uvwPoint.y + 0.5f;
    		
    		int segmentIndex = 0;
    		for (int i = 0; i < totalTriangles; i += 3) {			
    			triangles [i] = segmentIndex;
    			triangles [i + 2] = segmentIndex + 1;
    			triangles [i + 1] = circuitSegments;	
    			segmentIndex++;
    		}
    		
    		m.vertices = vertices;
    		m.uv = uv;
    		m.triangles = triangles;
    		m.RecalculateBounds ();
    		
    		return m;
    	}
    	
        public static Mesh Rectangle (Matrix4x4 meshTransform, Matrix4x4 uvTransform)
    	{
    		Mesh mesh = new Mesh ();
    		Vector3[] vertices = new Vector3[4];
    		int[] triangles = new int[6];
    		Vector2[] uv = new Vector2[4];
    		Vector2 uvwPoint;
    		
    		vertices [0].x = -0.5f;
    		vertices [0].y = 0.5f;				
    		vertices [0] = meshTransform.MultiplyPoint (vertices [0]);
    		vertices [0].z = 0f;
    		
    		vertices [1].x = 0.5f;
    		vertices [1].y = 0.5f;				
    		vertices [1] = meshTransform.MultiplyPoint (vertices [1]);
    		vertices [1].z = 0f;
    		
    		vertices [2].x = -0.5f;
    		vertices [2].y = -0.5f;					
    		vertices [2] = meshTransform.MultiplyPoint (vertices [2]);
    		vertices [2].z = 0f;
    		
    		vertices [3].x = 0.5f;
    		vertices [3].y = -0.5f;		
    		vertices [3] = meshTransform.MultiplyPoint (vertices [3]);
    		vertices [3].z = 0f;
    		
    		triangles [0] = 0;
    		triangles [1] = 1;
    		triangles [2] = 2;
    		triangles [3] = 1;
    		triangles [4] = 3;
    		triangles [5] = 2;
    		
    		uv [0].x = -0.5f;
    		uv [0].y = 0.5f;
    		
            uvwPoint = uvTransform.MultiplyPoint(new Vector3 (uv [0].x, uv [0].y, 0f));
    		uv [0].x = uvwPoint.x + 0.5f;
    		uv [0].y = uvwPoint.y + 0.5f;

    		uv [1].x = 0.5f;
    		uv [1].y = 0.5f;
    		
            uvwPoint = uvTransform.MultiplyPoint(new Vector3 (uv [1].x, uv [1].y, 0f));
    		uv [1].x = uvwPoint.x + 0.5f;
    		uv [1].y = uvwPoint.y + 0.5f;
    		
    		uv [2].x = -0.5f;
    		uv [2].y = -0.5f;
    		
            uvwPoint = uvTransform.MultiplyPoint(new Vector3 (uv [2].x, uv [2].y, 0f));
    		uv [2].x = uvwPoint.x + 0.5f;
    		uv [2].y = uvwPoint.y + 0.5f;
    		
    		uv [3].x = 0.5f;
    		uv [3].y = -0.5f;

            uvwPoint = uvTransform.MultiplyPoint(new Vector3 (uv [3].x, uv [3].y, 0f));
    		uv [3].x = uvwPoint.x + 0.5f;
    		uv [3].y = uvwPoint.y + 0.5f;
    		
    		mesh.vertices = vertices;
    		mesh.triangles = triangles;
    		mesh.uv = uv;
    		mesh.RecalculateBounds ();

    		return mesh;
    	}

        public static Mesh Rectangle ()
        {
            return Rectangle(Matrix4x4.identity, Matrix4x4.identity);
        }

        public static Mesh Line(int tessellation, Vector3[] positions, Color32 color, float size = 1f, bool closeLine = false)
        {
            return Line(tessellation, positions, SVGArrayUtils.CreatePreinitializedArray<Color32>(color, positions.Length), SVGArrayUtils.CreatePreinitializedArray<float>(size, positions.Length), null, closeLine);
        }

        public static Mesh Line(int tessellation, Vector2[] positions, Color32 color, float size = 1f, bool closeLine = false)
        {
            Vector3[] newPositions = new Vector3[positions.Length];
            for(int i = 0; i < positions.Length; i++)
            {
                newPositions[i].x = positions[i].x;
                newPositions[i].y = positions[i].y;
            }
            return Line(tessellation, newPositions, SVGArrayUtils.CreatePreinitializedArray<Color32>(color, positions.Length), SVGArrayUtils.CreatePreinitializedArray<float>(size, positions.Length), null, closeLine);
        }

        public static Mesh Line(int tessellation, Vector2[] positions, Color32[] colors = null, float[] sizes = null, Vector3[] rotations = null, bool closeLine = false)
        {
            Vector3[] newPositions = new Vector3[positions.Length];
            for(int i = 0; i < positions.Length; i++)
            {
                newPositions[i].x = positions[i].x;
                newPositions[i].y = positions[i].y;
            }
            return Line(tessellation, newPositions, colors, sizes, rotations, closeLine);
        }

        public static Vector2 lineUVScale = Vector2.one;
        public static Vector2 lineUVOffset = Vector2.zero;
        public static void ResetLineSettings()
        {
            lineUVScale = Vector2.one;
            lineUVOffset = Vector2.zero;
        }
        public static Mesh Line(int tessellation, Vector3[] positions, Color32[] colors = null, float[] sizes = null, Vector3[] rotations = null, bool closeLine = false)
        {       
            if (positions == null)
            {
#if DEBUG
                Debug.LogWarning("Line / Array is Null!");
#endif
                ResetLineSettings();
                return null;
            }       
            
            if (positions.Length < 2)
            {
#if DEBUG
                Debug.LogWarning("Line / We need at least 2 positions!");
#endif
                ResetLineSettings();
                return null;
            }

            if (tessellation < 1)
                tessellation = 1;
            
            int _tessellation = tessellation * 2;
            float _tessellationProgress;
            
            int _vertexCount = positions.Length * _tessellation;
            int _positionsLength = positions.Length;
            int totalTriangles = (_positionsLength - 1) * (_tessellation - 1) * 6;

            bool useColors = (colors != null && colors.Length == _positionsLength);
            bool useRotations = (rotations != null && rotations.Length == _positionsLength);

            Vector3[] _vertices = new Vector3[_vertexCount];       
            int[] _triangles = new int[totalTriangles];

            Color32[] _colors = null;
            if(useColors)
                _colors = new Color32[_vertexCount];        

            if (sizes == null)
                sizes = SVGArrayUtils.CreatePreinitializedArray<float>(1f, _vertexCount);

            Vector2[] _uvs = new Vector2[_vertexCount];
            Vector3[] normals = new Vector3[_positionsLength];
            float[] magnitudes = new float[_positionsLength];

            int i = 0, j = 0, ij = 0, positionIndex;       
            Vector3 lastPosition = positions [0], currentPosition = positions [0];
			float magnitude = 0f, size = 1f, totalCurveLength = 0f;//currentMagnitude = 0f, 
            Color32 color = Color.white;

            // Pre-Processing
            for (i = 0; i < _positionsLength; i++)
            {
                // Normal direction
                normals [i].x = positions [i].x - lastPosition.x;
                normals [i].y = positions [i].y - lastPosition.y;
                normals [i].z = positions [i].z - lastPosition.z;
                
                // Normal magnitude
                magnitudes [i] = Mathf.Sqrt(normals [i].x * normals [i].x + normals [i].y * normals [i].y + normals [i].z * normals [i].z);
                
                if (magnitudes [i] != 0f)
                {
                    normals [i].x /= magnitudes [i];
                    normals [i].y /= magnitudes [i];
                    normals [i].z /= magnitudes [i];
                }

                if(useRotations)
                {
                    normals [i].x += rotations [i].x;
                    normals [i].y += rotations [i].y;
                    normals [i].z += rotations [i].z;
                }
                
                // Total curve magnitude
                totalCurveLength += magnitudes [i];
                lastPosition = positions [i];
            }
            
            //totalCurveLength = (totalCurveLength * (1f / totalCurveLength)) * 5f;
            
            normals [0] = (positions [1] - positions [0]).normalized;
            if(useRotations)
                normals [0] += rotations [0];

            lastPosition = positions [0];
            
            int tl = 0;
            
            Vector3 currentNormal, rotatedNormal, lastRotatedNormal = Vector3.Cross(normals [0], Vector3.forward);
            
            int normalsLength = normals.Length;
            int normalsLengthMinusOne = normalsLength - 1;
//            int _vertexCountMinuOne = _vertexCount - 1;

            for (i = 0; i < _vertexCount; i+= _tessellation)
            {                       
                positionIndex = i / _tessellation;
                
                currentPosition = positions [positionIndex];
                
                size = sizes [positionIndex] * 0.5f;
                if(useColors)
                    color = colors [positionIndex];

                currentNormal = normals [positionIndex];            
                rotatedNormal = Vector3.Cross(currentNormal, Vector3.forward);
                if (positionIndex < normalsLengthMinusOne)
                    lastRotatedNormal = Vector3.Cross(normals [positionIndex + 1], Vector3.forward);

                magnitude += magnitudes [positionIndex] / totalCurveLength;

                for (j = 0; j < _tessellation; j++)
                {
                    ij = i + j;
                    _tessellationProgress = (float)j / (float)(_tessellation - 1);              
                    _vertices [ij] = currentPosition + Vector3.Lerp(rotatedNormal, lastRotatedNormal, 0.5f).normalized * (-1f + _tessellationProgress * 2f) * size;                
                    if(useColors)
                        _colors [ij] = color;   

                    _uvs [ij].x = _tessellationProgress * lineUVScale.x + lineUVOffset.x;
                    _uvs [ij].y = magnitude * lineUVScale.y + lineUVOffset.y;
                    
                    if (i != 0 && j != 0)
                    {                   
                        _triangles [tl] = ij - _tessellation - 1;
                        _triangles [tl + 1] = ij - 1;
                        _triangles [tl + 2] = ij;
                        
                        _triangles [tl + 3] = ij - _tessellation - 1;
                        _triangles [tl + 4] = ij;
                        _triangles [tl + 5] = ij - _tessellation;
                        tl += 6;
                    }
                }   

                lastRotatedNormal = rotatedNormal;
            }

            if (closeLine)
            {
                int indexA, indexB;
                size = Mathf.Lerp(sizes [0], sizes [_positionsLength - 1], 0.5f) * 0.5f;
                Vector3 normal = Vector3.Cross(Vector3.Lerp(normals [0], normals [_positionsLength - 1], 0.5f).normalized, Vector3.forward);
                Vector3 position = Vector3.Lerp(positions [0], positions [_positionsLength - 1], 0.5f);
                Vector3 posA = position - normal * size;
                Vector3 posB = position + normal * size;

                float progress;
                float tesselationMinusOne = _tessellation - 1;
                for (i = 0; i < _tessellation; i++)
                {
                    progress = (float)i / tesselationMinusOne;

                    indexA = _vertexCount - _tessellation + i;
                    indexB = i;

                    _vertices [indexA] = _vertices [indexB] = Vector3.Lerp(posA, posB, progress);

                    if (useColors)
                        _colors [indexA] = _colors [indexB] = Color32.Lerp(_colors [indexA], _colors [indexB], .5f);
                }  
            } else
            {
				int indexA;//, indexB;
                size = sizes [_positionsLength - 1] * 0.5f;
                Vector3 normal = Vector3.Cross(normals [_positionsLength - 1], Vector3.forward);
                Vector3 position = positions [_positionsLength - 1];
                Vector3 posA = position - normal * size;
                Vector3 posB = position + normal * size;
                
                float progress;
                float tesselationMinusOne = _tessellation - 1;
                for (i = 0; i < _tessellation; i++)
                {
                    progress = (float)i / tesselationMinusOne;
                    
                    indexA = _vertexCount - _tessellation + i;                
                    _vertices [indexA] = Vector3.Lerp(posA, posB, progress);
                    
                    if (useColors)
                        _colors [indexA] = _colors [indexA];
                }  
            }

            Mesh mesh = new Mesh();
            mesh.vertices = _vertices;
            mesh.triangles = _triangles;
            mesh.uv = _uvs;
            if(useColors)
                mesh.colors32 = _colors;

            ResetLineSettings();
            return mesh;
        }

        public static bool VectorLine(Vector2[] positions, out SVGShape svgLayer, Color32 colorA, Color32 colorB, float size, float offset, ClosePathRule closeLine = ClosePathRule.NEVER)
        {
            svgLayer = new SVGShape();
            if (positions == null)
            {
#if DEBUG
                Debug.LogWarning("Line / Array is Null!");
#endif
                return false;
            }       
            
            if (positions.Length < 2)
            {
#if DEBUG
                Debug.LogWarning("Line / We need at least 2 positions!");
#endif
                return false;
            }

            if(positions.Length == 2)
            {
                closeLine = ClosePathRule.NEVER;
            } else {
                if(closeLine == ClosePathRule.AUTO)
                {
                    if(positions[0] == positions[positions.Length - 1]) closeLine = ClosePathRule.ALWAYS;
                } 
            }

            SVGLineData lineData = new SVGLineData(positions);
            lineData.UpdateAll();
                    
            size *= 0.5f;

            int _vertexCount = positions.Length * 2;
            int _positionsLength = positions.Length;
            int totalTriangles = (_positionsLength - 1) * (2 - 1) * 6;
            int positionIndex = 0, verticeIndex = 0;

            List<Vector2> _vertices = new List<Vector2>(_vertexCount);
            List<int> _triangles = new List<int>(totalTriangles);
            List<Color32> _colors = new List<Color32>(_vertexCount);
            List<Vector2> _angles = new List<Vector2>(_vertexCount);

            Vector2 previousPosition;
            Vector2 currentPosition;
            Vector2 nextPosition;

            Vector2 rotatedNormal, nextRotatedNormal;
            Vector2 normal, offsetNormal, intersection;

            int edgeCount = lineData.GetEdgeCount();
            int edgeCountMinusOne = edgeCount - 1;

            float finSize = size;

            for (positionIndex = 0; positionIndex <= edgeCount; positionIndex++)
            {   
                currentPosition = positions [positionIndex];

                if(positionIndex == 0)
                {
                    previousPosition = positions[0];
                    rotatedNormal = SVGMath.RotateVectorClockwise(lineData.GetNormal(0));
                    nextPosition = positions[1];
                    nextRotatedNormal = SVGMath.RotateVectorClockwise(lineData.GetNormal(0));
                } else if(positionIndex == edgeCount)
                {
                    previousPosition = positions[positionIndex - 1];
                    rotatedNormal = SVGMath.RotateVectorClockwise(lineData.GetNormal(positionIndex - 1));
                    if(closeLine == ClosePathRule.ALWAYS)
                    {
                        nextPosition = positions[0];
                        nextRotatedNormal = SVGMath.RotateVectorClockwise((positions[positions.Length-1] - positions[0]).normalized);
                    } else {
                        nextPosition = positions[positions.Length - 1];
                        nextRotatedNormal = SVGMath.RotateVectorClockwise(lineData.GetNormal(edgeCountMinusOne));
                    }
                } else {
                    previousPosition = positions[positionIndex - 1];
                    rotatedNormal = SVGMath.RotateVectorClockwise(lineData.GetNormal(positionIndex - 1));
                    nextRotatedNormal = SVGMath.RotateVectorClockwise(lineData.GetNormal(positionIndex));
                    nextPosition = positions[positionIndex + 1];
                }


                Vector2 startA = previousPosition + rotatedNormal * finSize + rotatedNormal * offset;
                Vector2 endA = currentPosition + rotatedNormal * finSize + rotatedNormal * offset;

                Vector2 startB = currentPosition + nextRotatedNormal * finSize + nextRotatedNormal * offset;
                Vector2 endB = nextPosition + nextRotatedNormal * finSize + nextRotatedNormal * offset;

                if(positionIndex == 0)
                {
                    Vector2[] finPos = new Vector2[]{   
                        currentPosition + rotatedNormal * -finSize + rotatedNormal * offset, 
                        currentPosition + rotatedNormal * finSize + rotatedNormal * offset,
                    };
                    _vertices.AddRange(finPos);
                    _colors.AddRange(new Color32[]{ colorA, colorB});
                    _angles.AddRange(new Vector2[]{ Vector2.zero, finPos[1] - finPos[0]});
                    verticeIndex += 2;
                } else {
                    bool intersect = SVGMath.LineLineIntersection(out intersection, startA, endA, startB, endB);
                    if(!intersect)
                    {
                        normal = Vector2.Lerp(rotatedNormal, nextRotatedNormal, 0.5f).normalized;                
                        offsetNormal = normal * offset;

                        if(positionIndex == edgeCount && closeLine != ClosePathRule.ALWAYS)
                        {
                            Vector2[] finPos = new Vector2[]{   
                                endA, 
                                currentPosition + normal * -finSize + offsetNormal,
                            };
                            _vertices.AddRange(finPos);
                            _colors.AddRange(new Color32[]{ colorB, colorA});
                            _angles.AddRange(new Vector2[]{ Vector2.zero,  Vector2.zero});
                            verticeIndex += 2;

                            _triangles.AddRange(new int[]{
                                verticeIndex - 4,
                                verticeIndex - 2,
                                verticeIndex - 1,

                                verticeIndex - 2,
                                verticeIndex - 4,
                                verticeIndex - 3,
                            });
                        } else {
                            Vector2[] finPos = new Vector2[]{   
                                endA, 
                                currentPosition + normal * -finSize + offsetNormal,
                                startB
                            };
                            _vertices.AddRange(finPos);
                            _colors.AddRange(new Color32[]{ colorB, colorA, colorB});
                            _angles.AddRange(new Vector2[]{ finPos[0] - currentPosition, Vector2.zero, finPos[2] - currentPosition });
                            verticeIndex += 3;

                            _triangles.AddRange(new int[]{
                                verticeIndex - 3,
                                verticeIndex - 2,
                                verticeIndex - 5,
                                
                                verticeIndex - 5,
                                verticeIndex - 4,
                                verticeIndex - 3,
                                
                                verticeIndex - 1,
                                verticeIndex - 2,
                                verticeIndex - 3,
                            }); 
                        }
                    } else {
                        normal = Vector2.Lerp(rotatedNormal, nextRotatedNormal, 0.5f).normalized;                
                        offsetNormal = normal * offset;

                        Vector2[] finPos = new Vector2[]{currentPosition + normal * -finSize + offsetNormal, intersection };
                        _vertices.AddRange(finPos);
                        _colors.AddRange(new Color32[]{ colorA, colorB});
                        _angles.AddRange(new Vector2[]{ Vector2.zero, finPos[1] - finPos[0]});
                        verticeIndex += 2;

                        _triangles.AddRange(new int[]{
                            verticeIndex - 4,
                            verticeIndex - 2,
                            verticeIndex - 1,
                            
                            verticeIndex - 4,
                            verticeIndex - 1,
                            verticeIndex - 3
                        });                       
                    }
                }
            }

            if(closeLine == ClosePathRule.ALWAYS)
            {                
                previousPosition = positions[positions.Length - 1];
                currentPosition = positions [0];
                nextPosition = positions [1];

                rotatedNormal = SVGMath.RotateVectorClockwise(
                    (previousPosition - currentPosition).normalized
                    );
                nextRotatedNormal = SVGMath.RotateVectorClockwise(lineData.GetNormal(0));

                Vector2 startA = previousPosition + rotatedNormal * finSize + rotatedNormal * offset;
                Vector2 endA = currentPosition + rotatedNormal * finSize + rotatedNormal * offset;
                
                Vector2 startB = currentPosition + nextRotatedNormal * finSize + nextRotatedNormal * offset;
                Vector2 endB = nextPosition + nextRotatedNormal * finSize + nextRotatedNormal * offset;

                bool intersect = SVGMath.LineLineIntersection(out intersection, startA, endA, startB, endB);

                if(!intersect)
                {
                    normal = Vector2.Lerp(rotatedNormal, nextRotatedNormal, 0.5f).normalized;                
                    offsetNormal = normal * offset;
                    Vector2[] finPos = new Vector2[]{   
                        endA, 
                        currentPosition + normal * -finSize + offsetNormal,
                        startB
                    };
                    
                    _vertices.AddRange(finPos);
                    _colors.AddRange(new Color32[]{colorB, colorA, colorB});
                    _angles.AddRange(new Vector2[]{ finPos[0] - finPos[1], Vector2.zero, finPos[2] - finPos[1] });
                    verticeIndex += 3;
                    
                    if (positionIndex != 0)
                    {
                        _triangles.AddRange(new int[]{
                            verticeIndex - 3,
                            verticeIndex - 2,
                            verticeIndex - 5,
                            
                            verticeIndex - 5,
                            verticeIndex - 4,
                            verticeIndex - 3,
                            
                            verticeIndex - 1,
                            verticeIndex - 2,
                            verticeIndex - 3,
                        });
                    }
                } else {
                    normal = Vector2.Lerp(rotatedNormal, nextRotatedNormal, 0.5f).normalized;                
                    offsetNormal = normal * offset;
                    Vector2[] finPos = new Vector2[]{   currentPosition + normal * -finSize + offsetNormal, intersection };
                    _vertices.AddRange(finPos);
                    _colors.AddRange(new Color32[]{colorA, colorB});
                    _angles.AddRange(new Vector2[]{Vector2.zero, finPos[1] - finPos[0]});
                    verticeIndex += 2;
                    
                    if (positionIndex != 0)
                    {
                        _triangles.AddRange(new int[]{
                            verticeIndex - 4,
                            verticeIndex - 2,
                            verticeIndex - 1,
                            
                            verticeIndex - 4,
                            verticeIndex - 1,
                            verticeIndex - 3
                        });
                    }  
                }

                _vertices[1] = _vertices[_vertices.Count - 1];
                _vertices[0] = _vertices[_vertices.Count - 2];
            }
            
            for(int i = 0; i < _angles.Count; i++)
            {
                _vertices[i] -= _angles[i];
                _angles[i].Normalize();
                _angles[i] = new Vector2(_angles[i].x, _angles[i].y * -1f);
            }
            
            svgLayer.vertices = _vertices.ToArray();
            svgLayer.triangles = _triangles.ToArray();
            svgLayer.colors = _colors.ToArray();
            svgLayer.angles = _angles.ToArray();
            svgLayer.RecalculateBounds();

            return true;
        }
    	
    	public static void ChangeMeshUV2 (Mesh mesh, Vector2 uv)
    	{
            int length = mesh.vertices.Length;
            Vector2[] uvs = new Vector2[length];
    		for (int i = 0; i < length; i++) {
    			uvs [i].x = uv.x;
    			uvs [i].y = uv.y;
    		}
    		mesh.uv2 = uvs;
    	}
    	
    	public static void ChangeMeshColor (Mesh mesh, Color32 color)
    	{
    		Color32[] colors = mesh.colors32;
    		int length = mesh.vertexCount;
    		if (colors.Length != length)
    			colors = new Color32[length];
    		
    		for (int i = 0; i < length; i++) {
    			colors [i].r = color.r;
    			colors [i].g = color.g;
    			colors [i].b = color.b;
    			colors [i].a = color.a;
    		}
    		mesh.colors32 = colors;
    	}
    	
    	public static void ChangeMeshColor (Mesh mesh, ColorChannel channel, byte value)
    	{
    		Color32[] colors = mesh.colors32;
    		int length = mesh.vertexCount;
    		if (colors.Length != length)
    			colors = new Color32[length];
    		
    		switch (channel) {
    		case ColorChannel.RED:
    			for (int i = 0; i < length; i++) {
    				colors [i].r = value;
    			}
    			break;
    		case ColorChannel.GREEN:
    			for (int i = 0; i < length; i++) {
    				colors [i].g = value;
    			}
    			break;	
    		case ColorChannel.BLUE:
    			for (int i = 0; i < length; i++) {
    				colors [i].b = value;
    			}
    			break;
    		case ColorChannel.ALPHA:
    			for (int i = 0; i < length; i++) {
    				colors [i].a = value;
    			}
    			break;	
    		}		
    		mesh.colors32 = colors;
    	}
    	
    	public static void ChangeMeshColor (Mesh mesh, ColorChannel channel, float value)
    	{
    		ChangeMeshColor (mesh, channel, (byte)Mathf.RoundToInt (Mathf.Lerp (0, 255, value)));
    	}
    	
    	public static void ChangeMeshColor (Mesh mesh, Color color)
    	{
    		ChangeMeshColor (mesh, (Color32)color);
    	}
        
        public static void Fill(Mesh source, Mesh destination)
        {
            if(destination == null) return;
            if(source == null) return;

            destination.name = source.name;

            destination.vertices = (Vector3[])source.vertices.Clone();
            destination.triangles = (int[])source.triangles.Clone();
            
            Color32[] colors32 = source.colors32;
            if(colors32 != null && colors32.Length > 0) destination.colors32 = (Color32[])colors32.Clone();
            
            Vector2[] uv = source.uv;
            if(uv != null && uv.Length > 0) destination.uv = (Vector2[])uv.Clone();
            
            Vector2[] uv2 = source.uv2;
            if(uv2 != null && uv2.Length > 0) destination.uv2 = (Vector2[])uv2.Clone();

            Vector2[] uv3 = source.uv3;
            if(uv3 != null && uv3.Length > 0) destination.uv3 = (Vector2[])uv3.Clone();
            
            Vector2[] uv4 = source.uv4;
            if(uv4 != null && uv4.Length > 0) destination.uv4 = (Vector2[])uv4.Clone();
            
            Vector3[] normals = source.normals;
            if(normals != null && normals.Length > 0) destination.normals = (Vector3[])normals.Clone();
            
            Vector4[] tangents = source.tangents;
            if(tangents != null && tangents.Length > 0) destination.tangents = (Vector4[])tangents.Clone();

            int subMeshCount = source.subMeshCount;
            destination.subMeshCount = subMeshCount;
            if(subMeshCount > 0)
            {
                for(int i = 0; i < subMeshCount; i++)
                {
                    destination.SetTriangles(source.GetTriangles(i), i);
                }
            }
            
            destination.bounds = source.bounds;
        }

        public static Mesh Clone(Mesh mesh)
        {
            if(mesh == null)
                return null;

            Mesh output = new Mesh();
            output.name = mesh.name;
            Fill(mesh, output);
            return output;
        }

        public static Material CloneMaterial(Material original)
        {
            if(original == null) return null;
            Material material = new Material(original.shader);
            material.CopyPropertiesFromMaterial(original);
            return material;
        }
    }
}
