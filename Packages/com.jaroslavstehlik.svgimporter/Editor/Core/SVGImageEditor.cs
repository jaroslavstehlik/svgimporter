// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;

namespace SVGImporter
{
    /// <summary>The SVG Image Inspector, laid out as uGUI's Image Inspector.</summary>
    [CustomEditor(typeof(SVGImage), true)]
    [CanEditMultipleObjects]
    public class SVGImageEditor : GraphicEditor
    {
        static class Styles
        {
            public static readonly GUIContent vectorGraphics = EditorGUIUtility.TrTextContent("Vector Graphics", "The SVG Asset to draw.");
            public static readonly GUIContent imageType = EditorGUIUtility.TrTextContent("Image Type");
            public static readonly string noBorder = L10n.Tr("This SVG Asset doesn't have a border.");
            public static readonly string notForCanvas = L10n.Tr("The SVG Asset's Rendering must be UI (Canvas).");
            public static readonly GUIContent useCanvasFormat = EditorGUIUtility.TrTextContent("Fix", "Sets the SVG file's Rendering to UI (Canvas) and reimports it.");
        }

        SerializedProperty m_VectorGraphics;
        SerializedProperty m_Type;
        SerializedProperty m_PreserveAspect;
        SerializedProperty m_UsePivot;
        AnimBool m_ShowType;
        AnimBool m_ShowSliced;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_VectorGraphics = serializedObject.FindProperty("_vectorGraphics");
            m_Type = serializedObject.FindProperty("m_Type");
            m_PreserveAspect = serializedObject.FindProperty("m_PreserveAspect");
            m_UsePivot = serializedObject.FindProperty("m_UsePivot");

            m_ShowType = new AnimBool(m_VectorGraphics.objectReferenceValue != null);
            m_ShowType.valueChanged.AddListener(Repaint);
            m_ShowSliced = new AnimBool(IsSliced());
            m_ShowSliced.valueChanged.AddListener(Repaint);
            SetShowNativeSize(true);
        }

        protected override void OnDisable()
        {
            m_ShowType.valueChanged.RemoveListener(Repaint);
            m_ShowSliced.valueChanged.RemoveListener(Repaint);
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            VectorGraphicsGUI();
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();

            m_ShowType.target = m_VectorGraphics.objectReferenceValue != null;
            if (EditorGUILayout.BeginFadeGroup(m_ShowType.faded))
                TypeGUI();
            EditorGUILayout.EndFadeGroup();

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

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (SVGImage image in targets.Cast<SVGImage>())
                    image.SetAllDirty();
            }
        }

        void VectorGraphicsGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_VectorGraphics, Styles.vectorGraphics);
            var asset = m_VectorGraphics.objectReferenceValue as SVGAsset;
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                foreach (SVGImage image in targets.Cast<SVGImage>())
                    image.Refresh();
                // Assets with a border are drawn sliced, as sprites are.
                if (asset != null)
                {
                    if (asset.border.sqrMagnitude > 0f)
                        m_Type.enumValueIndex = (int)SVGImage.Type.Sliced;
                    else if (m_Type.enumValueIndex == (int)SVGImage.Type.Sliced)
                        m_Type.enumValueIndex = (int)SVGImage.Type.Simple;
                }
            }

            if (asset != null && !m_VectorGraphics.hasMultipleDifferentValues && asset.format != SVGAssetFormat.uGUI)
            {
                // 1.x SVG Assets (.asset files) are read-only until converted to .svg files.
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(asset)) as SVGFileImporter;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(Styles.notForCanvas, MessageType.Error);
                    using (new EditorGUI.DisabledScope(importer == null))
                    {
                        if (GUILayout.Button(Styles.useCanvasFormat, GUILayout.ExpandHeight(true)))
                        {
                            var settings = new SerializedObject(importer);
                            settings.FindProperty("_format").intValue = (int)SVGAssetFormat.uGUI;
                            settings.ApplyModifiedPropertiesWithoutUndo();
                            importer.SaveAndReimport();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }

        void TypeGUI()
        {
            EditorGUILayout.PropertyField(m_Type, Styles.imageType);
            m_ShowSliced.target = IsSliced();
            EditorGUI.indentLevel++;
            if (EditorGUILayout.BeginFadeGroup(m_ShowSliced.faded))
            {
                if (targets.Cast<SVGImage>().Any(image => image.vectorGraphics != null && !image.hasBorder))
                    EditorGUILayout.HelpBox(Styles.noBorder, MessageType.Warning);
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
        }

        bool IsSliced()
        {
            return !m_Type.hasMultipleDifferentValues && m_Type.enumValueIndex == (int)SVGImage.Type.Sliced;
        }

        void SetShowNativeSize(bool instant)
        {
            SetShowNativeSize(!m_Type.hasMultipleDifferentValues && m_Type.enumValueIndex == (int)SVGImage.Type.Simple, instant);
        }

        public override string GetInfoString()
        {
            var image = (SVGImage)target;
            return image.vectorGraphics != null ? image.vectorGraphics._editor_Info : "";
        }
    }
}
