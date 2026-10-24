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
	[CustomEditor(typeof(SVGAtlas))]
	public class SVGAtlasEditor : Editor
    {        
		protected Texture2D gradientTexture;
        public override void OnInspectorGUI()
        {
			base.OnInspectorGUI();

			if(gradientTexture == null)
			{
				gradientTexture = new Texture2D(128, 16);
			}

			SVGAtlas atlas = target as SVGAtlas;
			if(atlas.atlasData != null && atlas.atlasData.gradients != null)
			{
//				Debug.Log(atlas.atlasData);
				/*
				for(int i = 0; i < atlas.atlasData.gradients.Length; i++)
				{
					//SVGAtlas.RenderGradient(gradientTexture, atlas.atlasData.gradients[i], 0, 0, gradientTexture.width, gradientTexture.height);
					//GUILayout.Box(gradientTexture);
				}
				*/
			}
        }       
    }
}
