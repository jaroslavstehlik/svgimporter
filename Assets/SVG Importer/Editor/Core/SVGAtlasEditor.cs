// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
