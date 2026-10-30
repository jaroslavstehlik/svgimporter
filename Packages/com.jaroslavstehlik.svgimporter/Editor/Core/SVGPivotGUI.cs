// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEditor;
using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// The pivot fields as Unity's sprite importer draws them: a Pivot popup of the nine positions
    /// and Custom, and for Custom a normalised position with (0, 0) at the bottom left.
    ///
    /// SVG Assets store the pivot with y pointing down, as in the SVG ((0, 0) at the top left), and
    /// whether it is custom; the fields convert, so stored values don't change.
    /// </summary>
    internal static class SVGPivotGUI
    {
        static readonly GUIContent s_PivotLabel = new GUIContent("Pivot", "Sprite pivot point in its local space. May be used for syncing animation frames of different sizes.");
        static readonly GUIContent s_CustomPivotLabel = new GUIContent("Custom Pivot", "Pivot position, from (0, 0) at the bottom left to (1, 1) at the top right.");

        // Unity's SpriteAlignment order and names: Center, Top Left, Top, Top Right, Left, Right,
        // Bottom Left, Bottom, Bottom Right, Custom.
        static readonly GUIContent[] s_Alignments = {
            new GUIContent("Center"), new GUIContent("Top Left"), new GUIContent("Top"), new GUIContent("Top Right"),
            new GUIContent("Left"), new GUIContent("Right"), new GUIContent("Bottom Left"), new GUIContent("Bottom"),
            new GUIContent("Bottom Right"), new GUIContent("Custom"),
        };
        const int k_Custom = 9;

        // The pivot of each alignment, y pointing down as stored.
        static readonly Vector2[] s_Pivots = {
            new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 1f), new Vector2(0.5f, 1f),
            new Vector2(1f, 1f),
        };

        public static void Draw(SerializedProperty customPivotPoint, SerializedProperty pivotPoint)
        {
            int alignment = Alignment(customPivotPoint.boolValue, pivotPoint.vector2Value);
            EditorGUI.showMixedValue = customPivotPoint.hasMultipleDifferentValues || pivotPoint.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            alignment = EditorGUILayout.Popup(s_PivotLabel, alignment, s_Alignments);
            if (EditorGUI.EndChangeCheck())
            {
                customPivotPoint.boolValue = alignment == k_Custom;
                if (alignment != k_Custom)
                    pivotPoint.vector2Value = s_Pivots[alignment];
            }
            EditorGUI.showMixedValue = false;

            if (alignment == k_Custom && !customPivotPoint.hasMultipleDifferentValues)
            {
                Vector2 stored = pivotPoint.vector2Value;
                EditorGUI.showMixedValue = pivotPoint.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                Vector2 shown = EditorGUILayout.Vector2Field(s_CustomPivotLabel, new Vector2(stored.x, 1f - stored.y));
                if (EditorGUI.EndChangeCheck())
                    pivotPoint.vector2Value = new Vector2(shown.x, 1f - shown.y);
                EditorGUI.showMixedValue = false;
            }
        }

        // Custom, or the preset the pivot is at.
        static int Alignment(bool custom, Vector2 pivot)
        {
            if (custom) return k_Custom;
            for (int i = 0; i < s_Pivots.Length; i++)
                if (Vector2.Distance(pivot, s_Pivots[i]) < 0.001f) return i;
            return k_Custom;
        }
    }
}
