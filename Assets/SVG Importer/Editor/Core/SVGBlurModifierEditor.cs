// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace SVGImporter
{
	[CustomEditor(typeof(SVGBlurModifier), true)]
	[CanEditMultipleObjects]
	public class SVGBlurModifierEditor : SVGModifierEditor {

		SerializedProperty camera;
		SerializedProperty radius;
		SerializedProperty motionBlur;
		SerializedProperty manualMotionBlur;
		SerializedProperty direction;
		SerializedProperty useCameraVelocity;

		public override void OnEnable()
		{
			base.OnEnable();
			camera = serializedObject.FindProperty("camera");
			radius = serializedObject.FindProperty("radius");
			motionBlur = serializedObject.FindProperty("motionBlur");
			manualMotionBlur = serializedObject.FindProperty("manualMotionBlur");
			direction = serializedObject.FindProperty("direction");
			useCameraVelocity = serializedObject.FindProperty("useCameraVelocity");
		}

		public override void OnInspectorGUI ()
		{
			ValidateAsset();

			bool validSVGAsset = true;
			for(int i = 0; i < targets.Length; i++)
			{
				SVGModifier modifier = targets[i] as SVGModifier;
				if(modifier == null) continue;
				if(modifier.svgRenderer == null) continue;
				if(modifier.svgRenderer.vectorGraphics == null) continue;
				if(!modifier.svgRenderer.vectorGraphics.antialiasing) 
				{
					validSVGAsset = false;
					break;
				}
			}
			
			if(!validSVGAsset)
			{
				EditorGUILayout.HelpBox("To use Blur Modifier please enable the antialiasing option on your SVG Asset", MessageType.Error);
			}

			serializedObject.Update();
			EditorGUI.BeginChangeCheck();
			ManualUpdateGUI();
			SelectionGUI();
			EditorGUILayout.PropertyField(camera);
			EditorGUILayout.PropertyField(radius);
			EditorGUILayout.PropertyField(motionBlur);
			if(!motionBlur.hasMultipleDifferentValues && motionBlur.boolValue)
			{
				EditorGUILayout.PropertyField(manualMotionBlur);
				EditorGUILayout.PropertyField(direction);
				EditorGUILayout.PropertyField(useCameraVelocity);
			}

			if(EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}
		}

	}
}