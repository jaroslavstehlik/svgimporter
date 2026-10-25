

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Geometry
{
    using Rendering;
    using Data;
    using Utils;
    
    public class SVGMesh
    {
        public static bool CombineMeshes(SVGLayer[] layers, Mesh mesh, out Shader[] shaders, SVGUseGradients useGradients = SVGUseGradients.Always, SVGAssetFormat format = SVGAssetFormat.Transparent, bool compressDepth = true, bool antialiased = false, float depthOffset = 0.01f)
        {
            string[] shaderNames;
            bool combined = CombineMeshes(layers, mesh, out shaderNames, useGradients, format, compressDepth, antialiased, depthOffset);
            shaders = System.Array.ConvertAll(shaderNames, Shader.Find);
            return combined;
        }

        /// <summary>As above, with the shader of each submesh by name (SVGShader's name constants).</summary>
        internal static bool CombineMeshes(SVGLayer[] layers, Mesh mesh, out string[] shaders, SVGUseGradients useGradients, SVGAssetFormat format, bool compressDepth, bool antialiased, float depthOffset)
        {
            #if DEBUG_IMPORT
            long timeStart = System.DateTime.Now.Ticks;
            #endif
            
            shaders = new string[0];
            
            //if(SVGAssetImport.sliceMesh) Create9Slice();
            
            SVGFill fill;
            bool useOpaqueShader = false;
            bool useTransparentShader = false;
            bool hasGradients = (useGradients == SVGUseGradients.Always);
            
            if(layers == null) return false;
            int totalLayers = layers.Length, totalTriangles = 0, opaqueTriangles = 0, transparentTriangles = 0;
            FILL_BLEND lastBlendType = FILL_BLEND.ALPHA_BLENDED;
            
            // Depth of each shape (Opaque format), numbered across all layers in drawing order. Kept
            // here: the layers belong to the caller and are drawn again later.
            int shapeCount = 0;
            for(int i = 0; i < totalLayers; i++) shapeCount += layers[i].shapes.Length;
            float[] depths = new float[shapeCount];
            if(format == SVGAssetFormat.Opaque)
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
                        FILL_BLEND[] blends = new FILL_BLEND[shapeCount];
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
                                    bool opaque = shape.fill.blend == FILL_BLEND.OPAQUE || (highest != -1 && blends[highest] == FILL_BLEND.OPAQUE);
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
                            depths[index++] = fill.blend == FILL_BLEND.OPAQUE || lastBlendType == FILL_BLEND.OPAQUE ? ++highestDepth : highestDepth;
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
                    if(fill.blend == FILL_BLEND.OPAQUE) { 
                        opaqueTriangles += layers[i].shapes[j].triangles.Length;
                        useOpaqueShader = true; 
                    }
                    else if(fill.blend == FILL_BLEND.ALPHA_BLENDED) { 
                        transparentTriangles += layers[i].shapes[j].triangles.Length;
                        useTransparentShader = true; 
                    }
                    if(fill.fillType == FILL_TYPE.GRADIENT) hasGradients = true;
                    vertexCount = layers[i].shapes[j].vertices.Length;
                    totalVertices += vertexCount;
                }
            }
            
            totalTriangles = opaqueTriangles + transparentTriangles;
            
            if(useGradients == SVGUseGradients.Never) hasGradients = false;
            if(format != SVGAssetFormat.Opaque)
            { 
                useOpaqueShader = false; 
                useTransparentShader = true;
            }
            
            Vector3[] vertices = new Vector3[totalVertices];
            Color32[] colors32 = new Color32[totalVertices];
            Vector2[] uv = null;
            Vector2[] uv2 = null;
            Vector3[] normals = null;
            int[][] triangles = null;
            List<string> outputShaders = new List<string>();

            if(antialiased) normals = new Vector3[totalVertices];

            if(hasGradients)
            {
                uv = new Vector2[totalVertices];
                uv2 = new Vector2[totalVertices];
                
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
                        if(layers[i].shapes[j].fill.fillType == FILL_TYPE.GRADIENT && layers[i].shapes[j].fill.gradientColors != null)
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
                        } else if(layers[i].shapes[j].fill.fillType == FILL_TYPE.TEXTURE)
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
                triangles = new int[2][]{new int[opaqueTriangles], new int[transparentTriangles]};
                
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
                        if(layers[i].shapes[j].fill.blend == FILL_BLEND.OPAQUE)
                        {
                            for(int k = 0; k < triangleCount; k++)
                            {
                                triangles[0][lastOpauqeTriangleIndex++] = lastVertexIndex + layers[i].shapes[j].triangles[k];
                            }
                        } else {
                            for(int k = 0; k < triangleCount; k++)
                            {
                                triangles[1][lastTransparentTriangleIndex++] = lastVertexIndex + layers[i].shapes[j].triangles[k];
                            }
                        }
                        
                        lastVertexIndex += layers[i].shapes[j].vertices.Length;
                    }
                }
            } else {
                triangles = new int[1][]{new int[totalTriangles]};
                
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
                            triangles[0][lastTriangleIndex++] = lastVertexIndex + layers[i].shapes[j].triangles[k];
                        }
                        lastVertexIndex += layers[i].shapes[j].vertices.Length;
                    }
                }
            }
            
            if(outputShaders.Count != 0) shaders = outputShaders.ToArray();
            
            /* * * * * * * * * * * * * * * * * * * * * * * * 
             *                                             *
             *      Mesh Creation                          *
             *                                             *
             * * * * * * * * * * * * * * * * * * * * * * * */
            
            mesh.Clear();
            mesh.MarkDynamic();
            // 16-bit indices address 65,535 vertices; larger meshes need 32-bit indices.
            mesh.indexFormat = vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.colors32 = colors32;
            
            if(uv != null)
            {
                mesh.uv = uv;
            }

            if(uv2 != null)
            {
                mesh.uv2 = uv2;
            }

            if(normals != null)
            {
                mesh.normals = normals;
            }

            if(triangles.Length == 1)
            {
                mesh.triangles = triangles[0];
            } else {
                mesh.subMeshCount = triangles.Length;
                for(int i = 0; i < triangles.Length; i++)
                {
                    mesh.SetTriangles(triangles[i], i);
                }
            }
            
            #if DEBUG_IMPORT
            System.TimeSpan timeSpan = new System.TimeSpan(System.DateTime.Now.Ticks - timeStart);
            Debug.Log("Mesh combination took: "+timeSpan.TotalSeconds +"s");
            #endif
            
            return true;
        }
    }
}
