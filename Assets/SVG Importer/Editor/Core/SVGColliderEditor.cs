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
    [CustomEditor(typeof(SVGCollider2D))]
    public class SVGColliderEditor : Editor
    {
        SerializedProperty quality;
        SerializedProperty offset;

        void OnEnable()
        {
            quality = serializedObject.FindProperty("_quality");
            offset = serializedObject.FindProperty("_offset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(quality, new GUIContent("Quality"));
            EditorGUILayout.PropertyField(offset, new GUIContent("Offset"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }       
    }
}