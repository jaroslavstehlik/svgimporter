using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using System.Linq;

public class SVGEditorHandles {

    //
    // Static Fields
    //
    public static Vector2 s_DragScreenOffset;

    public static int s_RectSelectionID = GUIUtility.GetControlID(FocusType.Keyboard);
    
    public static Vector2 s_CurrentMousePosition;
    
    public static Vector2 s_DragStartScreenPosition;

    public static bool s_OneClickDragStarted;

    public static Rect FromToRect(Vector2 start, Vector2 end)
    {
        Rect result = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
        if (result.width < 0f)
        {
            result.x += result.width;
            result.width = -result.width;
        }
        if (result.height < 0f)
        {
            result.y += result.height;
            result.height = -result.height;
        }
        return result;
    }

    
    //
    // Static Methods
    //
    public static Rect GetCurrentRect(bool screenSpace, float textureWidth, float textureHeight, Vector2 startPoint, Vector2 endPoint)
    {
        Rect rect = FromToRect(Handles.inverseMatrix.MultiplyPoint(startPoint), Handles.inverseMatrix.MultiplyPoint(endPoint));
        rect = ClampedRect(RoundToInt(rect), new Rect(0f, 0f, textureWidth, textureHeight), false);
        if (screenSpace)
        {
            Vector2 vector = Handles.matrix.MultiplyPoint(new Vector2(rect.xMin, rect.yMin));
            Vector2 vector2 = Handles.matrix.MultiplyPoint(new Vector2(rect.xMax, rect.yMax));
            rect = new Rect(vector.x, vector.y, vector2.x - vector.x, vector2.y - vector.y);
        }
        return rect;
    }
    
    public static void HandleSliderRectMouseDown(int id, Event evt, Rect pos)
    {
        GUIUtility.keyboardControl = id;
        GUIUtility.hotControl = id;
        SVGEditorHandles.s_CurrentMousePosition = evt.mousePosition;
        SVGEditorHandles.s_DragStartScreenPosition = evt.mousePosition;
        Vector2 b = Handles.matrix.MultiplyPoint(pos.center);
        SVGEditorHandles.s_DragScreenOffset = SVGEditorHandles.s_CurrentMousePosition - b;
        EditorGUIUtility.SetWantsMouseJumping(1);
    }
    
    public static Vector2 PivotSlider(Rect sprite, Vector2 pos, GUIStyle pivotDot, GUIStyle pivotDotActive)
    {
        int controlID = GUIUtility.GetControlID("Slider1D".GetHashCode(), FocusType.Keyboard);
        pos = new Vector2(sprite.xMin + sprite.width * pos.x, sprite.yMin + sprite.height * pos.y);
        Vector2 vector = Handles.matrix.MultiplyPoint(pos);
        Rect position = new Rect(vector.x - pivotDot.fixedWidth * 0.5f, vector.y - pivotDot.fixedHeight * 0.5f, pivotDotActive.fixedWidth, pivotDotActive.fixedHeight);
        Event current = Event.current;
        switch (current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (current.button == 0 && position.Contains(Event.current.mousePosition) && !current.alt)
                {
                    int num = controlID;
                    GUIUtility.keyboardControl = num;
                    GUIUtility.hotControl = num;
                    SVGEditorHandles.s_CurrentMousePosition = current.mousePosition;
                    SVGEditorHandles.s_DragStartScreenPosition = current.mousePosition;
                    Vector2 b = Handles.matrix.MultiplyPoint(pos);
                    SVGEditorHandles.s_DragScreenOffset = SVGEditorHandles.s_CurrentMousePosition - b;
                    current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID && (current.button == 0 || current.button == 2))
                {
                    GUIUtility.hotControl = 0;
                    current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(0);
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    SVGEditorHandles.s_CurrentMousePosition += current.delta;
                    Vector2 a = pos;
                    Vector3 vector2 = Handles.inverseMatrix.MultiplyPoint(SVGEditorHandles.s_CurrentMousePosition - SVGEditorHandles.s_DragScreenOffset);
                    pos = new Vector2(vector2.x, vector2.y);
                    if (!Mathf.Approximately((a - pos).magnitude, 0f))
                    {
                        GUI.changed = true;
                    }
                    current.Use();
                }
                break;
            case EventType.KeyDown:
                if (GUIUtility.hotControl == controlID && current.keyCode == KeyCode.Escape)
                {
                    pos = Handles.inverseMatrix.MultiplyPoint(SVGEditorHandles.s_DragStartScreenPosition - SVGEditorHandles.s_DragScreenOffset);
                    GUIUtility.hotControl = 0;
                    GUI.changed = true;
                    current.Use();
                }
                break;
            case EventType.Repaint:
				EditorGUIUtility.AddCursorRect(position, UnityEditor.MouseCursor.Arrow, controlID);
                if (GUIUtility.hotControl == controlID)
                {
                    pivotDotActive.Draw(position, GUIContent.none, controlID);
                }
                else
                {
                    pivotDot.Draw(position, GUIContent.none, controlID);
                }
                break;
        }
        pos = new Vector2((pos.x - sprite.xMin) / sprite.width, (pos.y - sprite.yMin) / sprite.height);
        return pos;
    }
    
	public static Vector2 PointSlider(Vector2 pos, UnityEditor.MouseCursor cursor, GUIStyle dragDot, GUIStyle dragDotActive)
    {
        int controlID = GUIUtility.GetControlID("Slider1D".GetHashCode(), FocusType.Keyboard);
        Vector2 vector = Handles.matrix.MultiplyPoint(pos);
        Rect rect = new Rect(vector.x - dragDot.fixedWidth * 0.5f, vector.y - dragDot.fixedHeight * 0.5f, dragDot.fixedWidth, dragDot.fixedHeight);
        Event current = Event.current;
        EventType typeForControl = current.GetTypeForControl(controlID);
        if (typeForControl == EventType.Repaint)
        {
            if (GUIUtility.hotControl == controlID)
            {
                dragDotActive.Draw(rect, GUIContent.none, controlID);
            }
            else
            {
                dragDot.Draw(rect, GUIContent.none, controlID);
            }
        }
        return SVGEditorHandles.ScaleSlider(pos, cursor, rect);
    }
    
    public static Rect RectCreator(float textureWidth, float textureHeight, GUIStyle rectStyle)
    {
        Event current = Event.current;
        Vector2 mousePosition = current.mousePosition;
        int num = SVGEditorHandles.s_RectSelectionID;
        Rect result = default(Rect);
        switch (current.GetTypeForControl(num))
        {
            case EventType.MouseDown:
                if (current.button == 0)
                {
                    GUIUtility.hotControl = num;
                    Rect rect = new Rect(0f, 0f, textureWidth, textureHeight);
                    Vector2 v = Handles.inverseMatrix.MultiplyPoint(mousePosition);
                    v.x = Mathf.Min(Mathf.Max(v.x, rect.xMin), rect.xMax);
                    v.y = Mathf.Min(Mathf.Max(v.y, rect.yMin), rect.yMax);
                    SVGEditorHandles.s_DragStartScreenPosition = Handles.matrix.MultiplyPoint(v);
                    SVGEditorHandles.s_CurrentMousePosition = mousePosition;
                    current.Use();
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == num && current.button == 0)
                {
                    if (SVGEditorHandles.ValidRect(SVGEditorHandles.s_DragStartScreenPosition, SVGEditorHandles.s_CurrentMousePosition))
                    {
                        result = SVGEditorHandles.GetCurrentRect(false, textureWidth, textureHeight, SVGEditorHandles.s_DragStartScreenPosition, SVGEditorHandles.s_CurrentMousePosition);
                        GUI.changed = true;
                        current.Use();
                    }
                    GUIUtility.hotControl = 0;
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == num)
                {
                    SVGEditorHandles.s_CurrentMousePosition = new Vector2(mousePosition.x, mousePosition.y);
                    current.Use();
                }
                break;
            case EventType.KeyDown:
                if (GUIUtility.hotControl == num && current.keyCode == KeyCode.Escape)
                {
                    GUIUtility.hotControl = 0;
                    GUI.changed = true;
                    current.Use();
                }
                break;
            case EventType.Repaint:
                if (GUIUtility.hotControl == num && SVGEditorHandles.ValidRect(SVGEditorHandles.s_DragStartScreenPosition, SVGEditorHandles.s_CurrentMousePosition))
                {
                    BeginLines(Color.green * 1.5f);
                    DrawBox(SVGEditorHandles.GetCurrentRect(false, textureWidth, textureHeight, SVGEditorHandles.s_DragStartScreenPosition, SVGEditorHandles.s_CurrentMousePosition));
                    EndLines();
                }
                break;
        }
        return result;
    }
    
	public static Vector2 ScaleSlider(Vector2 pos, UnityEditor.MouseCursor cursor, Rect cursorRect)
    {
        int controlID = GUIUtility.GetControlID("Slider1D".GetHashCode(), FocusType.Keyboard);
        return SVGEditorHandles.ScaleSlider(controlID, pos, cursor, cursorRect);
    }
    
	public static Vector2 ScaleSlider(int id, Vector2 pos, UnityEditor.MouseCursor cursor, Rect cursorRect)
    {
        Vector2 b = Handles.matrix.MultiplyPoint(pos);
        Event current = Event.current;
        switch (current.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (current.button == 0 && cursorRect.Contains(Event.current.mousePosition) && !current.alt)
                {
                    GUIUtility.keyboardControl = id;
                    GUIUtility.hotControl = id;
                    SVGEditorHandles.s_CurrentMousePosition = current.mousePosition;
                    SVGEditorHandles.s_DragStartScreenPosition = current.mousePosition;
                    SVGEditorHandles.s_DragScreenOffset = SVGEditorHandles.s_CurrentMousePosition - b;
                    current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id && (current.button == 0 || current.button == 2))
                {
                    GUIUtility.hotControl = 0;
                    current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(0);
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    SVGEditorHandles.s_CurrentMousePosition += current.delta;
                    Vector2 a = pos;
                    pos = Handles.inverseMatrix.MultiplyPoint(SVGEditorHandles.s_CurrentMousePosition);
                    if (!Mathf.Approximately((a - pos).magnitude, 0f))
                    {
                        GUI.changed = true;
                    }
                    current.Use();
                }
                break;
            case EventType.KeyDown:
                if (GUIUtility.hotControl == id && current.keyCode == KeyCode.Escape)
                {
                    pos = Handles.inverseMatrix.MultiplyPoint(SVGEditorHandles.s_DragStartScreenPosition - SVGEditorHandles.s_DragScreenOffset);
                    GUIUtility.hotControl = 0;
                    GUI.changed = true;
                    current.Use();
                }
                break;
            case EventType.Repaint:
                EditorGUIUtility.AddCursorRect(cursorRect, cursor, id);
                break;
        }
        return pos;
    }
    
    public static Rect SliderRect(Rect pos)
    {
        int controlID = GUIUtility.GetControlID("SliderRect".GetHashCode(), FocusType.Keyboard);
        Event current = Event.current;
        if (SVGEditorHandles.s_OneClickDragStarted && current.type == EventType.Repaint)
        {
            SVGEditorHandles.HandleSliderRectMouseDown(controlID, current, pos);
            SVGEditorHandles.s_OneClickDragStarted = false;
        }
        switch (current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (current.button == 0 && pos.Contains(Handles.inverseMatrix.MultiplyPoint(Event.current.mousePosition)) && !current.alt)
                {
                    SVGEditorHandles.HandleSliderRectMouseDown(controlID, current, pos);
                    current.Use();
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID && (current.button == 0 || current.button == 2))
                {
                    GUIUtility.hotControl = 0;
                    current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(0);
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    SVGEditorHandles.s_CurrentMousePosition += current.delta;
                    Vector2 center = pos.center;
                    pos.center = Handles.inverseMatrix.MultiplyPoint(SVGEditorHandles.s_CurrentMousePosition - SVGEditorHandles.s_DragScreenOffset);
                    if (!Mathf.Approximately((center - pos.center).magnitude, 0f))
                    {
                        GUI.changed = true;
                    }
                    current.Use();
                }
                break;
            case EventType.KeyDown:
                if (GUIUtility.hotControl == controlID && current.keyCode == KeyCode.Escape)
                {
                    pos.center = Handles.inverseMatrix.MultiplyPoint(SVGEditorHandles.s_DragStartScreenPosition - SVGEditorHandles.s_DragScreenOffset);
                    GUIUtility.hotControl = 0;
                    GUI.changed = true;
                    current.Use();
                }
                break;
            case EventType.Repaint:
            {
                Vector2 vector = Handles.inverseMatrix.MultiplyPoint(new Vector2(pos.xMin, pos.yMin));
                Vector2 vector2 = Handles.inverseMatrix.MultiplyPoint(new Vector2(pos.xMax, pos.yMax));
			EditorGUIUtility.AddCursorRect(new Rect(vector.x, vector.y, vector2.x - vector.x, vector2.y - vector.y), UnityEditor.MouseCursor.Arrow, controlID);
                break;
            }
        }
        return pos;
    }
    
    public static bool ValidRect(Vector2 startPoint, Vector2 endPoint)
    {
        return Mathf.Abs((endPoint - startPoint).x) > 5f && Mathf.Abs((endPoint - startPoint).y) > 5f;
    }

    public static void BeginLines(Color color)
    {
#if !UNITY_4_6
        typeof(HandleUtility).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).First(m => m.Name == "ApplyWireMaterial" && m.GetParameters().Length == 0).Invoke(null, null);
#endif
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
        DrawLine(array[0], array[1]);
        DrawLine(array[1], array[2]);
        DrawLine(array[2], array[3]);
        DrawLine(array[3], array[0]);
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

    public class Styles
    {       
        static GUIContent TextContent(string content)
        {
            return (GUIContent)typeof(EditorGUIUtility).GetMethod("TextContent", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new []{content});
        }
        
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
