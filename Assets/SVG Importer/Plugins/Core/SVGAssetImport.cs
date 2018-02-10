// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

#define IGNORE_EXCEPTIONS

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{
    using Document;
    using Rendering;
    using Geometry;
    using Data;
    using Utils;

    public class SVGAssetImport
    {    
        public static SVGAtlasData atlasData;

        public static List<SVGError> errors;	
        protected static bool _importingSVG = false;
        public static bool importingSVG
        {
            get {
                return _importingSVG;
            }
        }

        public static SVGUseGradients useGradients;
        public static bool antialiasing;
        public static float vpm;
        public static Vector2 pivotPoint;
        public static bool ignoreSVGCanvas;
        public static float meshScale = 1f;        
        public static Vector4 border;
        public static bool sliceMesh = false;
        public static float minDepthOffset = 0.001f;
        public static SVGAssetFormat format = SVGAssetFormat.Opaque;
        public static bool compressDepth = true;

        private string _SVGFile;
        private Texture2D _texture = null;
        private SVGGraphics _graphics;
        private SVGDocument _svgDocument;
    
        public SVGAssetImport(string svgFile, float vertexPerMeter = 1000f)
        {
            vpm = vertexPerMeter;
            this._SVGFile = svgFile;
            _graphics = new SVGGraphics(vertexPerMeter, antialiasing);
        }

        private void CreateEmptySVGDocument()
        {
            _svgDocument = new SVGDocument(_SVGFile, this._graphics);
        }

#if UNITY_EDITOR
        public void StartProcess(SVGAsset asset)
        {
            if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if(errors == null)
            {
                errors = new List<SVGError>();
            } else {
                errors.Clear();
            }
            _importingSVG = true;

            System.Reflection.FieldInfo _editor_runtimeMaterials = typeof(SVGAsset).GetField("_runtimeMaterials", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _editor_runtimeMaterials.SetValue(asset, null);

            System.Reflection.FieldInfo _editor_runtimeMesh = typeof(SVGAsset).GetField("_runtimeMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _editor_runtimeMesh.SetValue(asset, null);

            UnityEditor.SerializedObject svgAsset = new UnityEditor.SerializedObject(asset);
            UnityEditor.SerializedProperty sharedMesh = svgAsset.FindProperty("_sharedMesh");            
            UnityEditor.SerializedProperty sharedShaders = svgAsset.FindProperty("_sharedShaders");

            Clear();
            SVGParser.Init();
            SVGGraphics.Init();
            atlasData = new SVGAtlasData();
            atlasData.Init(SVGAtlas.defaultAtlasTextureWidth * SVGAtlas.defaultAtlasTextureHeight);
            atlasData.AddGradient(SVGAtlasData.GetDefaultGradient());
            SVGElement _rootSVGElement = null;

#if IGNORE_EXCEPTIONS
            try {
#else
                Debug.LogWarning("Exceptions are turned on!");
#endif
                // Create new Asset
                CreateEmptySVGDocument();
                _rootSVGElement = this._svgDocument.rootElement;
#if IGNORE_EXCEPTIONS
            } catch (System.Exception exception) {
                _rootSVGElement = null;
                errors.Add(SVGError.Syntax);
                Debug.LogError("SVG Document Exception: "+exception.Message, asset);
            }
#endif

            if(_rootSVGElement == null)
            {
                Debug.LogError("SVG Document is corrupted! "+UnityEditor.AssetDatabase.GetAssetPath(asset), asset);
                _importingSVG = false;
                return;
            }

#if IGNORE_EXCEPTIONS
            try {
#endif
                _rootSVGElement.Render();

                Rect viewport = _rootSVGElement.paintable.viewport;
                viewport.x *= SVGAssetImport.meshScale;
                viewport.y *= SVGAssetImport.meshScale;
                viewport.size *= SVGAssetImport.meshScale;

                Vector2 offset;
                SVGGraphics.CorrectSVGLayers(SVGGraphics.layers, viewport, asset, out offset);

                // Handle gradients
                bool hasGradients = false;

                // Create actual Mesh
                Shader[] outputShaders;
                Mesh mesh = new Mesh();
                SVGMesh.CombineMeshes(SVGGraphics.layers.ToArray(), mesh, out outputShaders, useGradients, format, compressDepth, asset.antialiasing);
                if(mesh == null)
                    return;

                if(useGradients == SVGUseGradients.Always)
                {
                    if(outputShaders != null)
                    {
                        for(int i = 0; i < outputShaders.Length; i++)
                        {
                            if(outputShaders[i] == null) continue;
                            if(outputShaders[i].name == SVGShader.SolidColorOpaque.name)
                            {
                                outputShaders[i] = SVGShader.GradientColorOpaque;
                            } else if(outputShaders[i].name == SVGShader.SolidColorAlphaBlended.name)
                            {
                                outputShaders[i] = SVGShader.GradientColorAlphaBlended;
                            } else if(outputShaders[i].name == SVGShader.SolidColorAlphaBlendedAntialiased.name)
                            {
                                outputShaders[i] = SVGShader.GradientColorAlphaBlendedAntialiased;
                            }
                        }
                    }
                    hasGradients = true;
                } else {
                    if(outputShaders != null)
                    {
                        for(int i = 0; i < outputShaders.Length; i++)
                        {
                            if(outputShaders[i] == null) continue;
                            if(outputShaders[i].name == SVGShader.GradientColorOpaque.name ||
                               outputShaders[i].name == SVGShader.GradientColorAlphaBlended.name ||
                               outputShaders[i].name == SVGShader.GradientColorAlphaBlendedAntialiased.name)
                            {
                                hasGradients = true;
                                break;
                            }
                        }
                    }
                }

                if(!asset.useLayers)
                {
                    sharedMesh.objectReferenceValue = AddObjectToAsset<Mesh>(mesh, asset, HideFlags.HideInHierarchy);
                }

//                Material sharedMaterial;
                if(outputShaders != null && outputShaders.Length > 0)
                {
                    sharedShaders.arraySize = outputShaders.Length;
                    if(hasGradients)
                    {
                        for(int i = 0; i < outputShaders.Length; i++)
                        {
                            sharedShaders.GetArrayElementAtIndex(i).stringValue = outputShaders[i].name;                                                
                        }
                    } else {
                        for(int i = 0; i < outputShaders.Length; i++)
                        {
                            if(outputShaders[i].name == SVGShader.GradientColorAlphaBlended.name)
                            {
                                    outputShaders[i] = SVGShader.SolidColorAlphaBlended;
                            } else if(outputShaders[i].name == SVGShader.GradientColorOpaque.name)
                            {
                                outputShaders[i] = SVGShader.SolidColorOpaque;                                
                            }
                            sharedShaders.GetArrayElementAtIndex(i).stringValue = outputShaders[i].name;
                        }
                    }
                }

                // Serialize the Asset
                svgAsset.ApplyModifiedProperties();

                // Handle Canvas Rectangle
                System.Reflection.MethodInfo _editor_SetCanvasRectangle = typeof(SVGAsset).GetMethod("_editor_SetCanvasRectangle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _editor_SetCanvasRectangle.Invoke(asset, new object[]{new Rect(viewport.x, viewport.y, viewport.size.x, viewport.size.y)});

                if(asset.generateCollider)
                {
                    // Create polygon contour
                    if(SVGGraphics.paths != null && SVGGraphics.paths.Count > 0)
                    {
                        List<List<Vector2>> polygons = new List<List<Vector2>>();
                        for(int i = 0; i < SVGGraphics.paths.Count; i++)
                        {
                            Vector2[] points = SVGGraphics.paths[i].points;
                            for(int j = 0; j < points.Length; j++)
                            {
                                points[j].x = points[j].x * SVGAssetImport.meshScale - offset.x;
                                points[j].y = (points[j].y * SVGAssetImport.meshScale + offset.y) * -1f;
                            }

                            polygons.Add(new List<Vector2>(points));
                        }
                        
                        polygons = SVGGeom.MergePolygon(polygons);
                        
                        SVGPath[] paths = new SVGPath[polygons.Count];
                        for(int i = 0; i < polygons.Count; i++)
                        {
                            paths[i] = new SVGPath(polygons[i].ToArray());
                        }

                        System.Reflection.MethodInfo _editor_SetColliderShape = typeof(SVGAsset).GetMethod("_editor_SetColliderShape", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if(paths != null && paths.Length > 0)
                        {
                            _editor_SetColliderShape.Invoke(asset, new object[]{paths});
                        } else {
                            _editor_SetColliderShape.Invoke(asset, new object[]{null});
                        }
                    }
                } else {
                    System.Reflection.MethodInfo _editor_SetColliderShape = typeof(SVGAsset).GetMethod("_editor_SetColliderShape", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    _editor_SetColliderShape.Invoke(asset, new object[]{null});
                }

                System.Reflection.MethodInfo _editor_SetGradients = typeof(SVGAsset).GetMethod("_editor_SetGradients", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _editor_SetGradients.Invoke(asset, new object[]{null});
                if(hasGradients)
                {
                    if(atlasData.gradientCache != null && atlasData.gradientCache.Count > 0)
                    {
                        int gradientsCount = SVGAssetImport.atlasData.gradientCache.Count;
                        CCGradient[] gradients = new CCGradient[gradientsCount];
                        int i = 0;
                        foreach(KeyValuePair<string, CCGradient> entry in SVGAssetImport.atlasData.gradientCache)
                        {
                            gradients[i++] = entry.Value;
                        }
                        _editor_SetGradients.Invoke(asset, new object[]{gradients});
                    }
                }

                System.Reflection.MethodInfo _editor_SetLayers = typeof(SVGAsset).GetMethod("_editor_SetLayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _editor_SetLayers.Invoke(asset, new object[]{null});
                if(asset.useLayers)
                {
                    if(SVGGraphics.layers != null && SVGGraphics.layers.Count > 0)
                    {
                        _editor_SetLayers.Invoke(asset, new object[]{SVGGraphics.layers.ToArray()});
                    }
                }

#if IGNORE_EXCEPTIONS
            } catch(System.Exception exception) {
                Debug.LogWarning("Asset: "+UnityEditor.AssetDatabase.GetAssetPath(asset)+" Failed to import\n"+exception.Message, asset);
                errors.Add(SVGError.CorruptedFile);
            }
#endif
            if(_svgDocument != null)
            {
                _svgDocument.Clear();
                _svgDocument = null;
            }
            Clear();

            UnityEditor.EditorUtility.SetDirty(asset);
            _importingSVG = false;
        }
#endif

        public static void Clear()
        {
            if(atlasData != null)
            {
                atlasData.Clear();
                atlasData = null;
            }
            SVGParser.Clear();
            SVGGraphics.Clear();
        }

#if UNITY_EDITOR
        protected T AddObjectToAsset<T>(T obj, SVGAsset asset, HideFlags hideFlags) where T : UnityEngine.Object
        {
            if(obj == null)
                return null;

            obj.hideFlags = hideFlags;
            UnityEditor.AssetDatabase.AddObjectToAsset(obj, asset);
            return obj;
        }
#endif

        public void NewSVGFile(string svgFile)
        {
            this._SVGFile = svgFile;
        }

        public Texture2D GetTexture()
        {
            if (this._texture == null)
            {
                return new Texture2D(0, 0, TextureFormat.ARGB32, false);
            }
            return this._texture;
        }

        public Texture2D CloneTexture(Texture2D texture)
        {
            if(texture == null)
                return null;

            Texture2D output = new Texture2D(texture.width, texture.height, texture.format, false);
            output.name = texture.name;
            output.SetPixels32(texture.GetPixels32());
            output.wrapMode = TextureWrapMode.Clamp;
            output.anisoLevel = 0;
#if UNITY_EDITOR
            output.alphaIsTransparency = true;
#endif
            output.filterMode = FilterMode.Bilinear;
            output.Apply();
            return output;
        }  
    }
}
