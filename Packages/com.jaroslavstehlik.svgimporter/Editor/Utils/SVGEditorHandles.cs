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

    
    //
    // Static Methods
    //
    
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

    public static void BeginLines(Color color)
    {
        typeof(HandleUtility).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).First(m => m.Name == "ApplyWireMaterial" && m.GetParameters().Length == 0).Invoke(null, null);
        GL.PushMatrix();
        GL.MultMatrix(Handles.matrix);
        GL.Begin(1);
        GL.Color(color);
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
