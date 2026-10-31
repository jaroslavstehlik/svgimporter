// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections;

namespace SVGImporter
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SVGRenderer))]
    internal class SVGRendererEditor : Editor
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

        static class Styles
        {
            public static readonly GUIContent sortingLayer = EditorGUIUtility.TrTextContent("Sorting Layer", "The renderer's sorting layer.");
            public static readonly GUIContent orderInLayer = EditorGUIUtility.TrTextContent("Order in Layer", "The renderer's order within its sorting layer.");
            public static readonly string opaqueSorting = L10n.Tr("Opaque + Transparent assets draw with the depth test, so their position along z orders them, not Order in Layer. An SVG Opaque Sorting Group on a parent orders them by hierarchy.");
            public static readonly GUIContent additionalSettings = EditorGUIUtility.TrTextContent("Additional Settings");
            public static readonly GUIContent castShadows = EditorGUIUtility.TrTextContent("Cast Shadows");
            public static readonly GUIContent receiveShadows = EditorGUIUtility.TrTextContent("Receive Shadows");
            public static readonly GUIContent lightProbes = EditorGUIUtility.TrTextContent("Light Probes");
            public static readonly GUIContent renderingLayerMask = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "The rendering layers the renderer is on, for lights and effects that filter by them.");
            public static readonly GUIContent maskInteraction = EditorGUIUtility.TrTextContent("Mask Interaction", "How the renderer interacts with Sprite Masks, as a sprite's does.");
            public static readonly GUIContent receive2DLights = EditorGUIUtility.TrTextContent("Receive 2D Lights", "Whether the URP 2D Renderer's 2D lights light the renderer, as they light sprites. Off draws it unlit. Other renderers always draw SVGs unlit.");
        }

        static bool s_AdditionalSettings;

        SerializedProperty vectorGraphics;
        SerializedProperty color;
        SerializedProperty materials;
        SerializedProperty transparentMaterial;
        SerializedProperty opaqueMaterial;
        SerializedProperty type;
        SerializedProperty sortingLayerID;
        SerializedProperty sortingOrder;
        SerializedProperty shadowCastingMode;
        SerializedProperty receiveShadows;
        SerializedProperty lightProbeUsage;
        SerializedProperty renderingLayerMask;
        SerializedProperty receive2DLights;
        SerializedProperty maskInteraction;

        void OnEnable()
        {
            vectorGraphics = serializedObject.FindProperty("_vectorGraphics");
            color = serializedObject.FindProperty("_color");
            transparentMaterial = serializedObject.FindProperty("_transparentMaterial");
            opaqueMaterial = serializedObject.FindProperty("_opaqueMaterial");
            type = serializedObject.FindProperty("_type");
            sortingLayerID = serializedObject.FindProperty("_sortingLayerID");
            sortingOrder = serializedObject.FindProperty("_sortingOrder");
            shadowCastingMode = serializedObject.FindProperty("_shadowCastingMode");
            receiveShadows = serializedObject.FindProperty("_receiveShadows");
            lightProbeUsage = serializedObject.FindProperty("_lightProbeUsage");
            renderingLayerMask = serializedObject.FindProperty("_renderingLayerMask");
            receive2DLights = serializedObject.FindProperty("_receive2DLights");
            maskInteraction = serializedObject.FindProperty("_maskInteraction");

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
                    if(renderer.vectorGraphics != null && renderer.vectorGraphics.format != SVGAssetFormat.OpaqueAndTransparent)
                    {
                        return true;
                    }
                }
            } else {
                SVGRenderer renderer = target as SVGRenderer;
                if(renderer.vectorGraphics != null && renderer.vectorGraphics.format != SVGAssetFormat.OpaqueAndTransparent)
                {
                    return true;
                }
            }

            return false;
        }

        void OpaqueMaterialHelpBox()
        {
            EditorGUILayout.HelpBox("The Opaque material is used only when the SVG Asset's Rendering is Opaque + Transparent.", MessageType.Warning);
        }

        bool HasSelectedOpaqueAsset()
        {
            foreach (UnityEngine.Object t in targets)
            {
                var r = t as SVGRenderer;
                if (r != null && r.vectorGraphics != null && r.vectorGraphics.format == SVGAssetFormat.OpaqueAndTransparent)
                    return true;
            }
            return false;
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
            if(type.hasMultipleDifferentValues || (SVGDrawMode)type.enumValueIndex == SVGDrawMode.Sliced)
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

            EditorGUILayout.Space();
            SortingLayerField(sortingLayerID);
            EditorGUILayout.PropertyField(sortingOrder, Styles.orderInLayer);
            if(HasSelectedOpaqueAsset())
                EditorGUILayout.HelpBox(Styles.opaqueSorting, MessageType.Info);
            EditorGUILayout.PropertyField(maskInteraction, Styles.maskInteraction);
            EditorGUILayout.PropertyField(receive2DLights, Styles.receive2DLights);

            s_AdditionalSettings = EditorGUILayout.Foldout(s_AdditionalSettings, Styles.additionalSettings, true);
            if (s_AdditionalSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(shadowCastingMode, Styles.castShadows);
                EditorGUILayout.PropertyField(receiveShadows, Styles.receiveShadows);
                EditorGUILayout.PropertyField(lightProbeUsage, Styles.lightProbes);
                EditorGUILayout.PropertyField(renderingLayerMask, Styles.renderingLayerMask);
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
            return asset._editor_Info;
        }

        
        // A popup of the project's sorting layers, as renderers show it.
        static void SortingLayerField(SerializedProperty layerID)
        {
            SortingLayer[] layers = SortingLayer.layers;
            var names = new GUIContent[layers.Length];
            int selected = -1;
            for(int i = 0; i < layers.Length; i++)
            {
                names[i] = new GUIContent(layers[i].name);
                if(layers[i].id == layerID.intValue) selected = i;
            }
            EditorGUI.showMixedValue = layerID.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            selected = EditorGUILayout.Popup(Styles.sortingLayer, selected, names);
            if(EditorGUI.EndChangeCheck() && selected >= 0)
                layerID.intValue = layers[selected].id;
            EditorGUI.showMixedValue = false;
        }
    }
}
