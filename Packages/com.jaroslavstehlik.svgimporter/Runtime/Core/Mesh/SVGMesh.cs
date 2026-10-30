

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Geometry
{
    using Rendering;
    using Data;
    using Utils;
    
    /// <summary>
    /// Arrays SVGMesh.CombineMeshes reuses between builds of the same mesh, so that renderers
    /// rebuilding their mesh every frame for modifiers don't allocate.
    /// </summary>
    internal sealed class SVGMeshBuffers
    {
        internal Vector3[] vertices, normals;
        internal Color32[] colors;
        internal Vector2[] uv, uv2;
        internal int[] opaqueTriangles, transparentTriangles;
        internal float[] depths;
        internal readonly List<string> shaderNames = new List<string>(2);
        internal string[] shaders;

        // An array of at least length elements, the first length of them cleared if asked.
        internal static T[] Ensure<T>(ref T[] array, int length, bool clear)
        {
            if(array == null || array.Length < length) array = new T[length];
            else if(clear) System.Array.Clear(array, 0, length);
            return array;
        }

        // The shader names as an array, the previous one while they are the same.
        internal string[] Shaders()
        {
            if(shaders == null || shaders.Length != shaderNames.Count) return shaders = shaderNames.ToArray();
            for(int i = 0; i < shaders.Length; i++)
                if(shaders[i] != shaderNames[i]) return shaders = shaderNames.ToArray();
            return shaders;
        }
    }

    internal class SVGMesh
    {
        static readonly string[] s_NoShaders = new string[0];
        public static bool CombineMeshes(SVGLayer[] layers, Mesh mesh, out Shader[] shaders, SVGUseGradients useGradients = SVGUseGradients.Always, SVGAssetFormat format = SVGAssetFormat.Transparent, bool compressDepth = true, bool antialiased = false, float depthOffset = 0.01f)
        {
            string[] shaderNames;
            bool combined = CombineMeshes(layers, mesh, out shaderNames, useGradients, format, compressDepth, antialiased, depthOffset);
            shaders = System.Array.ConvertAll(shaderNames, Shader.Find);
            return combined;
        }

        /// <summary>
        /// As above, with the shader of each submesh by name (SVGShader's name constants). Buffers
        /// kept from a previous build of the mesh are reused; the shader names array then too.
        /// </summary>
        internal static bool CombineMeshes(SVGLayer[] layers, Mesh mesh, out string[] shaders, SVGUseGradients useGradients, SVGAssetFormat format, bool compressDepth, bool antialiased, float depthOffset, SVGMeshBuffers buffers = null)
        {
            #if DEBUG_IMPORT
            long timeStart = System.DateTime.Now.Ticks;
            #endif
            
            shaders = s_NoShaders;
            
            
            SVGFill fill;
            bool useOpaqueShader = false;
            bool useTransparentShader = false;
            bool hasGradients = (useGradients == SVGUseGradients.Always);
            
            if(layers == null) return false;
            int totalLayers = layers.Length, totalTriangles = 0, opaqueTriangles = 0, transparentTriangles = 0;
            SVGFillBlend lastBlendType = SVGFillBlend.Transparent;
            
            // Depth of each shape (Opaque format), numbered across all layers in drawing order. Kept
            // here: the layers belong to the caller and are drawn again later.
            int shapeCount = 0;
            for(int i = 0; i < totalLayers; i++) shapeCount += layers[i].shapes.Length;
            SVGMeshBuffers b = buffers ?? new SVGMeshBuffers();
            float[] depths = SVGMeshBuffers.Ensure(ref b.depths, shapeCount, false);
            if(format == SVGAssetFormat.OpaqueAndTransparent)
            {
                if(compressDepth)
                {
                    // Shapes that overlap nothing drawn before them share depth 0; otherwise a shape
                    // goes in front of the highest one it overlaps.
                    SVGBounds quadTreeBounds = SVGBounds.InfiniteInverse;
                    for(int i = 0; i < totalLayers; i++)
                    {
                        foreach(SVGShape shape in layers[i].shapes)
                        {
                            if (shape.bounds.size.sqrMagnitude == 0f) continue;
                            quadTreeBounds.Encapsulate(shape.bounds.center, shape.bounds.size);
                        }
                    }

                    if(!quadTreeBounds.isInfiniteInverse)
                    {
                        quadTreeBounds.size *= 1.2f;
                        SVGDepthTree depthTree = new SVGDepthTree(quadTreeBounds);
                        SVGFillBlend[] blends = new SVGFillBlend[shapeCount];
                        int index = 0;
                        for(int i = 0; i < totalLayers; i++)
                        {
                            foreach(SVGShape shape in layers[i].shapes)
                            {
                                blends[index] = shape.fill.blend;
                                int[] overlapping = depthTree.TestDepthAdd(index, new SVGBounds(shape.bounds.center, shape.bounds.size));
                                float depth = 0;
                                if(overlapping != null && overlapping.Length > 0)
                                {
                                    float highestDepth = 0;
                                    int highest = -1;
                                    foreach(int other in overlapping)
                                    {
                                        if(depths[other] > highestDepth)
                                        {
                                            highestDepth = depths[other];
                                            highest = other;
                                        }
                                    }
                                    bool opaque = shape.fill.blend == SVGFillBlend.Opaque || (highest != -1 && blends[highest] == SVGFillBlend.Opaque);
                                    depth = opaque ? highestDepth + 1 : highestDepth;
                                }
                                depths[index++] = depth;
                            }
                        }
                    }
                } else {
                    int highestDepth = 0, index = 0;
                    for(int i = 0; i < totalLayers; i++)
                    {
                        foreach(SVGShape shape in layers[i].shapes)
                        {
                            fill = shape.fill;
                            depths[index++] = fill.blend == SVGFillBlend.Opaque || lastBlendType == SVGFillBlend.Opaque ? ++highestDepth : highestDepth;
                            lastBlendType = fill.blend;
                        }
                    }
                }
            } else {
                int index = 0;
                for(int i = 0; i < totalLayers; i++)
                    foreach(SVGShape shape in layers[i].shapes)
                        depths[index++] = shape.depth;
            }
            
            int totalVertices = 0, vertexCount, vertexStart = 0, currentVertex;
            for(int i = 0; i < totalLayers; i++)
            {
                int totalShapes = layers[i].shapes.Length;
                for(int j = 0; j < totalShapes; j++)
                {
                    fill = layers[i].shapes[j].fill;
                    if(fill.blend == SVGFillBlend.Opaque) { 
                        opaqueTriangles += layers[i].shapes[j].triangles.Length;
                        useOpaqueShader = true; 
                    }
                    else if(fill.blend == SVGFillBlend.Transparent) { 
                        transparentTriangles += layers[i].shapes[j].triangles.Length;
                        useTransparentShader = true; 
                    }
                    if(fill.fillType == SVGFillType.Gradient) hasGradients = true;
                    vertexCount = layers[i].shapes[j].vertices.Length;
                    totalVertices += vertexCount;
                }
            }
            
            totalTriangles = opaqueTriangles + transparentTriangles;
            
            if(useGradients == SVGUseGradients.Never) hasGradients = false;
            if(format != SVGAssetFormat.OpaqueAndTransparent)
            { 
                useOpaqueShader = false; 
                useTransparentShader = true;
            }
            
            // Every vertex gets a position and a colour; UVs and normals only some, the rest zero.
            Vector3[] vertices = SVGMeshBuffers.Ensure(ref b.vertices, totalVertices, false);
            Color32[] colors32 = SVGMeshBuffers.Ensure(ref b.colors, totalVertices, false);
            Vector2[] uv = null;
            Vector2[] uv2 = null;
            Vector3[] normals = null;
            int[] opaqueIndices, transparentIndices = null;
            List<string> outputShaders = b.shaderNames;
            outputShaders.Clear();

            if(antialiased) normals = SVGMeshBuffers.Ensure(ref b.normals, totalVertices, true);

            if(hasGradients)
            {
                uv = SVGMeshBuffers.Ensure(ref b.uv, totalVertices, true);
                uv2 = SVGMeshBuffers.Ensure(ref b.uv2, totalVertices, true);
                
                if(useOpaqueShader)
                {
                    outputShaders.Add(SVGShader.GradientColorOpaqueName);
                }
                if(useTransparentShader)
                {
                    if(antialiased)
                    {
                        outputShaders.Add(SVGShader.GradientColorAlphaBlendedAntialiasedName);
                    } else {
                        outputShaders.Add(SVGShader.GradientColorAlphaBlendedName);
                    }
                }
            } else {
                if(useOpaqueShader)
                {
                    outputShaders.Add(SVGShader.SolidColorOpaqueName);
                }
                if(useTransparentShader)
                {
                    if(antialiased)
                    {
                        outputShaders.Add(SVGShader.SolidColorAlphaBlendedAntialiasedName);
                    } else {
                        outputShaders.Add(SVGShader.SolidColorAlphaBlendedName);
                    }
                }
            }
            
            int shapeIndex = 0;
            for(int i = 0; i < totalLayers; i++)
            {
                int totalShapes = layers[i].shapes.Length;
                for(int j = 0; j < totalShapes; j++)
                {
                    vertexCount = layers[i].shapes[j].vertices.Length;
                    float shapeDepth = depths[shapeIndex++];
                    
                    if(layers[i].shapes[j].colors != null && layers[i].shapes[j].colors.Length == vertexCount)
                    {
                        Color32 finalColor = layers[i].shapes[j].fill.finalColor;
                        for(int k = 0; k < vertexCount; k++)
                        {
                            currentVertex = vertexStart + k;
                            vertices[currentVertex] = layers[i].shapes[j].vertices[k];
                            vertices[currentVertex].z = useOpaqueShader ? shapeDepth * -depthOffset : shapeDepth;
                            colors32[currentVertex].r = (byte)(finalColor.r * layers[i].shapes[j].colors[k].r / 255);
                            colors32[currentVertex].g = (byte)(finalColor.g * layers[i].shapes[j].colors[k].g / 255);
                            colors32[currentVertex].b = (byte)(finalColor.b * layers[i].shapes[j].colors[k].b / 255);
                            colors32[currentVertex].a = (byte)(finalColor.a * layers[i].shapes[j].colors[k].a / 255);
                        }
                    } else {
                        Color32 finalColor = layers[i].shapes[j].fill.finalColor;
                        for(int k = 0; k < vertexCount; k++)
                        {
                            currentVertex = vertexStart + k;
                            vertices[currentVertex] = layers[i].shapes[j].vertices[k];
                            vertices[currentVertex].z = useOpaqueShader ? shapeDepth * -depthOffset : shapeDepth;
                            colors32[currentVertex] = finalColor;
                        }
                    }
                    
                    if(hasGradients)
                    {
                        if(layers[i].shapes[j].fill.fillType == SVGFillType.Gradient && layers[i].shapes[j].fill.gradientColors != null)
                        {
                            SVGMatrix svgFillTransform = layers[i].shapes[j].fill.transform;
                            Rect viewport = layers[i].shapes[j].fill.viewport;
                            
                            Vector2 uvPoint = Vector2.zero;
                            Vector2 uv2Value = new Vector2(layers[i].shapes[j].fill.gradientColors.index, (int)layers[i].shapes[j].fill.gradientType);
                            
                            if(normals != null && layers[i].shapes[j].angles != null && layers[i].shapes[j].angles.Length == vertexCount)
                            {
                                for(int k = 0; k < vertexCount; k++)
                                {
                                    currentVertex = vertexStart + k;
                                    uvPoint.x = vertices [currentVertex].x;
                                    uvPoint.y = vertices [currentVertex].y;
                                    uvPoint = svgFillTransform.Transform(uvPoint);
                                    uv[currentVertex].x = (uvPoint.x - viewport.x) / viewport.width;
                                    uv[currentVertex].y = (uvPoint.y - viewport.y) / viewport.height;
                                    uv2[currentVertex] = uv2Value;
                                    normals[currentVertex].x = layers[i].shapes[j].angles[k].x;
                                    normals[currentVertex].y = layers[i].shapes[j].angles[k].y;
                                }
                            } else {
                                for(int k = 0; k < vertexCount; k++)
                                {
                                    currentVertex = vertexStart + k;
                                    uvPoint.x = vertices [currentVertex].x;
                                    uvPoint.y = vertices [currentVertex].y;
                                    uvPoint = svgFillTransform.Transform(uvPoint);
                                    uv[currentVertex].x = (uvPoint.x - viewport.x) / viewport.width;
                                    uv[currentVertex].y = (uvPoint.y - viewport.y) / viewport.height;
                                    uv2[currentVertex] = uv2Value;
                                }
                            }
                        } else if(layers[i].shapes[j].fill.fillType == SVGFillType.Texture)
                        {
                            SVGMatrix svgFillTransform = layers[i].shapes[j].fill.transform;
                            Vector2 uvPoint = Vector2.zero;
                            if(normals != null && layers[i].shapes[j].angles != null && layers[i].shapes[j].angles.Length == vertexCount)
                            {
                                for(int k = 0; k < vertexCount; k++)
                                {
                                    currentVertex = vertexStart + k;
                                    uvPoint.x = vertices [currentVertex].x;
                                    uvPoint.y = vertices [currentVertex].y;
                                    uv[currentVertex] = svgFillTransform.Transform(uvPoint);
                                    normals[currentVertex].x = layers[i].shapes[j].angles[k].x;
                                    normals[currentVertex].y = layers[i].shapes[j].angles[k].y;
                                }
                            } else {
                                for(int k = 0; k < vertexCount; k++)
                                {
                                    currentVertex = vertexStart + k;
                                    uvPoint.x = vertices [currentVertex].x;
                                    uvPoint.y = vertices [currentVertex].y;
                                    uv[currentVertex] = svgFillTransform.Transform(uvPoint);
                                }
                            }
                        } else {
                            if(normals != null && layers[i].shapes[j].angles != null && layers[i].shapes[j].angles.Length == vertexCount)
                            {
                                for(int k = 0; k < vertexCount; k++)
                                {
                                    currentVertex = vertexStart + k;
                                    normals[currentVertex].x = layers[i].shapes[j].angles[k].x;
                                    normals[currentVertex].y = layers[i].shapes[j].angles[k].y;
                                }
                            }
                        }
                    } else {                    
                        if(antialiased)
                        {
                            if(layers[i].shapes[j].angles != null && layers[i].shapes[j].angles.Length == vertexCount)
                            {
                                for(int k = 0; k < vertexCount; k++)
                                {
                                    currentVertex = vertexStart + k;
                                    normals[currentVertex] = layers[i].shapes[j].angles[k];
                                }
                            }
                        }
                    }
                    vertexStart += vertexCount;
                }
            }
            
            // Submesh Order
            if(useOpaqueShader && useTransparentShader)
            {
                opaqueIndices = SVGMeshBuffers.Ensure(ref b.opaqueTriangles, opaqueTriangles, false);
                transparentIndices = SVGMeshBuffers.Ensure(ref b.transparentTriangles, transparentTriangles, false);
                
                int lastVertexIndex = 0;
                int triangleCount;
                int lastOpauqeTriangleIndex = 0;
                int lastTransparentTriangleIndex = 0;
                
                for(int i = 0; i < totalLayers; i++)
                {
                    int totalShapes = layers[i].shapes.Length;
                    for(int j = 0; j < totalShapes; j++)
                    {
                        triangleCount = layers[i].shapes[j].triangles.Length;
                        if(layers[i].shapes[j].fill.blend == SVGFillBlend.Opaque)
                        {
                            for(int k = 0; k < triangleCount; k++)
                            {
                                opaqueIndices[lastOpauqeTriangleIndex++] = lastVertexIndex + layers[i].shapes[j].triangles[k];
                            }
                        } else {
                            for(int k = 0; k < triangleCount; k++)
                            {
                                transparentIndices[lastTransparentTriangleIndex++] = lastVertexIndex + layers[i].shapes[j].triangles[k];
                            }
                        }
                        
                        lastVertexIndex += layers[i].shapes[j].vertices.Length;
                    }
                }
            } else {
                opaqueIndices = SVGMeshBuffers.Ensure(ref b.opaqueTriangles, totalTriangles, false);
                
                int lastVertexIndex = 0;
                int triangleCount;
                int lastTriangleIndex = 0;
                
                for(int i = 0; i < totalLayers; i++)
                {
                    int totalShapes = layers[i].shapes.Length;
                    for(int j = 0; j < totalShapes; j++)
                    {
                        triangleCount = layers[i].shapes[j].triangles.Length;
                        for(int k = 0; k < triangleCount; k++)
                        {
                            opaqueIndices[lastTriangleIndex++] = lastVertexIndex + layers[i].shapes[j].triangles[k];
                        }
                        lastVertexIndex += layers[i].shapes[j].vertices.Length;
                    }
                }
            }
            
            shaders = buffers != null ? b.Shaders() : outputShaders.ToArray();
            
            /* * * * * * * * * * * * * * * * * * * * * * * * 
             *                                             *
             *      Mesh Creation                          *
             *                                             *
             * * * * * * * * * * * * * * * * * * * * * * * */
            
            mesh.Clear();
            mesh.MarkDynamic();
            // 16-bit indices address 65,535 vertices; larger meshes need 32-bit indices.
            mesh.indexFormat = totalVertices > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices, 0, totalVertices);
            mesh.SetColors(colors32, 0, totalVertices);
            if(uv != null) mesh.SetUVs(0, uv, 0, totalVertices);
            if(uv2 != null) mesh.SetUVs(1, uv2, 0, totalVertices);
            if(normals != null) mesh.SetNormals(normals, 0, totalVertices);

            if(transparentIndices == null)
            {
                mesh.SetTriangles(opaqueIndices, 0, totalTriangles, 0);
            } else {
                mesh.subMeshCount = 2;
                mesh.SetTriangles(opaqueIndices, 0, opaqueTriangles, 0);
                mesh.SetTriangles(transparentIndices, 0, transparentTriangles, 1);
            }
            
            #if DEBUG_IMPORT
            System.TimeSpan timeSpan = new System.TimeSpan(System.DateTime.Now.Ticks - timeStart);
            Debug.Log("Mesh combination took: "+timeSpan.TotalSeconds +"s");
            #endif
            
            return true;
        }
    }
}
