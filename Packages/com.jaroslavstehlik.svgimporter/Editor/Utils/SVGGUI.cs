using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Utils
{
	using Rendering;

	public class SVGGUI
	{		
		public static GUIStyle helpBox
		{
			get {
#if UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
				return typeof(EditorStyles).GetProperty("helpBox", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null, null) as GUIStyle;
#else
				return EditorStyles.helpBox;
#endif
			}
		}

		static Material _invertMaterial;
		public static void ApplyInvertMaterial()
		{
			if(_invertMaterial == null)
			{
				_invertMaterial = new Material(Shader.Find("SVG Importer/Utils/InverseShader"));
			}
			_invertMaterial.SetPass(0);
		}
		
		public static void HilightLayer(SVGLayer layer)
		{
			if(layer.shapes == null) return;
			int shapesLength = layer.shapes.Length;
			for(int i = 0; i < shapesLength; i++)
			{
				HilightShape(layer.shapes[i]);
			}
		}

		public static void HilightShape(SVGShape shape)
		{
			if(shape.triangles == null || shape.vertices == null) return;
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			Color c = Handles.color * new Color(1f, 1f, 1f, 0.25f);
			
			ApplyInvertMaterial();
			GL.PushMatrix();
			GL.MultMatrix(Handles.matrix);
			GL.Begin(GL.TRIANGLES);
			GL.Color(c);
			
			int trianglesLength = shape.triangles.Length;
			Vector2 vertex;
			for(int i = 0; i < trianglesLength; i+=3)
			{
				vertex = shape.vertices[shape.triangles[i]];
				GL.Vertex3(vertex.x, vertex.y, 0f);
				
				vertex = shape.vertices[shape.triangles[i + 1]];
				GL.Vertex3(vertex.x, vertex.y, 0f);
				
				vertex = shape.vertices[shape.triangles[i + 2]];
				GL.Vertex3(vertex.x, vertex.y, 0f);
			}
			
			GL.End();
			GL.PopMatrix();
		}
	}

	public class SVGLayerList
	{
		public int maxVisibleItems = 10;
		public int scrollIndex;
		public float itemHeight = 20;
		public int hilightedLayer;

		float scroll;

		public void DoLayout(SVGLayer[] layers, LayerSelection layerSelection)
		{
			if(layers == null) return;

			float width = EditorGUIUtility.currentViewWidth - 55f;
			float height = itemHeight * Mathf.Min(layers.Length, maxVisibleItems);

			Rect rect = GUILayoutUtility.GetRect(width, height);
			rect.width = width;
			Rect finRect = new Rect(rect.position.x - rect.width * 0.5f, rect.position.y - itemHeight * 0.5f, rect.width, itemHeight);

			int layersLength = scrollIndex + Mathf.Clamp(layers.Length - scrollIndex, 0, maxVisibleItems);
			int index = 0;

			if(rect.Contains(Event.current.mousePosition))
			{
				hilightedLayer = Mathf.Clamp(Mathf.FloorToInt((Event.current.mousePosition.y - rect.y) / itemHeight), 0, layers.Length);
				if(Event.current.type == EventType.ScrollWheel)
				{
					scrollIndex = Mathf.Clamp(scrollIndex + (int)Mathf.Sign(Event.current.delta.y), 0, layers.Length);
					Event.current.Use();
				}
			} else {
				hilightedLayer = -1;
			}

			bool selected;
			bool lastSelected;
			for(int i = scrollIndex; i < layersLength; i++)
			{
				finRect.x = rect.x;
				finRect.y = rect.y + index * itemHeight;

				lastSelected = layerSelection.Contains(i);
				selected = GUI.Toggle(finRect, layerSelection.Contains(i), GUIContent.none);
				if(selected != lastSelected)
				{
					if(selected)
					{
						layerSelection.Add(i);
					}
					else 
					{
						layerSelection.Remove(i);
					}
				}

				finRect.x += itemHeight;
				GUI.Label(finRect, i+" "+layers[i].name);
				index++;
			}

			if(layers.Length > maxVisibleItems)
			{
				finRect.x = width + 20;
				finRect.y = rect.y;
				finRect.width = 35f;
				finRect.height = rect.height;
				scrollIndex = Mathf.Clamp((int)GUI.VerticalScrollbar(finRect, scrollIndex, maxVisibleItems, 0f, layers.Length), 0, layers.Length);
			} else {
				scrollIndex = 0;
			}
		}
	}
}