// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using System.Reflection;

using UnityEditorInternal;
using UnityEngine;
using UnityEditor;

namespace SVGImporter
{
	internal class SVGUtilityWindow : EditorWindow
	{
		//
		// Static Fields
		//
		protected const float k_MaxZoom = 10f;
		
		protected const float k_MinZoomPercentage = 0.9f;
		
		protected const float k_WheelZoomSpeed = 0.03f;
		
		protected static SVGUtilityWindow.Styles s_Styles;
		
		protected const float k_MouseZoomSpeed = 0.005f;
		
		protected const float k_InspectorHeight = 148f;
		
		protected const float k_ScrollbarMargin = 16f;
		
		protected const float k_BorderMargin = 10f;
		
		protected const float k_InspectorWidth = 330f;
		
		protected const float k_InspectorWindowMargin = 8f;
		
		//
		// Fields
		//
		protected float m_Zoom = -1f;
		
//		protected bool m_ShowAlpha;
		
		protected Vector2 m_ScrollPosition = default(Vector2);
		
//		protected float m_MipLevel;
		
		protected Texture2D m_Texture;
		
		protected Rect m_TextureViewRect;
		
		protected Rect m_TextureRect;
		
		//
		// Properties
		//
		protected Rect maxRect
		{
			get
			{
				float num = this.m_TextureViewRect.width * 0.5f / this.GetMinZoom();
				float num2 = this.m_TextureViewRect.height * 0.5f / this.GetMinZoom();
				float left = -num;
				float top = -num2;
				float width = (float)this.m_Texture.width + num * 2f;
				float height = (float)this.m_Texture.height + num2 * 2f;
				return new Rect(left, top, width, height);
			}
		}
		
		protected Rect maxScrollRect
		{
			get
			{
				float num = (float)this.m_Texture.width * 0.5f * this.m_Zoom;
				float num2 = (float)this.m_Texture.height * 0.5f * this.m_Zoom;
				return new Rect(-num, -num2, this.m_TextureViewRect.width + num * 2f, this.m_TextureViewRect.height + num2 * 2f);
			}
		}

		protected void DoTextureGUI()
		{
			if (this.m_Texture == null)
			{
				return;
			}
			if (this.m_Zoom < 0f)
			{
				this.m_Zoom = this.GetMinZoom();
			}
			this.m_TextureRect = new Rect(this.m_TextureViewRect.width / 2f - (float)this.m_Texture.width * this.m_Zoom / 2f, this.m_TextureViewRect.height / 2f - (float)this.m_Texture.height * this.m_Zoom / 2f, (float)this.m_Texture.width * this.m_Zoom, (float)this.m_Texture.height * this.m_Zoom);
			this.HandleScrollbars();
			this.SetupHandlesMatrix();
			this.HandleZoom();
			this.HandlePanning();
			this.DrawScreenspaceBackground();
			if (Event.current.type == EventType.Repaint)
			{
				this.DrawTexturespaceBackground();
				this.DrawGizmos();
			}
			this.DoTextureGUIExtras();
		}
		
		protected virtual void DoTextureGUIExtras()
		{
		}
		
		protected virtual void DrawGizmos()
		{
		}
		
		protected void DrawScreenspaceBackground()
		{
			if (Event.current.type == EventType.Repaint)
			{
				SVGUtilityWindow.s_Styles.preBackground.Draw(this.m_TextureViewRect, false, false, false, false);
			}
		}

		protected void DrawTexturespaceBackground()
		{
			float num = Mathf.Max(this.maxRect.width, this.maxRect.height);
			Vector2 b = new Vector2(this.maxRect.xMin, this.maxRect.yMin);
			float num2 = num * 0.5f;
			float a = (!EditorGUIUtility.isProSkin) ? 0.08f : 0.15f;
			float num3 = 8f;
			SVGEditorUtility.BeginLines(new Color(0f, 0f, 0f, a));
			for (float num4 = 0f; num4 <= num; num4 += num3)
			{
				SVGEditorUtility.DrawLine(new Vector2(-num2 + num4, num2 + num4) + b, new Vector2(num2 + num4, -num2 + num4) + b);
			}
			SVGEditorUtility.EndLines();
		}
		
		protected float GetMinZoom()
		{
			if (this.m_Texture == null)
			{
				return 1f;
			}
			return Mathf.Min(this.m_TextureViewRect.width / (float)this.m_Texture.width, this.m_TextureViewRect.height / (float)this.m_Texture.height) * 0.9f;
		}
		
		protected void HandlePanning()
		{
			bool flag = (!Event.current.alt && Event.current.button > 0) || (Event.current.alt && Event.current.button <= 0);
			if (flag && GUIUtility.hotControl == 0)
			{
				EditorGUIUtility.AddCursorRect(this.m_TextureViewRect, MouseCursor.Pan);
				if (Event.current.type == EventType.MouseDrag)
				{
					this.m_ScrollPosition -= Event.current.delta;
					Event.current.Use();
				}
			}
			if (((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) && flag) || ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown) && Event.current.keyCode == KeyCode.LeftAlt))
			{
				base.Repaint();
			}
		}
		
		protected void HandleScrollbars()
		{
			Rect position = new Rect(this.m_TextureViewRect.xMin, this.m_TextureViewRect.yMax, this.m_TextureViewRect.width, 16f);
			this.m_ScrollPosition.x = GUI.HorizontalScrollbar(position, this.m_ScrollPosition.x, this.m_TextureViewRect.width, this.maxScrollRect.xMin, this.maxScrollRect.xMax);
			Rect position2 = new Rect(this.m_TextureViewRect.xMax, this.m_TextureViewRect.yMin, 16f, this.m_TextureViewRect.height);
			this.m_ScrollPosition.y = GUI.VerticalScrollbar(position2, this.m_ScrollPosition.y, this.m_TextureViewRect.height, this.maxScrollRect.yMin, this.maxScrollRect.yMax);
		}
		
		protected void HandleZoom()
		{
			bool flag = Event.current.alt && Event.current.button == 1;
			if (flag)
			{
				EditorGUIUtility.AddCursorRect(this.m_TextureViewRect, MouseCursor.Zoom);
			}
			if (((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) && flag) || ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown) && Event.current.keyCode == KeyCode.LeftAlt))
			{
				base.Repaint();
			}
			if (Event.current.type == EventType.ScrollWheel || (Event.current.type == EventType.MouseDrag && Event.current.alt && Event.current.button == 1))
			{
				float num = 1f - Event.current.delta.y * ((Event.current.type != EventType.ScrollWheel) ? -0.005f : 0.03f);
				float num2 = this.m_Zoom * num;
				float num3 = Mathf.Clamp(num2, this.GetMinZoom(), 10f);
				if (num3 != this.m_Zoom)
				{
					this.m_Zoom = num3;
					if (num2 != num3)
					{
						num /= num2 / num3;
					}
					this.m_ScrollPosition *= num;
					Event.current.Use();
				}
			}
		}
		
		protected void InitStyles()
		{
			if (SVGUtilityWindow.s_Styles == null)
			{
				SVGUtilityWindow.s_Styles = new SVGUtilityWindow.Styles();
			}
		}
		
		private float Log2(float x)
		{
			return (float)(Math.Log((double)x) / Math.Log(2.0));
		}

		protected void SetNewTexture(Texture2D texture)
		{
			if (texture != this.m_Texture)
			{
				this.m_Texture = texture;
				this.m_Zoom = -1f;
			}
		}
		
		protected void SetupHandlesMatrix()
		{
			Vector3 pos = new Vector3(this.m_TextureRect.x, this.m_TextureRect.yMax, 0f);
			Vector3 s = new Vector3(this.m_Zoom, -this.m_Zoom, 1f);
			Handles.matrix = Matrix4x4.TRS(pos, Quaternion.identity, s);
		}

		internal static GUIContent TextContent(string content)
		{
			return (GUIContent)typeof(EditorGUIUtility).GetMethod("TextContent", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new []{content});
		}

		//
		// Nested Types
		//
		protected class Styles
		{
			public readonly GUIStyle dragdot = "U2D.dragDot";
			public readonly GUIStyle dragdotDimmed = "U2D.dragDotDimmed";
			public readonly GUIStyle dragdotactive = "U2D.dragDotActive";
			public readonly GUIStyle createRect = "U2D.createRect";
			public readonly GUIStyle preToolbar = "preToolbar";
			public readonly GUIStyle preButton = "preButton";
			public readonly GUIStyle preLabel = "preLabel";
			public readonly GUIStyle preSlider = "preSlider";
			public readonly GUIStyle preSliderThumb = "preSliderThumb";
			public readonly GUIStyle preBackground = "preBackground";
			public readonly GUIStyle pivotdotactive = "U2D.pivotDotActive";
			public readonly GUIStyle pivotdot = "U2D.pivotDot";
			public readonly GUIStyle dragBorderdot = new GUIStyle();
			public readonly GUIStyle dragBorderDotActive = new GUIStyle();
			public readonly GUIStyle toolbar;
			public readonly GUIContent alphaIcon;
			public readonly GUIContent RGBIcon;
			public readonly GUIStyle notice;
			public readonly GUIContent smallMip;
			public readonly GUIContent largeMip;
			public static readonly GUIContent[] spriteAlignmentOptions = new GUIContent[]
			{
				TextContent("SpriteInspector.Pivot.Center"),
				TextContent("SpriteInspector.Pivot.TopLeft"),
				TextContent("SpriteInspector.Pivot.Top"),
				TextContent("SpriteInspector.Pivot.TopRight"),
				TextContent("SpriteInspector.Pivot.Left"),
				TextContent("SpriteInspector.Pivot.Right"),
				TextContent("SpriteInspector.Pivot.BottomLeft"),
				TextContent("SpriteInspector.Pivot.Bottom"),
				TextContent("SpriteInspector.Pivot.BottomRight"),
				TextContent("SpriteInspector.Pivot.Custom")
			};
			public static GUIContent s_PivotLabel = TextContent("Pivot");
			public static GUIContent s_NoSelectionWarning = TextContent("SpriteEditor.NoTextureOrSpriteSelected");
			public Styles()
			{
				this.toolbar = new GUIStyle(EditorStyles.toolbar);
				this.toolbar.margin.top = 0;
				this.toolbar.margin.bottom = 0;
				this.alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
				this.RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
				this.preToolbar.border.top = 0;
				this.createRect.border = new RectOffset(3, 3, 3, 3);
				this.notice = new GUIStyle(GUI.skin.label);
				this.notice.alignment = TextAnchor.MiddleCenter;
				this.notice.normal.textColor = Color.yellow;
				this.dragBorderdot.fixedHeight = 5f;
				this.dragBorderdot.fixedWidth = 5f;
				this.dragBorderdot.normal.background = EditorGUIUtility.whiteTexture;
				this.dragBorderDotActive.fixedHeight = this.dragBorderdot.fixedHeight;
				this.dragBorderDotActive.fixedWidth = this.dragBorderdot.fixedWidth;
				this.dragBorderDotActive.normal.background = EditorGUIUtility.whiteTexture;
				this.smallMip = EditorGUIUtility.IconContent("PreTextureMipMapLow");
				this.largeMip = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
			}
		}
	}
}

