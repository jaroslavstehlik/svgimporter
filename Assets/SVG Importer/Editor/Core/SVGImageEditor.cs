// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

//#define DEBUG_MATERIALS

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.AnimatedValues;

using System.Linq;
using System.Reflection;

namespace SVGImporter
{
    [CustomEditor(typeof(SVGImage), true)]
    [CanEditMultipleObjects]
    public class SVGImageEditor : Editor
    {
        SVGImage image;

        protected SerializedProperty m_Type;
        protected SerializedProperty m_Color;
        protected SerializedProperty m_Material;
        protected SerializedProperty m_VectorGraphics;
        protected SerializedProperty m_PreserveAspect;
        protected SerializedProperty m_UsePivot;
        protected SerializedProperty m_RaycastTarget;

        protected GUIContent m_VectorContent;

        private GUIContent m_CorrectButtonContent;
        protected AnimBool m_ShowNativeSize;
        AnimBool m_ShowSlicedOrTiled;
        AnimBool m_ShowSliced;
        AnimBool m_ShowType;

        void OnEnable()
        {
            image = (SVGImage)target;

            m_Type = serializedObject.FindProperty("m_Type");
            m_Color = serializedObject.FindProperty("m_Color");
            m_Material = serializedObject.FindProperty("m_Material");
            m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
            
            m_ShowNativeSize = new AnimBool(false);
            m_ShowNativeSize.valueChanged.AddListener(Repaint);

            m_VectorGraphics = serializedObject.FindProperty("_vectorGraphics");
            m_PreserveAspect = serializedObject.FindProperty("m_PreserveAspect");
            m_UsePivot = serializedObject.FindProperty("m_UsePivot");

            m_VectorContent = new GUIContent("Vector Graphics");
            m_CorrectButtonContent = new GUIContent("Set Native Size", "Sets the size to match the content.");

            m_ShowType = new AnimBool(m_VectorGraphics.objectReferenceValue != null);
            m_ShowType.valueChanged.AddListener(Repaint);

            var typeEnum = (SVGImage.Type)m_Type.enumValueIndex;
            
            m_ShowSlicedOrTiled = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == SVGImage.Type.Sliced);
            m_ShowSliced = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == SVGImage.Type.Sliced);
            m_ShowSlicedOrTiled.valueChanged.AddListener(Repaint);
            m_ShowSliced.valueChanged.AddListener(Repaint);

            SetShowNativeSize(true);
        }       

        protected virtual void OnDisable()
        {
            Tools.hidden = false;
            m_ShowNativeSize.valueChanged.RemoveListener(Repaint);
            m_ShowType.valueChanged.RemoveListener(Repaint);
            m_ShowSlicedOrTiled.valueChanged.RemoveListener(Repaint);
            m_ShowSliced.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool changed = false;
            if(VectorGUI())
                changed = true;

            if(AppearanceControlsGUI())
                changed = true;

            m_ShowType.target = m_VectorGraphics.objectReferenceValue != null;
            if (EditorGUILayout.BeginFadeGroup(m_ShowType.faded))
            {
                if(TypeGUI())
                    changed = true;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUI.BeginChangeCheck();
            SetShowNativeSize(false);
            if (EditorGUILayout.BeginFadeGroup(m_ShowNativeSize.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PreserveAspect);
                EditorGUILayout.PropertyField(m_UsePivot);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
            NativeSizeButtonGUI();
            RaycastControlsGUI();

            if(EditorGUI.EndChangeCheck())
                changed = true;

            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
                image.SetAllDirty();
            }
        }
        
        protected bool VectorGUI()
        {
            bool changed = false;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_VectorGraphics, m_VectorContent);
            if(EditorGUI.EndChangeCheck())
            {
                MethodInfo Refresh = typeof(SVGImage).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance);
                Refresh.Invoke(target, null);
                changed = true;

                SVGImage.Type oldType = (SVGImage.Type)m_Type.enumValueIndex;
                SVGAsset vectorGraphics = m_VectorGraphics.objectReferenceValue as SVGAsset;
                if(vectorGraphics != null)
                {
                    if (vectorGraphics.border.SqrMagnitude() > 0)
                    {
                        m_Type.enumValueIndex = (int)SVGImage.Type.Sliced;
                    }
                    else if (oldType == SVGImage.Type.Sliced)
                    {
                        m_Type.enumValueIndex = (int)SVGImage.Type.Simple;
                    }
                }
            }

            #if DEBUG_MATERIALS
            EditorGUILayout.ObjectField("Default Material", image.defaultMaterialTest, typeof(Material), true);
            EditorGUILayout.ObjectField("Mask Material", image.maskMaterialTest, typeof(Material), true);
            EditorGUILayout.ObjectField("Current Material", image.currenttMaterialTest, typeof(Material), true);
            EditorGUILayout.ObjectField("Render Material", image.renderMaterialTest, typeof(Material), true);
            EditorGUILayout.ObjectField("Canvas Render Material", image.canvasRenderer.GetMaterial(), typeof(Material), true);
            #endif

            SVGAsset newVectorGraphics = m_VectorGraphics.objectReferenceValue as SVGAsset;
            if (newVectorGraphics)
            {
                if(newVectorGraphics.format != SVGAssetFormat.uGUI)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("Vector Graphics Asset must be set to uGUI format", MessageType.Error);
                    if(GUILayout.Button("Apply"))
                    {
                        FieldInfo _editor_format = typeof(SVGAsset).GetField("_format",  BindingFlags.NonPublic | BindingFlags.Instance);
                        _editor_format.SetValue(newVectorGraphics, (int)SVGAssetFormat.uGUI);

                        MethodInfo _editor_ApplyChanges = typeof(SVGAsset).GetMethod("_editor_ApplyChanges", BindingFlags.NonPublic | BindingFlags.Instance);
                        _editor_ApplyChanges.Invoke(newVectorGraphics, new object[]{false});

                        SVGAssetEditor.UpdateInstances(new SVGAsset[]{newVectorGraphics});
                        changed = true;
                    }
                    EditorGUILayout.EndHorizontal();
                } else {
                    #if UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9 || UNITY_5_0 || UNITY_5_1
                    int vertexCount = newVectorGraphics.uiVertexCount;
                    int percent = Mathf.RoundToInt(((float)vertexCount / 65534f) * 100f);
                    if(vertexCount > 128)
                    {
                        GUIContent infoContent = new GUIContent("Asset takes "+percent+"% of this Canvas, "+vertexCount+" vertices"+
                            "\nUI Canvas has limit of 65,534 vertices in total.");

						EditorStyles s_Current = (EditorStyles)typeof(EditorStyles).GetField("s_Current", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
						GUIStyle helpBoxStyle = (GUIStyle)typeof(EditorStyles).GetField("m_HelpBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(s_Current);
						Rect helpRect = GUILayoutUtility.GetRect(infoContent, helpBoxStyle);
                        EditorGUI.ProgressBar(helpRect, percent * 0.01f, "");
                        EditorGUI.HelpBox(helpRect, infoContent.text, MessageType.Warning);
                    }
					#endif
                }
            }

            return changed;
        }

        protected bool TypeGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Type, new GUIContent("Image Type"));

            ++EditorGUI.indentLevel;
            {
                SVGImage.Type typeEnum = (SVGImage.Type)m_Type.enumValueIndex;
                
                bool showSlicedOrTiled = (!m_Type.hasMultipleDifferentValues && (typeEnum == SVGImage.Type.Sliced));
                if (showSlicedOrTiled && targets.Length > 1)
                    showSlicedOrTiled = targets.Select(obj => obj as SVGImage).All(img => img.hasBorder);

                SVGImage image = target as SVGImage;
                if (EditorGUILayout.BeginFadeGroup(m_ShowSliced.faded))
                {
                    if (image.vectorGraphics != null && !image.hasBorder)
                        EditorGUILayout.HelpBox("This Image doesn't have a border.", MessageType.Warning);
                }
                EditorGUILayout.EndFadeGroup();
            }
            --EditorGUI.indentLevel;

            return EditorGUI.EndChangeCheck();
        }

        void SetShowNativeSize(bool instant)
        {
            SVGImage.Type type = (SVGImage.Type)m_Type.enumValueIndex;
            bool showNativeSize = (type == SVGImage.Type.Simple);
            SetShowNativeSize(showNativeSize, instant);
        }

        protected void SetShowNativeSize(bool show, bool instant)
        {
            if (instant)
                m_ShowNativeSize.value = show;
            else
                m_ShowNativeSize.target = show;
        }
        
        protected void NativeSizeButtonGUI()
        {
            if (EditorGUILayout.BeginFadeGroup(m_ShowNativeSize.faded))
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth);
                    if (GUILayout.Button(m_CorrectButtonContent, EditorStyles.miniButton))
                    {
                        foreach (Graphic graphic in targets.Select(obj => obj as Graphic))
                        {
                            Undo.RecordObject(graphic.rectTransform, "Set Native Size");
                            graphic.SetNativeSize();
                            EditorUtility.SetDirty(graphic);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFadeGroup();
        }
        
        protected bool AppearanceControlsGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Color);
            EditorGUILayout.PropertyField(m_Material);
            return EditorGUI.EndChangeCheck();
        }

        protected string GetEditorInfo(SVGAsset asset)
        {
            PropertyInfo _editor_Info = typeof(SVGAsset).GetProperty("_editor_Info", BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)_editor_Info.GetValue(asset, new object[0]);
        }

        public override string GetInfoString()
        {
            if(image.vectorGraphics != null)
                return GetEditorInfo(image.vectorGraphics);
            return "";
        }

        protected void RaycastControlsGUI()
        {
            EditorGUILayout.PropertyField(m_RaycastTarget);
        }
    }
}
