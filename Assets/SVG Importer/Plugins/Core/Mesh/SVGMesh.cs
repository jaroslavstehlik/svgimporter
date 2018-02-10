//#define DEBUG_IMPORT

// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
        public static bool CombineMeshes(SVGLayer[] layers, Mesh mesh, out Shader[] shaders, SVGUseGradients useGradients = SVGUseGradients.Always, SVGAssetFormat format = SVGAssetFormat.Transparent, bool compressDepth = true, bool antialiased = false)
        {
            #if DEBUG_IMPORT
            long timeStart = System.DateTime.Now.Ticks;
            #endif
            
            shaders = new Shader[0];
            
            //if(SVGAssetImport.sliceMesh) Create9Slice();
            
            SVGFill fill;
            bool useOpaqueShader = false;
            bool useTransparentShader = false;
            bool hasGradients = (useGradients == SVGUseGradients.Always);
            
            if(layers == null) return false;
            int totalLayers = layers.Length, totalTriangles = 0, opaqueTriangles = 0, transparentTriangles = 0;
            FILL_BLEND lastBlendType = FILL_BLEND.ALPHA_BLENDED;
            
            // Z Sort meshes
            if(format == SVGAssetFormat.Opaque)
            {
                SVGShape shape;
                if(compressDepth)
                {
                    SVGBounds quadTreeBounds = SVGBounds.InfiniteInverse;
                    for(int i = 0; i < totalLayers; i++)
                    {
                        int totalShapes = layers[i].shapes.Length;
                        for (int j = 0; j < totalShapes; j++)
                        {
                            shape = layers[i].shapes[j];
                            if (shape.bounds.size.sqrMagnitude == 0f) continue;
                            quadTreeBounds.Encapsulate(shape.bounds.center, shape.bounds.size);
                        }
                    }
                    
                    quadTreeBounds.size *= 1.2f;
                    
                    if(!quadTreeBounds.isInfiniteInverse)
                    {
                        SVGDepthTree depthTree = new SVGDepthTree(quadTreeBounds);
                        for(int i = 0; i < totalLayers; i++)
                        {
                            int totalShapes = layers[i].shapes.Length;
                            for (int j = 0; j < totalShapes; j++)
                            {                           
                                shape = layers[i].shapes[j];
                                int[] nodes = depthTree.TestDepthAdd(j, new SVGBounds(shape.bounds.center, shape.bounds.size));
                                
                                int nodesLength = 0;
                                if(nodes == null || nodes.Length == 0)
                                {
                                    shape.depth = 0;
                                } else {
                                    nodesLength = nodes.Length;
                                    int highestDepth = 0;
                                    int highestLayer = -1;
                                    
                                    for(int k = 0; k < nodesLength; k++)
                                    {
                                        if((int)layers[i].shapes[nodes[k]].depth > highestDepth)
                                        {
                                            highestDepth = (int)layers[i].shapes[nodes[k]].depth;
                                            highestLayer = nodes[k];
                                        }
                                    }
                                    
                                    if(layers[i].shapes[j].fill.blend == FILL_BLEND.OPAQUE)
                                    {
                                        shape.depth = highestDepth + 1;
                                    } else 
                                    {
                                        if(highestLayer != -1 && layers[i].shapes[highestLayer].fill.blend == FILL_BLEND.OPAQUE)
                                        {
                                            shape.depth = highestDepth + 1;
                                        } else {
                                            shape.depth = highestDepth;
                                        }
                                    }
                                }
                                layers[i].shapes[j] = shape;
                            }
                        }
                    }
                } else {
                    int highestDepth = 0;
                    for(int i = 0; i < totalLayers; i++)
                    {
                        int totalShapes = layers[i].shapes.Length;
                        for (int j = 0; j < totalShapes; j++)
                        {
                            shape = layers[i].shapes[j];
                            fill = shape.fill;
                            if (fill.blend == FILL_BLEND.OPAQUE || lastBlendType == FILL_BLEND.OPAQUE)
                            {
                                shape.depth = ++highestDepth;
                            } else 
                            {
                                shape.depth = highestDepth;
                            }
                            
                            lastBlendType = fill.blend;
                            layers[i].shapes[j] = shape;
                        }
                    }
                }
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
            List<Shader> outputShaders = new List<Shader>();

            if(antialiased) normals = new Vector3[totalVertices];

            if(hasGradients)
            {
                uv = new Vector2[totalVertices];
                uv2 = new Vector2[totalVertices];
                
                if(useOpaqueShader)
                {
                    outputShaders.Add(SVGShader.GradientColorOpaque);
                }
                if(useTransparentShader)
                {
                    if(antialiased)
                    {
                        outputShaders.Add(SVGShader.GradientColorAlphaBlendedAntialiased);
                    } else {
                        outputShaders.Add(SVGShader.GradientColorAlphaBlended);
                    }
                }
            } else {
                if(useOpaqueShader)
                {
                    outputShaders.Add(SVGShader.SolidColorOpaque);
                }
                if(useTransparentShader)
                {
                    if(antialiased)
                    {
                        outputShaders.Add(SVGShader.SolidColorAlphaBlendedAntialiased);
                    } else {
                        outputShaders.Add(SVGShader.SolidColorAlphaBlended);
                    }
                }
            }
            
            for(int i = 0; i < totalLayers; i++)
            {
                int totalShapes = layers[i].shapes.Length;
                for(int j = 0; j < totalShapes; j++)
                {
                    vertexCount = layers[i].shapes[j].vertices.Length;
                    
                    if(layers[i].shapes[j].colors != null && layers[i].shapes[j].colors.Length == vertexCount)
                    {
                        Color32 finalColor = layers[i].shapes[j].fill.finalColor;
                        for(int k = 0; k < vertexCount; k++)
                        {
                            currentVertex = vertexStart + k;
                            vertices[currentVertex] = layers[i].shapes[j].vertices[k];
                            if (useOpaqueShader)
                            {
                                vertices[currentVertex].z = layers[i].shapes[j].depth * -SVGAssetImport.minDepthOffset;
                            } else
                            {
                                vertices[currentVertex].z = layers[i].shapes[j].depth;
                            }
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
                            if (useOpaqueShader)
                            {
                                vertices[currentVertex].z = layers[i].shapes[j].depth * -SVGAssetImport.minDepthOffset;
                            } else
                            {
                                vertices[currentVertex].z = layers[i].shapes[j].depth;
                            }
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
                            
                            if(layers[i].shapes[j].angles != null && layers[i].shapes[j].angles.Length == vertexCount)
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
                            if(layers[i].shapes[j].angles != null && layers[i].shapes[j].angles.Length == vertexCount)
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
                            if(layers[i].shapes[j].angles != null && layers[i].shapes[j].angles.Length == vertexCount)
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
            
            if(vertices != null)
            {
                if(vertices.Length > 65000)
                {
                    Debug.LogError("A mesh may not have more than 65000 vertices. Please try to reduce quality or split SVG file.");
                    return false;
                }
            } else {
                return false;
            }
            
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
        /*
        protected static void Create9Slice()
        {
            int meshCount = SVGGraphics.layers.Count;
            SVGBounds meshBounds = SVGBounds.InfiniteInverse;
            for (int i = 0; i < meshCount; i++)
            {
                if (SVGGraphics.layers [i].size.sqrMagnitude == 0f) continue;                
                meshBounds.Encapsulate(SVGGraphics.layers [i].center, SVGGraphics.layers [i].size);
            }

            // 9-slice
            if(SVGAssetImport.border.sqrMagnitude > 0f)
            {
                Vector2 min = meshBounds.min;
                Vector2 max = meshBounds.max;

                float bottom = Mathf.Lerp(min.y, max.y, 0.5f);

                for(int i = 0; i < meshCount; i++)
                {
                    if(SVGAssetImport.border.x > 0)
                        SVGMeshCutter.MeshSplit(SVGGraphics.layers [i], new Vector2(Mathf.Lerp(min.x, max.x, SVGAssetImport.border.x), 0f), Vector2.up); 
                    if(SVGAssetImport.border.y > 0)
                        SVGMeshCutter.MeshSplit(SVGGraphics.layers [i], new Vector2(0f, bottom), Vector2.right);                     
                    if(SVGAssetImport.border.z > 0)
                        SVGMeshCutter.MeshSplit(SVGGraphics.layers [i], new Vector2(Mathf.Lerp(min.x, max.x, 1f - SVGAssetImport.border.z), 0f), Vector2.up);                     
                    if(SVGAssetImport.border.w > 0)
                        SVGMeshCutter.MeshSplit(SVGGraphics.layers [i], new Vector2(0f, Mathf.Lerp(min.y, max.y, SVGAssetImport.border.w)), Vector2.right); 
                }
            }
        }
        */
    }
}
