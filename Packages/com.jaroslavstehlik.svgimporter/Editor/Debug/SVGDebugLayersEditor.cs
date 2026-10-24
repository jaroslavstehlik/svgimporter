using UnityEngine;
using UnityEditor;
using System.Collections;
using SVGImporter;

namespace SVGImporter.Utils
{        
	[CustomEditor(typeof(SVGDebugLayers))]
	public class SVGDebugLayersEditor : Editor {

		SVGAsset currentAsset;
		SVGLayer hilightedLayer;
		SVGShape hilightedShape;
		protected bool hilighted;

		public override bool RequiresConstantRepaint ()
		{
			return true;
		}

	    public override void OnInspectorGUI()
	    {
			SVGDebugLayers go = target as SVGDebugLayers;
			SVGRenderer svgrenderer = go.GetComponent<SVGRenderer>();
			if(svgrenderer == null || svgrenderer.vectorGraphics == null) return;
			currentAsset = svgrenderer.vectorGraphics;
			SVGLayer[] svgLayers = currentAsset.layers;
			if(svgLayers == null) return;

			for(int i = 0; i < svgLayers.Length; i++)
			{
				string layerName = svgLayers[i].name;
				GUILayout.Label(layerName, SVGGUI.helpBox);
				Rect lastRect = GUILayoutUtility.GetLastRect();
				if(lastRect.Contains(Event.current.mousePosition))
				{
					hilightedLayer = svgLayers[i];
					SceneView.RepaintAll();
				}
				if(svgLayers[i].shapes != null)
				{
					for(int j = 0; j < svgLayers[i].shapes.Length; j++)
					{
						GUILayout.Label("	"+layerName + " "+svgLayers[i].shapes[j].type.ToString(), SVGGUI.helpBox);
						lastRect = GUILayoutUtility.GetLastRect();
						if(lastRect.Contains(Event.current.mousePosition))
						{
							hilightedShape = svgLayers[i].shapes[j];
							SceneView.RepaintAll();
						}
					}
				}
			}
	    }

		void OnSceneGUI()
		{
			if(currentAsset != null)
			{
				DebugPoints(hilightedLayer, currentAsset);
				DebugPoints(hilightedShape, currentAsset);
			}
		}

		void DebugPoints(SVGLayer svgLayer, SVGAsset svgAsset)
		{
			if(Event.current.type == EventType.Repaint)
			{
				SVGDebugLayers go = target as SVGDebugLayers;
				Matrix4x4 handlesMatrix = Handles.matrix;
				Handles.matrix = go.transform.localToWorldMatrix;

				if(svgLayer.shapes == null || svgLayer.shapes.Length == 0) return;

				for(int i = 0; i < svgLayer.shapes.Length; i++)
				{
					Vector3 lastPosition = svgLayer.shapes[i].vertices[0];
					Vector3 currentPosition;

					for(int j = 1; j < svgLayer.shapes[i].vertexCount; j++)
					{
						currentPosition = svgLayer.shapes[i].vertices[j];
						Handles.DrawLine(lastPosition, currentPosition);
						lastPosition = currentPosition;
					}
				}
				Handles.matrix = handlesMatrix;
			}
	    }

		void DebugPoints(SVGShape svgShape, SVGAsset svgAsset)
		{
			if(svgShape.vertexCount == 0) return;
			if(Event.current.type == EventType.Repaint)
			{
				SVGDebugLayers go = target as SVGDebugLayers;
				Matrix4x4 handlesMatrix = Handles.matrix;
				Handles.matrix = go.transform.localToWorldMatrix;

				Vector3 lastPosition = svgShape.vertices[0];
				Vector3 currentPosition;
				
				for(int j = 1; j < svgShape.vertexCount; j++)
				{
					currentPosition = svgShape.vertices[j];
					Handles.DrawLine(lastPosition, currentPosition);
					lastPosition = currentPosition;
				}
				Handles.matrix = handlesMatrix;
			}
		}
	}
}
