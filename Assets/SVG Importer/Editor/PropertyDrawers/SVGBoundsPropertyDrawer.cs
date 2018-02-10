// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace SVGImporter 
{
    using Utils;

    [CustomPropertyDrawer(typeof(SVGBounds))]
    public class SVGBoundsPropertyDrawer : PropertyDrawer {

    	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            SerializedProperty center = property.FindPropertyRelative("_center");
            SerializedProperty size = property.FindPropertyRelative("_size");

            float height = 18f;
            float offset = 10f;
            Rect labelRect = new Rect(position.x, position.y, position.width, height);
            Rect centerRect = new Rect(labelRect.x + offset, labelRect.y + labelRect.height, position.width - offset, height);
            Rect sizeRect = new Rect(centerRect.x , centerRect.y + centerRect.height, centerRect.width, height);

            EditorGUI.LabelField(labelRect, "Bounds");
            EditorGUI.BeginChangeCheck();
            center.vector2Value = EditorGUI.Vector2Field(centerRect, "center", center.vector2Value);
            size.vector2Value = EditorGUI.Vector2Field(sizeRect, "size", size.vector2Value);

            Vector2 centerValue = center.vector2Value;
            Vector2 sizeValue = size.vector2Value;
            Vector2 extentsValue = sizeValue * 0.5f;
        
            if (EditorGUI.EndChangeCheck())
            {
                property.FindPropertyRelative("_extents").vector2Value = extentsValue;
                property.FindPropertyRelative("_minX").floatValue = centerValue.x - extentsValue.x;
                property.FindPropertyRelative("_minY").floatValue = centerValue.y - extentsValue.y;
                property.FindPropertyRelative("_maxX").floatValue = centerValue.x + extentsValue.x;
                property.FindPropertyRelative("_maxY").floatValue = centerValue.y + extentsValue.y;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 53f;
        }
    }
}
