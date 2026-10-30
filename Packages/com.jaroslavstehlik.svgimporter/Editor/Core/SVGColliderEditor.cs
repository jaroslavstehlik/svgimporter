// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

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
    internal class SVGColliderEditor : Editor
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