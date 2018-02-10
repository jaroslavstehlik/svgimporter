// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SVGImporter
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SVGFrameAnimator))]
	public class SVGFrameAnimatorEditor : Editor
    {
		SerializedProperty frames;
		SerializedProperty frameIndex;

		private ReorderableList framesList;
		private float thumbnailSize = 30f;

        protected GUIStyle _bgStyle;
        public GUIStyle bgStyle
        {
            get {

                if( _bgStyle == null )
                {
                    _bgStyle = new GUIStyle( GUI.skin.box );
                    _bgStyle.normal.background = MakeTex( 2, 2, new Color( 1f, 0f, 0f, 0.25f ) );
                }

                return _bgStyle;
            }
        }

        private Texture2D MakeTex( int width, int height, Color col )
        {
            Color[] pix = new Color[width * height];
            for( int i = 0; i < pix.Length; ++i )
            {
                pix[ i ] = col;
            }
            Texture2D result = new Texture2D( width, height );
            result.SetPixels( pix );
            result.Apply();
            return result;
        }

        void OnEnable()
        {
			frames = serializedObject.FindProperty("frames");
			frameIndex = serializedObject.FindProperty("frameIndex");

			framesList = new ReorderableList(serializedObject, frames, true, true, true, true);
			framesList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Animation Frames");
			};
			framesList.elementHeight = thumbnailSize + 4;

			framesList.drawElementCallback =  
			(Rect rect, int index, bool isActive, bool isFocused) => {
				var element = framesList.serializedProperty.GetArrayElementAtIndex(index);
                
                if(frameIndex.floatValue == index)
                {
                    GUI.Box(rect, "", bgStyle);
                }

                rect.x += 2;
				rect.y += 2;

				Texture2D thumbnail = null;
				if(element.objectReferenceValue != null)
				{
					thumbnail = AssetPreview.GetAssetPreview(element.objectReferenceValue);
				}

				if(thumbnail != null)
				{
					EditorGUI.DrawPreviewTexture(new Rect(rect.x, rect.y, thumbnailSize, thumbnailSize), thumbnail);
				}
				EditorGUI.PropertyField(new Rect(rect.x + thumbnailSize + 4, rect.y, rect.width - thumbnailSize - 8, EditorGUIUtility.singleLineHeight), element, new GUIContent());
			};
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

			if(frames.arraySize > 1)
			{
				frameIndex.floatValue = EditorGUILayout.IntSlider("Current Frame", (int)frameIndex.floatValue, 0, frames.arraySize - 1);
				GUILayout.BeginHorizontal();
				GUI.enabled = !(frameIndex.floatValue <= 0f);
				if(GUILayout.Button("<"))
				{
					frameIndex.floatValue = Mathf.Clamp(frameIndex.floatValue - 1, 0, frames.arraySize - 1);
				}
				GUI.enabled = !(frameIndex.floatValue >= frames.arraySize - 1);
				if(GUILayout.Button(">"))
				{
					frameIndex.floatValue = Mathf.Clamp(frameIndex.floatValue + 1, 0, frames.arraySize - 1);
				}
				GUI.enabled = true;
				GUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();
            Rect beforeList = GUILayoutUtility.GetLastRect();
			framesList.DoLayoutList();
            Rect afterList = GUILayoutUtility.GetLastRect();
            OnDragGUI(new Rect(beforeList.min.x, beforeList.min.y, beforeList.max.x, afterList.min.y - beforeList.min.y));
			EditorGUILayout.Space();

			if(GUILayout.Button("Sort by name"))
			{
				List<SVGAsset> frameList = new List<SVGAsset>();
				for(int i = 0; i < frames.arraySize; i++)
				{
					frameList.Add(frames.GetArrayElementAtIndex(i).objectReferenceValue as SVGAsset);
				}

				frameList.Sort(delegate(SVGAsset x, SVGAsset y) {
					if(x == null && y == null) return 0;
					else if(x == null) return -1;
					else if(y == null) return 1;
					else return x.name.CompareTo(y.name);
				});

				for(int i = 0; i < frames.arraySize; i++)
				{
					frames.GetArrayElementAtIndex(i).objectReferenceValue = frameList[i];
				}
			}

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }  

        void OnDragGUI(Rect hitRect)
        {
            Event current = Event.current;
            if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform && current.type != EventType.DragExited)
            {
                return;
            }

            if(!hitRect.Contains(current.mousePosition))
                return;

            SVGAsset[] svgAssetsFromDraggedPathsOrObjects = SVGImporterLaunchEditor.GetSVGAssetsFromDraggedObjects();
            if (svgAssetsFromDraggedPathsOrObjects.Length == 0)
            {
                return;
            }
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            EventType type = current.type;
            if (type == EventType.DragPerform)
            {
                AddFrames(svgAssetsFromDraggedPathsOrObjects);
                current.Use();
            }
        }

        public void AddFrames(SVGAsset[] assets)
        {
            Undo.RecordObject(target, "Add Frames");
            SVGFrameAnimator frameAnimator = target as SVGFrameAnimator;
            if(frameAnimator == null) return;
            if(frameAnimator.frames == null || frameAnimator.frames.Length == 0)
            {
                frameAnimator.frames = (SVGAsset[])assets.Clone();
            } else {
                int start = frameAnimator.frames.Length;
                int end = frameAnimator.frames.Length + assets.Length;
                System.Array.Resize<SVGAsset>(ref frameAnimator.frames, frameAnimator.frames.Length + assets.Length);
                for(int i = start; i < end; i++)
                {
                    frameAnimator.frames[i] = assets[i - start];
                }
            }
            EditorUtility.SetDirty(target);
        }
    }
}