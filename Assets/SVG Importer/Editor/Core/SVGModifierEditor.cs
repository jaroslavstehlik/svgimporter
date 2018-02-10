// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SVGImporter
{
	using Utils;

	[CustomEditor(typeof(SVGModifier), true)]
	[CanEditMultipleObjects]
	public class SVGModifierEditor : Editor 
	{		
		SerializedProperty useSelection;
		SerializedProperty manualUpdate;
		SVGModifier modifier;
		SVGLayerList layerList;

		public virtual void OnEnable()
		{
			useSelection = serializedObject.FindProperty("useSelection");
			manualUpdate = serializedObject.FindProperty("manualUpdate");
			modifier = (SVGModifier)target;
			layerList = new SVGLayerList();

			SceneView.onSceneGUIDelegate += this.OnSceneView;
			EditorApplication.update += Update;
		}

		public virtual void OnFocus()
		{
			// Remove and re-add the sceneGUI delegate
			SceneView.onSceneGUIDelegate -= this.OnSceneView;
			SceneView.onSceneGUIDelegate += this.OnSceneView;
			EditorApplication.update -= Update;
			EditorApplication.update += Update;
		}

		public virtual void OnDisable()
		{
            SVGModifier._internal_selectingModifier = null;
            SceneView.onSceneGUIDelegate -= this.OnSceneView;
			EditorApplication.update -= Update;
		}

		void Update()
		{
			if(lastSceneView != null)
			{
				lastSceneView.Repaint();
			}
		}

		public void ValidateAsset()
		{
			bool validSVGAsset = true;
			for(int i = 0; i < targets.Length; i++)
			{
				SVGModifier modifier = targets[i] as SVGModifier;
				if(modifier == null) continue;
				if(modifier.svgRenderer == null) continue;
				if(modifier.svgRenderer.vectorGraphics == null) continue;
				if(!modifier.svgRenderer.vectorGraphics.useLayers) 
				{
					validSVGAsset = false;
					break;
				}
			}
			
			if(!validSVGAsset)
			{
				EditorGUILayout.HelpBox("To use SVG Modifiers please enable the SVG Layers option on your SVG Asset", MessageType.Error);
			}
		}

		public float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
		{
			return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}
		
		public bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
		{
			if(v1 == v2 && v1 == v3)
			{
				return pt == v1;
			} else {
				bool b1, b2, b3;
				
				b1 = Sign(pt, v1, v2) < 0.0f;
				b2 = Sign(pt, v2, v3) < 0.0f;
				b3 = Sign(pt, v3, v1) < 0.0f;
				
				return ((b1 == b2) && (b2 == b3));
			}
		}

		public bool InsideShape(SVGShape shape, Vector2 position)
		{
			if(shape.bounds.Contains(position))
			{
				for(int i = 0; i < shape.triangles.Length; i+=3)
				{
					if(PointInTriangle(position, 
					                   shape.vertices[shape.triangles[i]], 
					                   shape.vertices[shape.triangles[i + 1]],
					                   shape.vertices[shape.triangles[i + 2]]))
					{
						return true;
					}
				}
			}

			return false;
		}

		public void ManualUpdateGUI()
		{
			
		}

		public override void OnInspectorGUI ()
		{
			serializedObject.Update();
			EditorGUI.BeginChangeCheck();
            SelectionGUI();
            if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				SceneView.RepaintAll();
			}

			ValidateAsset();
			base.OnInspectorGUI ();
		}

		public void BaseOnInspectorGUI ()
		{
			base.OnInspectorGUI ();
		}

		public void SelectionGUI()
		{
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(manualUpdate);
            SVGAsset svgAsset = GetSVGAsset();
			if(svgAsset == null) return;
			SVGLayer[] svgLayers = svgAsset.layers;
			if(svgLayers == null || svgLayers.Length == 0) return;
			int layersLength = svgLayers.Length;

			EditorGUILayout.PropertyField(useSelection);
            EditorGUILayout.EndVertical();
            if (useSelection.boolValue)
			{
                GUILayout.BeginVertical();
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();

                bool selectingLayers = SVGModifier._internal_selectingModifier == modifier;
                Color guiColor = GUI.color;
                if (selectingLayers)
                {
                    GUI.color = Color.red;
                    if (GUILayout.Button("Select", "Button"))
                    {                        
                        SVGModifier._internal_selectingModifier = null;
                        selectingLayers = false;
                        SceneView.RepaintAll();
                    }
                } else
                {
                    GUI.color = Color.white;
                    if (GUILayout.Button("Select", "Button"))
                    {
                        SVGModifier._internal_selectingModifier = modifier;
                        selectingLayers = true;
                        SceneView.RepaintAll();
                    }
                }
                GUI.color = guiColor;

                if (GUILayout.Button("Clear"))
				{
					modifier.layerSelection.Clear();
				}
				if(GUILayout.Button("Invert"))
				{
					HashSet<int> cache = new HashSet<int>(modifier.layerSelection.cache);
					modifier.layerSelection.Clear();
					for(int i = 0; i < layersLength; i++)
					{
						if(cache.Contains(i)) continue;
						modifier.layerSelection.Add(i);
					}
				}
                GUILayout.EndVertical();
				GUILayout.EndHorizontal();				
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                if (selectingLayers)
				{
					layerList.DoLayout(svgLayers, modifier.layerSelection);
				}

			} else {

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                SVGModifier._internal_selectingModifier = null;
			}
		}

		public SVGAsset GetSVGAsset()
		{
			return modifier.svgRenderer.vectorGraphics;
		}

		static void DrawRect(Rect rect, Matrix4x4 matrix = default(Matrix4x4))
		{
			if(matrix == default(Matrix4x4)) matrix = Matrix4x4.identity;

			Vector3 min = rect.min;
			Vector3 max = rect.max;
			Handles.DrawLine(matrix.MultiplyPoint(new Vector3(min.x, min.y, 0f)), matrix.MultiplyPoint(new Vector3(max.x, min.y, 0f)));
			Handles.DrawLine(matrix.MultiplyPoint(new Vector3(max.x, min.y, 0f)), matrix.MultiplyPoint(new Vector3(max.x, max.y, 0f)));
			Handles.DrawLine(matrix.MultiplyPoint(new Vector3(max.x, max.y, 0f)), matrix.MultiplyPoint(new Vector3(min.x, max.y, 0f)));
			Handles.DrawLine(matrix.MultiplyPoint(new Vector3(min.x, max.y, 0f)), matrix.MultiplyPoint(new Vector3(min.x, min.y, 0f)));
		}

		static void DrawPath(Vector2[] path, Matrix4x4 matrix = default(Matrix4x4))
		{
			if(matrix == default(Matrix4x4)) matrix = Matrix4x4.identity;

			Vector2 lastPoint = matrix.MultiplyPoint(path[path.Length - 1]);
			Vector2 currentPoint;
			for(int i = 0; i < path.Length; i++)
			{
				currentPoint = matrix.MultiplyPoint(path[i]);
				Handles.DrawLine(lastPoint, currentPoint);
				lastPoint = currentPoint;
			}
		}

		static void DrawPath(Vector3[] path, Matrix4x4 matrix = default(Matrix4x4))
		{
			if(matrix == default(Matrix4x4)) matrix = Matrix4x4.identity;
			
			Vector3 lastPoint = matrix.MultiplyPoint(path[path.Length - 1]);
			Vector3 currentPoint;
			for(int i = 0; i < path.Length; i++)
			{
				currentPoint = matrix.MultiplyPoint(path[i]);
				Handles.DrawLine(lastPoint, currentPoint);
				lastPoint = currentPoint;
			}
		}

		public static Vector2 GUIPointToWorld(Vector2 gui)
		{
			Camera current = Camera.current;
			if (current)
			{
                #if UNITY_5_4_OR_NEWER
                gui = EditorGUIUtility.PointsToPixels(gui);
                gui.y = (float)Screen.height - gui.y - (35f * EditorGUIUtility.pixelsPerPoint);
                #else
                gui.y = (float)Screen.height - gui.y - 35f;
                #endif
                return current.ScreenToWorldPoint(new Vector3(gui.x, gui.y, current.nearClipPlane));
			}
			return gui;
		}

		public int GetHighestLayerAtPoint(SVGLayer[] layers, Vector2 mousePosition)
		{
			int index = -1;
			if(layers != null)
			{
				int layersLength = layers.Length;
				bool foundLayer = false;
				for(index = layersLength - 1; index >= 0; index--)
				{
					int shapesLength = layers[index].shapes.Length;
					for(int j = 0; j < shapesLength; j++)
					{
						if(InsideShape(layers[index].shapes[j], mousePosition))
						{
							foundLayer = true;
							break;
						}
					}
					if(foundLayer) break;
				}		
			}

			return index;
		}

		Vector2 clickGUIPosition;
		int controlIDHash = "SVGImporter_SVGModifierEditor_Select".GetHashCode();
		SceneView lastSceneView;
		void OnSceneView(SceneView sceneView)
		{
			lastSceneView = sceneView;
            if (!useSelection.boolValue) SVGModifier._internal_selectingModifier = null;

			Rect position = sceneView.position;
			position = new Rect(0f, 0f, position.size.x, position.size.y);

			Event current = Event.current;
			Vector2 localMousePosition = modifier.transform.worldToLocalMatrix.MultiplyPoint(GUIPointToWorld(current.mousePosition));
			int layerIndex;

			if(SVGModifier._internal_selectingModifier == modifier)
			{
                SVGAsset svgAsset = GetSVGAsset();
                
                if (position.Contains(current.mousePosition))
				{					
					if(current.button == 0)
					{
						switch (current.type)
						{						
						case EventType.mouseDown:
                                int controlID = GUIUtility.GetControlID(controlIDHash, FocusType.Passive);
                                GUIUtility.hotControl = controlID;
                                clickGUIPosition = current.mousePosition;
							break;
						case EventType.mouseUp:
							if((current.mousePosition - clickGUIPosition).sqrMagnitude < 0.1f)
							{
								layerIndex = GetHighestLayerAtPoint(svgAsset.layers, localMousePosition);
								if(current.shift)
								{
									if(layerIndex >= 0)
										modifier.layerSelection.Add(layerIndex);
								} else if(current.alt) {
									if(layerIndex >= 0)
										modifier.layerSelection.Remove(layerIndex);
								} else {
									modifier.layerSelection.Clear();
									if(layerIndex >= 0)
										modifier.layerSelection.Add(layerIndex);
								}
							}
							current.Use();
                                GUIUtility.hotControl = 0;
                                break;
						}
					}
				} else {
                    if (layerList.hilightedLayer >= 0)
					{
						SVGLayer[] layers = svgAsset.layers;
						if(layerList.hilightedLayer < layers.Length)
						{
							SVGGUI.HilightLayer(layers[layerList.hilightedLayer]);
						}
					}
				}

				if (GUI.changed) EditorUtility.SetDirty(target);

				if(current.type == EventType.repaint)
				{
					Color handlesColor = Handles.color;
					Handles.color = new Color(1f, 1f, 1f, 0.5f);
					if(svgAsset != null)
					{
						Matrix4x4 transform = modifier.transform.localToWorldMatrix;
						SVGLayer[] layers = svgAsset.layers;
						if(layers != null)
						{
							int index = 0;
							int layersLength = layers.Length;
							for(index = 0; index < layersLength; index++)
							{
								if(modifier.layerSelection.Contains(index))
								{
									for(int j = 0; j < layers[index].shapes.Length; j++)
									{
										Rect bounds = layers[index].shapes[j].bounds;
										DrawRect(bounds, transform);
									}
								}
							}
						}
					}

					Handles.color = handlesColor;

                    if(position.Contains(current.mousePosition))
                    {                                            
    					layerIndex = GetHighestLayerAtPoint(svgAsset.layers, localMousePosition);
    					if(layerIndex >= 0)
    					{
    						Matrix4x4 lastHandlesMatrix = Handles.matrix;
    						Handles.matrix = modifier.transform.localToWorldMatrix;
    						SVGGUI.HilightLayer(svgAsset.layers[layerIndex]);
    						Handles.matrix = lastHandlesMatrix;
    					}
                    }
				}
			}
		}
	}
}
