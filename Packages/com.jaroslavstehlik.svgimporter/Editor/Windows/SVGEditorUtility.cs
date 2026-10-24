using System;
using System.Reflection;

using UnityEditor;
using UnityEngine;


namespace SVGImporter
{
	internal static class SVGEditorUtility
	{
		//
		// Static Methods
		//

		public static void BeginLines(Color color)
		{
			typeof(HandleUtility).GetMethod("ApplyWireMaterial", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
			GL.PushMatrix();
			GL.MultMatrix(Handles.matrix);
			GL.Begin(1);
			GL.Color(color);
		}
		
		public static Rect ClampedRect(Rect rect, Rect clamp, bool maintainSize)
		{
			Rect result = new Rect(rect);
			if (maintainSize)
			{
				Vector2 center = rect.center;
				if (center.x + Mathf.Abs(rect.width) * 0.5f > clamp.xMax)
				{
					center.x = clamp.xMax - rect.width * 0.5f;
				}
				if (center.x - Mathf.Abs(rect.width) * 0.5f < clamp.xMin)
				{
					center.x = clamp.xMin + rect.width * 0.5f;
				}
				if (center.y + Mathf.Abs(rect.height) * 0.5f > clamp.yMax)
				{
					center.y = clamp.yMax - rect.height * 0.5f;
				}
				if (center.y - Mathf.Abs(rect.height) * 0.5f < clamp.yMin)
				{
					center.y = clamp.yMin + rect.height * 0.5f;
				}
				result.center = center;
			}
			else
			{
				if (result.width > 0f)
				{
					result.xMin = Mathf.Max(rect.xMin, clamp.xMin);
					result.xMax = Mathf.Min(rect.xMax, clamp.xMax);
				}
				else
				{
					result.xMin = Mathf.Min(rect.xMin, clamp.xMax);
					result.xMax = Mathf.Max(rect.xMax, clamp.xMin);
				}
				if (result.height > 0f)
				{
					result.yMin = Mathf.Max(rect.yMin, clamp.yMin);
					result.yMax = Mathf.Min(rect.yMax, clamp.yMax);
				}
				else
				{
					result.yMin = Mathf.Min(rect.yMin, clamp.yMax);
					result.yMax = Mathf.Max(rect.yMax, clamp.yMin);
				}
			}
			result.width = Mathf.Abs(result.width);
			result.height = Mathf.Abs(result.height);
			return result;
		}
		
		public static void DrawBox(Rect position)
		{
			Vector3[] array = new Vector3[5];
			int num = 0;
			array[num++] = new Vector3(position.xMin, position.yMin, 0f);
			array[num++] = new Vector3(position.xMax, position.yMin, 0f);
			array[num++] = new Vector3(position.xMax, position.yMax, 0f);
			array[num++] = new Vector3(position.xMin, position.yMax, 0f);
			SVGEditorUtility.DrawLine(array[0], array[1]);
			SVGEditorUtility.DrawLine(array[1], array[2]);
			SVGEditorUtility.DrawLine(array[2], array[3]);
			SVGEditorUtility.DrawLine(array[3], array[0]);
		}
		
		public static void DrawLine(Vector3 p1, Vector3 p2)
		{
			GL.Vertex(p1);
			GL.Vertex(p2);
		}
		
		public static void EndLines()
		{
			GL.End();
			GL.PopMatrix();
		}
		
		public static Vector2 GetPivotValue(SpriteAlignment alignment, Vector2 customOffset)
		{
			switch (alignment)
			{
			case SpriteAlignment.Center:
				return new Vector2(0.5f, 0.5f);
			case SpriteAlignment.TopLeft:
				return new Vector2(0f, 1f);
			case SpriteAlignment.TopCenter:
				return new Vector2(0.5f, 1f);
			case SpriteAlignment.TopRight:
				return new Vector2(1f, 1f);
			case SpriteAlignment.LeftCenter:
				return new Vector2(0f, 0.5f);
			case SpriteAlignment.RightCenter:
				return new Vector2(1f, 0.5f);
			case SpriteAlignment.BottomLeft:
				return new Vector2(0f, 0f);
			case SpriteAlignment.BottomCenter:
				return new Vector2(0.5f, 0f);
			case SpriteAlignment.BottomRight:
				return new Vector2(1f, 0f);
			case SpriteAlignment.Custom:
				return customOffset;
			default:
				return Vector2.zero;
			}
		}
		
		public static Rect RoundedRect(Rect rect)
		{
			return new Rect((float)Mathf.RoundToInt(rect.xMin), (float)Mathf.RoundToInt(rect.yMin), (float)Mathf.RoundToInt(rect.width), (float)Mathf.RoundToInt(rect.height));
		}
		
		public static Rect RoundToInt(Rect r)
		{
			r.xMin = (float)Mathf.RoundToInt(r.xMin);
			r.yMin = (float)Mathf.RoundToInt(r.yMin);
			r.xMax = (float)Mathf.RoundToInt(r.xMax);
			r.yMax = (float)Mathf.RoundToInt(r.yMax);
			return r;
		}
	}
}
