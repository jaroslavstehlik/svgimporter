// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Reflection;

namespace SVGImporter
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SVGRenderer))]
    public class SVGRendererEditor : Editor
    {
        SVGRenderer renderer;
        const string SVG_IMPORTER_SVGRENDERER_MATERIALSFOLDOUTKEY = "SVG_IMPORTER_SVGRENDERER_MATERIALSFOLDOUTKEY";
        static bool materialsFoldout
        {
            get {
                if(EditorPrefs.HasKey(SVG_IMPORTER_SVGRENDERER_MATERIALSFOLDOUTKEY)) 
                    return EditorPrefs.GetBool(SVG_IMPORTER_SVGRENDERER_MATERIALSFOLDOUTKEY);
                return false;
            }
            set {
                EditorPrefs.SetBool(SVG_IMPORTER_SVGRENDERER_MATERIALSFOLDOUTKEY, value);
            }
        }

        const string SVG_IMPORTER_SVGRENDERER_SORTINGLAYERFOLDOUTKEY = "SVG_IMPORTER_SVGRENDERER_SORTINGLAYERFOLDOUTKEY"; 
        static bool sortingLayerFoldout
        {
            get {
                if(EditorPrefs.HasKey(SVG_IMPORTER_SVGRENDERER_SORTINGLAYERFOLDOUTKEY)) 
                    return EditorPrefs.GetBool(SVG_IMPORTER_SVGRENDERER_SORTINGLAYERFOLDOUTKEY);
                return false;
            }
            set {
                EditorPrefs.SetBool(SVG_IMPORTER_SVGRENDERER_SORTINGLAYERFOLDOUTKEY, value);
            }
        }

        SerializedProperty vectorGraphics;
        SerializedProperty color;
        SerializedProperty materials;
        SerializedProperty transparentMaterial;
        SerializedProperty opaqueMaterial;
        SerializedProperty type;
        SerializedProperty sortingLayerID;
//        SerializedProperty sortingLayerName;
        SerializedProperty sortingOrder;
        SerializedProperty overrideSorter;
        SerializedProperty overrideSorterChildren;

        void OnEnable()
        {
            vectorGraphics = serializedObject.FindProperty("_vectorGraphics");
            color = serializedObject.FindProperty("_color");
            transparentMaterial = serializedObject.FindProperty("_transparentMaterial");
            opaqueMaterial = serializedObject.FindProperty("_opaqueMaterial");
            type = serializedObject.FindProperty("_type");
            sortingLayerID = serializedObject.FindProperty("_sortingLayerID");
//            sortingLayerName = serializedObject.FindProperty("_sortingLayerName");
            sortingOrder = serializedObject.FindProperty("_sortingOrder");
            overrideSorter = serializedObject.FindProperty("_overrideSorter");
            overrideSorterChildren = serializedObject.FindProperty("_overrideSorterChildren");

            if (serializedObject.isEditingMultipleObjects)
            {
                SVGRenderer renderer = (SVGRenderer)target;
                Renderer meshRenderer = renderer.GetComponent<Renderer>();
                if (meshRenderer != null)
                    EditorUtility.SetSelectedRenderState(meshRenderer, EditorSelectedRenderState.Hidden);
            } else
            {
                UnityEngine.Object[] renderers = (UnityEngine.Object[])targets;
                SVGRenderer renderer;
                if (renderers != null && renderers.Length > 0)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        renderer = renderers [i] as SVGRenderer;
                        if (renderer == null)
                            continue;
                        MeshRenderer meshRenderer = renderer.GetComponent<MeshRenderer>();
                        if (meshRenderer != null)
                            EditorUtility.SetSelectedRenderState(meshRenderer, EditorSelectedRenderState.Hidden);
                    }
                }
            }
        }

        bool HasSelectedTransparentAsset()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                UnityEngine.Object[] renderers = (UnityEngine.Object[])targets;
                for(int i = 0; i < renderers.Length; i++)
                {
                    renderer = renderers [i] as SVGRenderer;
                    if(renderer.vectorGraphics != null && renderer.vectorGraphics.format != SVGAssetFormat.Opaque)
                    {
                        return true;
                    }
                }
            } else {
                SVGRenderer renderer = target as SVGRenderer;
                if(renderer.vectorGraphics != null && renderer.vectorGraphics.format != SVGAssetFormat.Opaque)
                {
                    return true;
                }
            }

            return false;
        }

        void OpaqueMaterialHelpBox()
        {
            EditorGUILayout.HelpBox("Opaque Material is not used when vector graphics format is set to transparent or UGUI.", MessageType.Warning);
        }

        void SortingMaterialHelpBox()
        {
            EditorGUILayout.HelpBox("Unity sorting does not work on Opaque objects, change Format to transparent.", MessageType.Warning);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(vectorGraphics, new GUIContent("Vector Graphics"));
            EditorGUILayout.PropertyField(color, new GUIContent("Color"));

            if(!transparentMaterial.hasMultipleDifferentValues && !opaqueMaterial.hasMultipleDifferentValues)
            {
                if(transparentMaterial.objectReferenceValue != opaqueMaterial.objectReferenceValue)
                {
                    EditorGUI.showMixedValue = true;

                    Material material = EditorGUILayout.ObjectField("material", null, typeof(Material), false) as Material;
                    if(material != null)
                    {
                        transparentMaterial.objectReferenceValue = material;
                        opaqueMaterial.objectReferenceValue = material;
                    }

                    EditorGUI.showMixedValue = false;
                } else {
                    Material oldMaterial = transparentMaterial.objectReferenceValue as Material;
                    Material material = EditorGUILayout.ObjectField("material", oldMaterial, typeof(Material), false) as Material;
                    if(oldMaterial != material)
                    {
                        transparentMaterial.objectReferenceValue = material;
                        opaqueMaterial.objectReferenceValue = material;
                    }
                }
            }
            
            EditorGUILayout.PropertyField(type, new GUIContent("Image Type"));
            if(type.hasMultipleDifferentValues || (SVGRenderer.Type)type.enumValueIndex == SVGRenderer.Type.Sliced)
            {
                RectTransform rectTransform = ((SVGRenderer)target).GetComponent<RectTransform>();
                if(rectTransform == null)
                {
                    EditorGUILayout.HelpBox("To use the sliced image type, you need to add RectTransform component first.", MessageType.Warning);
                }
            }

            materialsFoldout = EditorGUILayout.Foldout(materialsFoldout, "Advanced Materials");
            if(materialsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(transparentMaterial, new GUIContent("Transparent"));
                EditorGUILayout.PropertyField(opaqueMaterial, new GUIContent("Opaque"));
                if(HasSelectedTransparentAsset())
                {
                    OpaqueMaterialHelpBox();
                }
                EditorGUI.indentLevel--;
            }

            sortingLayerFoldout = EditorGUILayout.Foldout(sortingLayerFoldout, "Sorting");
            if (sortingLayerFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Unity Sorter", EditorStyles.boldLabel);
                MethodInfo sortingLayerField = typeof(EditorGUILayout).GetMethod("SortingLayerField", 
                                                                                 BindingFlags.Static | BindingFlags.NonPublic,
                                                                                 System.Type.DefaultBinder,
                                                                                 new System.Type[] {typeof(GUIContent), typeof(SerializedProperty), typeof(GUIStyle), typeof(GUIStyle) },
                                                                                 null
                                                                                );
                if(sortingLayerField != null)
                {
                    sortingLayerField.Invoke(null, new System.Object[]{ new GUIContent("Sorting Layer"), sortingLayerID, EditorStyles.popup, EditorStyles.label });
                }
                EditorGUILayout.PropertyField(sortingOrder);
                if(!HasSelectedTransparentAsset())
                {
                    SortingMaterialHelpBox();
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("SVG Sorter", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(overrideSorter);
                if(overrideSorter.hasMultipleDifferentValues || overrideSorter.boolValue)
                {
                    EditorGUILayout.PropertyField(overrideSorterChildren, new GUIContent("Override Children", "Override sorting order in all children."));
                }
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {                
                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < serializedObject.targetObjects.Length; i++)
                {
                    SVGRenderer svgRenderer = serializedObject.targetObjects[i] as SVGRenderer;
                    if (svgRenderer != null) svgRenderer.UpdateRenderer();
                }
            }
        }
       
        public override string GetInfoString()
        {
            SVGAsset svgAsset = vectorGraphics.objectReferenceValue as SVGAsset;
            if (svgAsset)
                return GetEditorInfo(svgAsset);
            return "";
        }
        
        protected string GetEditorInfo(SVGAsset asset)
        {
            PropertyInfo _editor_Info = typeof(SVGAsset).GetProperty("_editor_Info", BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)_editor_Info.GetValue(asset, new object[0]);
        }

        // Get the sorting layer names
        public string[] GetSortingLayerNames()
        {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[])sortingLayersProperty.GetValue(null, new object[0]);
        }

        // Get the unique sorting layer IDs -- tossed this in for good measure
        public int[] GetSortingLayerUniqueIDs()
        {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
            return (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
        }
    }
}
