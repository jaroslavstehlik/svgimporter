// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace SVGImporter {

    [CustomEditor(typeof(SVGImporterSettings))]
    public class SVGImporterSettingsEditor : Editor
    {
        SerializedProperty format;
        SerializedProperty useGradients;
        SerializedProperty antialiasing;
		SerializedProperty antialiasingWidth;
		SerializedProperty meshCompression;
        SerializedProperty scale;
        SerializedProperty vpm;
        SerializedProperty depthOffset;
        SerializedProperty compressDepth;
        SerializedProperty customPivotPoint;
        SerializedProperty pivotPoint;
        SerializedProperty generateCollider;
		SerializedProperty keepSVGFile;
		SerializedProperty useLayers;
        SerializedProperty ignoreSVGCanvas;
        SerializedProperty optimizeMesh;
        SerializedProperty generateNormals;
        SerializedProperty generateTangents;

        void OnEnable()
        {
			SVGImporterLaunchEditor.OpenSettingsWindow();
            format = serializedObject.FindProperty("defaultSVGFormat");
            useGradients = serializedObject.FindProperty("defaultUseGradients");
            antialiasing = serializedObject.FindProperty("defaultAntialiasing");
			antialiasingWidth = serializedObject.FindProperty("defaultAntialiasingWidth");
			meshCompression = serializedObject.FindProperty("defaultMeshCompression");
            scale = serializedObject.FindProperty("defaultScale");
            vpm = serializedObject.FindProperty("defaultVerticesPerMeter");
            depthOffset = serializedObject.FindProperty("defaultDepthOffset");
            compressDepth = serializedObject.FindProperty("defaultCompressDepth");
            customPivotPoint = serializedObject.FindProperty("defaultCustomPivotPoint");
            pivotPoint = serializedObject.FindProperty("defaultPivotPoint");
            generateCollider = serializedObject.FindProperty("defaultGenerateCollider");
			keepSVGFile = serializedObject.FindProperty("defaultKeepSVGFile");
			useLayers = serializedObject.FindProperty("defaultUseLayers");
            ignoreSVGCanvas = serializedObject.FindProperty("defaultIgnoreSVGCanvas");
            optimizeMesh = serializedObject.FindProperty("defaultOptimizeMesh");
            generateNormals = serializedObject.FindProperty("defaultGenerateNormals");
            generateTangents = serializedObject.FindProperty("defaultGenerateTangents");
        }

        public override void OnInspectorGUI()
        {
			serializedObject.Update();
			EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(format, SVGAssetEditor.FORMAT_LABEL);
            EditorGUILayout.PropertyField(useGradients, SVGAssetEditor.USE_GRADIENTS_LABEL);
            EditorGUILayout.PropertyField(antialiasing, SVGAssetEditor.ANTIALIASING_LABEL);
			EditorGUILayout.PropertyField(antialiasingWidth, SVGAssetEditor.ANTIALIASING_WIDTH_LABEL);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(meshCompression, SVGAssetEditor.MESH_COMPRESSION_LABEL);
            EditorGUILayout.PropertyField(optimizeMesh, SVGAssetEditor.OPTIMIZE_MESH_LABEL);
            EditorGUILayout.PropertyField(scale, SVGAssetEditor.SCALE_LABEL);
            EditorGUILayout.PropertyField(vpm, SVGAssetEditor.QUALITY_LABEL);
            
            if (format.enumValueIndex == (int)SVGAssetFormat.Opaque)
            {
                EditorGUILayout.PropertyField(depthOffset, SVGAssetEditor.DEPTH_OFFSET_LABEL);
                EditorGUILayout.PropertyField(compressDepth, SVGAssetEditor.COMPRESS_DEPTH_LABEL);           
            }

            EditorGUILayout.PropertyField(customPivotPoint, SVGAssetEditor.CUSTOM_PIVOT_LABEL);
            if (customPivotPoint.boolValue)
            { 
                EditorGUILayout.PropertyField(pivotPoint, SVGAssetEditor.PIVOT_LABEL);
            } else
            {
                Vector2 pivotPointVector = pivotPoint.vector2Value;
                int selectedIndex = Mathf.RoundToInt(pivotPointVector.x * 2 + Mathf.Clamp(pivotPointVector.y * 6, 0, 8));
                
                selectedIndex = EditorGUILayout.Popup("Pivot", selectedIndex, SVGAssetEditor.anchorPosition);
                
                int x = selectedIndex % 3;
                int y = Mathf.FloorToInt(selectedIndex / 3);
                
                pivotPointVector.x = x / 2f;
                pivotPointVector.y = y / 2f;
                
                pivotPoint.vector2Value = pivotPointVector;
            }
            EditorGUILayout.PropertyField(generateCollider, SVGAssetEditor.GENERATE_COLLIDER_LABEL);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Normals & Tangents", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(generateNormals, SVGAssetEditor.GENERATE_NORMALS_LABEL);
            if(!generateNormals.boolValue)  EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(generateTangents, SVGAssetEditor.GENERATE_TANGENTS_LABEL);
            if(!generateNormals.boolValue && generateTangents.boolValue)
            {
                generateTangents.boolValue = false;
            }
            if(!generateNormals.boolValue) EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("SVG Document", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(keepSVGFile, SVGAssetEditor.KEEP_SVG_FILE_LABEL);
			EditorGUILayout.PropertyField(useLayers, SVGAssetEditor.USE_LAYERS_LABEL);
            EditorGUILayout.PropertyField(ignoreSVGCanvas, SVGAssetEditor.IGNORE_SVG_CANVAS_LABEL);

            GUILayout.Space(10f);

			if(EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}

            EditorGUILayout.BeginHorizontal();
            if(SVGPostprocessor.active)
            {
                EditorGUILayout.LabelField("Asset Postprocessor: On");
                if(GUILayout.Button("Stop"))
                {
                    SVGPostprocessor.Stop();
                }
            } else {
                EditorGUILayout.LabelField("Asset Postprocessor: Off");
                if(GUILayout.Button("Start"))
                {
                    SVGPostprocessor.Start();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if(SVGImporterLaunchEditor.active)
            {
                EditorGUILayout.LabelField("Support Service: On");
                /*
                if(GUILayout.Button("Stop"))
                {
                    SVGImporterLaunchEditor.Stop();
                }
                */
            } else {
                EditorGUILayout.LabelField("Support Service: Off");
                if(GUILayout.Button("Start"))
                {
                    SVGImporterLaunchEditor.Stop();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

    }
}
