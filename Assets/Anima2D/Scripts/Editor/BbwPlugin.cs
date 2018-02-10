using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Anima2D 
{
	public class BbwPlugin
	{
		[DllImport ("Anima2D")]
		private static extern int Bbw([In,Out] IntPtr vertices, int vertexCount, int originalVertexCount,
		                               [In,Out] IntPtr indices, int indexCount,
		                               [In,Out] IntPtr edges, int edgesCount,
		                               [In,Out] IntPtr controlPoints, int controlPointsCount,
		                               [In,Out] IntPtr boneEdges, int boneEdgesCount,
		                               [In,Out] IntPtr weights
		);

		[DllImport ("Anima2D")]
		private static extern void SaveData([In,Out] IntPtr vertices, int vertexCount, 
			                              [In,Out] IntPtr indices, int indexCount,
			                              [In,Out] IntPtr edges, int edgesCount,
			                              [In,Out] IntPtr controlPoints, int controlPointsCount,
			                              [In,Out] IntPtr boneEdges, int boneEdgesCount);

		public static void CalculateBbw(Vector2[] vertices, IndexedEdge[] edges, Vector2[] controlPoints, IndexedEdge[] controlPointEdges, out float[,] weights)
		{
			Vector2[] sampledEdges = SampleEdges(controlPoints,controlPointEdges,10);

			List<Vector2> verticesAndSamplesList = new List<Vector2>(vertices.Length + sampledEdges.Length);

			verticesAndSamplesList.AddRange(vertices);
			verticesAndSamplesList.AddRange(sampledEdges);

			List<IndexedEdge> edgesList = new List<IndexedEdge>(edges);
			List<Hole> holes = new List<Hole>();
			List<int> indicesList = new List<int>();

			SpriteMeshUtils.Tessellate(verticesAndSamplesList,edgesList,holes,indicesList, 4f);

			Vector2[] verticesAndSamples = verticesAndSamplesList.ToArray();
			int[] indices = indicesList.ToArray();

			weights = new float[controlPointEdges.Length,vertices.Length];

			GCHandle verticesHandle = GCHandle.Alloc(verticesAndSamples, GCHandleType.Pinned);
			GCHandle indicesHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
			GCHandle edgesHandle = GCHandle.Alloc(edges, GCHandleType.Pinned);
			GCHandle controlPointsHandle = GCHandle.Alloc(controlPoints, GCHandleType.Pinned);
			GCHandle boneEdgesHandle = GCHandle.Alloc(controlPointEdges, GCHandleType.Pinned);
			GCHandle weightsHandle = GCHandle.Alloc(weights, GCHandleType.Pinned);

			/*
			SaveData(verticesHandle.AddrOfPinnedObject(), vertices.Length,
	                 indicesHandle.AddrOfPinnedObject(), indices.Length,
	                 edgesHandle.AddrOfPinnedObject(),edges.Length,
	                 controlPointsHandle.AddrOfPinnedObject(), controlPoints.Length,
			         boneEdgesHandle.AddrOfPinnedObject(), controlPointEdges.Length);
			 */

			Bbw(verticesHandle.AddrOfPinnedObject(), verticesAndSamples.Length, vertices.Length,
			    indicesHandle.AddrOfPinnedObject(), indices.Length,
			    edgesHandle.AddrOfPinnedObject(), edges.Length,
			    controlPointsHandle.AddrOfPinnedObject(), controlPoints.Length,
			    boneEdgesHandle.AddrOfPinnedObject(), controlPointEdges.Length,
			    weightsHandle.AddrOfPinnedObject());

			verticesHandle.Free();
			indicesHandle.Free();
			edgesHandle.Free();
			controlPointsHandle.Free();
			boneEdgesHandle.Free();
			weightsHandle.Free();
		}

		static Vector2[] SampleEdges(Vector2[] controlPoints, IndexedEdge[] controlPointEdges, int samplesPerEdge)
		{
			int totalCount = controlPoints.Length + samplesPerEdge * controlPointEdges.Length;
				
			List<Vector2> sampledVertices = new List<Vector2>(totalCount);

			sampledVertices.AddRange(controlPoints);

			for(int i = 0; i < controlPointEdges.Length; i++)
			{
				IndexedEdge edge = controlPointEdges[i];

				Vector2 tip = controlPoints[edge.index1];
				Vector2 tail = controlPoints[edge.index2];

				for(int s=0; s < samplesPerEdge; s++)
				{
					float f = (s+1f)/(float)(samplesPerEdge+1f);
					sampledVertices.Add(f * tail + (1f-f)*tip);
				}
			}

			return sampledVertices.ToArray();
		}
	}
}
