// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;
using System.Reflection;
using System.IO;
	
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;

namespace SVGImporter
{
	using Utils;
	using Geometry;

	internal class SVGEditorWindow : EditorWindow
	{
        const int EMPTY_LAYER = 31;

		//
		// Static Fields
		//
		public static SVGEditorWindow s_Instance;
		protected static SVGAsset svgAsset;
		protected static Mesh _tempMesh;

        Camera _editorCamera;
        SVGRenderer _editorRenderer;

        int _previewResolution = 256;
        Vector2 viewScrollPosition;
        float viewZoom = 1f;
        bool viewAlpha;

        Rect textureOrigRect;
        Rect textureRect;
        Rect textureWindowRect;

        SVGEditorHandles.Styles styles;

        Vector2 originalPivotPoint;

		public static void GetWindow()
		{
			s_Instance = EditorWindow.GetWindow<SVGEditorWindow>(false, "SVG Asset Editor", true);
		}

        Bounds assetBounds
        {
            get {
                if(svgAsset.ignoreSVGCanvas)
                {
                    return svgAsset.bounds;
                } else {
                    Bounds bounds = svgAsset.bounds;
                    Vector2 canvasSize = svgAsset.canvasRectangle.size;
                    return new Bounds(new Vector3(-originalPivotPoint.x * canvasSize.x + canvasSize.x * 0.5f, originalPivotPoint.y * canvasSize.y - canvasSize.y * 0.5f, bounds.center.z), 
                                      new Vector3(canvasSize.x, canvasSize.y, bounds.size.z));
                }
            }
        }

		Rect windowRect
		{
			get {
				return new Rect(0f, 0f, this.position.width, this.position.height);
			}
		}

        void UpdateOriginalPivotPoint()
        {
            if(svgAsset != null)
            {
                originalPivotPoint = svgAsset.pivotPoint;
            }
        }

		void OnEnable()
		{
            GetSVGAsset();
            UpdateOriginalPivotPoint();
			base.minSize = new Vector2(360f, 200f);
			s_Instance = this;
			//Undo.undoRedoPerformed = (Undo.UndoRedoCallback)Delegate.Combine(Undo.undoRedoPerformed, new Undo.UndoRedoCallback(this.UndoRedoPerformed));
            CreateCamera();
            CreateSVGRenderer();
		}

        public void ManualUpdate()
        {
            RemoveCamera();
            RemoveSVGRenderer();
            CreateCamera();
            CreateSVGRenderer();
            OnSelectionChange();
        }

        long startRepaintTicks;

        void OnFocus()
        {
            UpdateEditorRendererPosition();
            Repaint();
        }

        void OnSelectionChange()
        {
            GetSVGAsset();
            UpdateOriginalPivotPoint();
            if(_editorRenderer != null){_editorRenderer.vectorGraphics = svgAsset;}
            UpdateEditorRendererPosition();
            Repaint();
        }

        void OnGUI () 
        {
            GetSVGAsset();
            if(styles == null)
            {
                styles = new SVGEditorHandles.Styles();
            }
            
            bool selectedValidObject = svgAsset != null && Selection.objects.Length == 1;
			if(selectedValidObject)
			{
				Mesh sharedMesh = svgAsset.sharedMesh;
				if(sharedMesh == null || sharedMesh.vertexCount == 0) 
					selectedValidObject = false;
			}
            
			if(!selectedValidObject)
            {
                SelectedWrongObject();              
            } else {
                SelectedCorrectObject();
            }
        }

        void OnDisable()
        {
            RemoveCamera();
            RemoveSVGRenderer();
        }

        void CreateCamera()
        {
            _editorCamera = editorCamera;
        }

        Camera editorCamera
        {
            get {
                if(_editorCamera == null)
                {
                    GameObject go = EditorUtility.CreateGameObjectWithHideFlags("SVG Editor Camera", HideFlags.HideAndDontSave, typeof(Camera));
                    _editorCamera = go.GetComponent<Camera>();
                    _editorCamera.hideFlags = HideFlags.HideAndDontSave;
                    _editorCamera.cullingMask = 1 << EMPTY_LAYER;
                    _editorCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    _editorCamera.clearFlags = CameraClearFlags.Color;
                    _editorCamera.orthographic = true;
                    _editorCamera.enabled = false;
                    //Debug.Log("Create Camera");
                }

                return _editorCamera;
            }
        }
        
        void RemoveCamera()
        {
            if(_editorCamera != null)
            {
                _editorCamera.targetTexture = null;
                DestroyImmediate(_editorCamera.gameObject);
                _editorCamera = null;
                //Debug.Log("Remove Camera");
            }
        }
        
        RenderTexture GetRenderTexture()
        {            
            float aspect = 1f;
            if(svgAsset != null) aspect = assetBounds.size.x / assetBounds.size.y;
            _previewResolution = Mathf.Clamp(Mathf.CeilToInt(windowRect.width), 0, 8192);
			return RenderTexture.GetTemporary(_previewResolution,
                                              Mathf.Clamp(Mathf.CeilToInt(_previewResolution / aspect), 0, 8192), 
			                                  24, 
			                                  RenderTextureFormat.Default, 
			                                  RenderTextureReadWrite.Default,
			                                  4);
        }
        
        void CreateSVGRenderer()
        {
            _editorRenderer = editorRenderer;
        }

        SVGRenderer editorRenderer
        {
            get {
                GetSVGAsset();
                if(svgAsset == null)
                {
                    RemoveSVGRenderer();
                    return null;
                }

                if(_editorRenderer == null)
                {
                    GameObject go = EditorUtility.CreateGameObjectWithHideFlags("editor SVG Renderer", HideFlags.HideAndDontSave, typeof(SVGRenderer));
                    go.layer = EMPTY_LAYER;
                    _editorRenderer = go.GetComponent<SVGRenderer>();
                    _editorRenderer.vectorGraphics = svgAsset;
                    _editorRenderer.gameObject.SetActive(false);
                    UpdateEditorRendererPosition();
                    //Debug.Log("Create SVG Renderer");
                }

                return _editorRenderer;
            }
        }

        void UpdateEditorRendererPosition()
        {
            if(svgAsset != null && _editorRenderer != null && editorCamera != null)
            {
                _editorRenderer.transform.position = editorCamera.transform.forward * (editorCamera.nearClipPlane + assetBounds.size.z + 1f) - assetBounds.center;
            }
        }
        
        void RenderSVGRenderer()
        {
            GetSVGAsset();
            if(svgAsset == null)
                return;

            editorRenderer.vectorGraphics = svgAsset;

            if(assetBounds.size.x > assetBounds.size.y)
            {
                editorCamera.orthographicSize = Mathf.Min(assetBounds.size.x, assetBounds.size.y) * 0.5f;
            } else {
                editorCamera.orthographicSize = Mathf.Max(assetBounds.size.x, assetBounds.size.y) * 0.5f;
            }

            _editorRenderer.gameObject.SetActive(true);
			editorCamera.targetTexture = GetRenderTexture();
            SVGAtlas.Instance.OnPreRender();
            editorCamera.Render();
            _editorRenderer.gameObject.SetActive(false);
        }
        
        void RemoveSVGRenderer()
        {
            if(_editorRenderer != null)
            {
                _editorRenderer.vectorGraphics = null;
                DestroyImmediate(_editorRenderer.gameObject);
                _editorRenderer = null;
                //Debug.Log("Remove SVG Renderer");
            }
        }

        void GetSVGAsset()
        {
            svgAsset = Selection.activeObject as SVGAsset;
        }

		void SelectedWrongObject()
		{
			GUILayout.Label("Please select single SVG Asset only.");
		}

		void SelectedCorrectObject()
		{
            GetSVGAsset();
            UpdateEditorRendererPosition();

            textureWindowRect = new Rect(windowRect.x, windowRect.y + 16f, windowRect.width - 16f, windowRect.height - 32f);

            float widthScale = windowRect.width / assetBounds.size.x;
            float heightScale = windowRect.height / assetBounds.size.y;
            float scale = Mathf.Min(widthScale, heightScale);
            float finalWidth = assetBounds.size.x * scale;
            float finalHeight = assetBounds.size.y * scale;

            textureOrigRect = new Rect(0f, 0f, finalWidth, finalHeight);
            textureRect = new Rect(textureWindowRect.width / 2f - finalWidth * viewZoom / 2f, textureWindowRect.height / 2f - finalHeight * viewZoom / 2f, finalWidth * viewZoom, finalHeight * viewZoom);
            textureRect.center += -viewScrollPosition;

            if (Event.current.type == EventType.Repaint)
            {
				#if UNITY_4_6
				MethodInfo SetTemporarilyAllowIndieRenderTexture = typeof(EditorUtility).GetMethod("SetTemporarilyAllowIndieRenderTexture", BindingFlags.Static | BindingFlags.NonPublic);
				SetTemporarilyAllowIndieRenderTexture.Invoke(null, new System.Object[]{(System.Object)true});
				#endif
				RenderSVGRenderer();
				if (viewAlpha)
				{
					EditorGUI.DrawTextureAlpha(textureRect, editorCamera.targetTexture);
				} else {
					EditorGUI.DrawTextureTransparent(textureRect, editorCamera.targetTexture);
				}

				RenderTexture.ReleaseTemporary(editorCamera.targetTexture);
				editorCamera.targetTexture = null;
				#if UNITY_4_6
				SetTemporarilyAllowIndieRenderTexture.Invoke(null, new System.Object[]{(System.Object)false});
				#endif
            }

            SetupHandlesMatrix();
            DoTextureGUIExtras();

            HandleScrollbars();
            HandleZoom();
            HandlePanning();
            DrawScreenspaceBackground();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();
            DoApplyRevertGUI();
            DoAlphaZoomToolbarGUI();
            EditorGUILayout.EndHorizontal();

            DoSelectedFrameInspector();
            UpdateEditorRendererPosition();
        }

        protected void HandleScrollbars()
        {
            Rect position = new Rect(textureWindowRect.xMin, textureWindowRect.yMax, textureWindowRect.width, 16f);
            viewScrollPosition.x = GUI.HorizontalScrollbar(position, viewScrollPosition.x, textureWindowRect.width, maxScrollRect.xMin, maxScrollRect.xMax);
            Rect position2 = new Rect(textureWindowRect.xMax, textureWindowRect.yMin, 16f, textureWindowRect.height);
            viewScrollPosition.y = GUI.VerticalScrollbar(position2, viewScrollPosition.y, textureWindowRect.height, maxScrollRect.yMin, maxScrollRect.yMax);
        }

        protected void SetupHandlesMatrix()
        {
            Vector3 pos = new Vector3(textureWindowRect.center.x - viewScrollPosition.x, textureWindowRect.center.y - viewScrollPosition.y - 16, 0f);
            Vector3 s = new Vector3(viewZoom, viewZoom, 1f);
            Handles.matrix = Matrix4x4.TRS(pos, Quaternion.identity, s);
        }

        protected void HandleZoom()
        {
            bool flag = Event.current.alt && Event.current.button == 1;
            if (flag)
            {
                EditorGUIUtility.AddCursorRect(textureWindowRect, MouseCursor.Zoom);
            }
            if (((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) && flag) || ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown) && Event.current.keyCode == KeyCode.LeftAlt))
            {
                base.Repaint();
            }
            if (Event.current.type == EventType.ScrollWheel || (Event.current.type == EventType.MouseDrag && Event.current.alt && Event.current.button == 1))
            {
                float num = 1f - Event.current.delta.y * ((Event.current.type != EventType.ScrollWheel) ? -0.005f : 0.03f);
                float num2 = viewZoom * num;
                float num3 = Mathf.Clamp(num2, this.GetMinZoom(), 10f);
                if (num3 != viewZoom)
                {
                    viewZoom = num3;
                    if (num2 != num3)
                    {
                        num /= num2 / num3;
                    }
                    viewScrollPosition *= num;
                    Event.current.Use();
                }
            }
        }

        protected void HandlePanning()
        {
            bool flag = (!Event.current.alt && Event.current.button > 0) || (Event.current.alt && Event.current.button <= 0);
            if (flag && GUIUtility.hotControl == 0)
            {
                EditorGUIUtility.AddCursorRect(textureWindowRect, MouseCursor.Pan);
                if (Event.current.type == EventType.MouseDrag)
                {
                    viewScrollPosition -= Event.current.delta;
                    Event.current.Use();
                }
            }
            if (((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) && flag) || ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown) && Event.current.keyCode == KeyCode.LeftAlt))
            {
                base.Repaint();
            }
        }
        
        protected void DoTextureGUIExtras()
        {
            HandleBorderCornerScalingHandles();
            HandleBorderSidePointScalingSliders();
            HandleBorderSideScalingHandles();
            HandlePivotHandle();

            if(Event.current.type == EventType.Repaint)
            {
                Vector4 border = svgAsset.border;
                
                Rect rect = new Rect(-textureOrigRect.size.x * 0.5f, -textureOrigRect.size.y * 0.5f, textureOrigRect.size.x, textureOrigRect.size.y);
                Rect drawRect = new Rect(
                    rect.x + rect.size.x * border.x,
                    rect.y + rect.size.y * border.w,
                    rect.size.x * Mathf.Abs(border.z - (1f - border.x)),
                    rect.size.y * Mathf.Abs(border.y - (1f - border.w))
                    );

                SVGEditorHandles.BeginLines(Color.green * 1.5f);
                SVGEditorHandles.DrawLine(
                    new Vector2(-position.width, drawRect.min.y),
                    new Vector2(position.width, drawRect.min.y)
                    );

                SVGEditorHandles.DrawLine(
                    new Vector2(-position.width, drawRect.max.y),
                    new Vector2(position.width, drawRect.max.y)
                    );

                SVGEditorHandles.DrawLine(
                    new Vector2(drawRect.min.x, -position.height),
                    new Vector2(drawRect.min.x, position.height)
                    );
                
                SVGEditorHandles.DrawLine(
                    new Vector2(drawRect.max.x, -position.height),
                    new Vector2(drawRect.max.x, position.height)
                    );

                //SVGEditorHandles.DrawBox(drawRect);
                SVGEditorHandles.EndLines();
            }
        }

        private void HandleBorderCornerScalingHandles()
        {
            if (svgAsset == null){return;}

            GUIStyle dragBorderdot = styles.dragBorderdot;
            GUIStyle dragBorderDotActive = styles.dragBorderDotActive;
            Color color = new Color(0f, 1f, 0f);
            Rect rect = new Rect(-textureOrigRect.size.x * 0.5f, -textureOrigRect.size.y * 0.5f, textureOrigRect.size.x, textureOrigRect.size.y);
            Vector4 border = svgAsset.border;
            float num = rect.xMin + border.x * textureOrigRect.size.x;
            float num2 = rect.xMax - border.z * textureOrigRect.size.x;
            float num3 = rect.yMax - border.y * textureOrigRect.size.y;
            float num4 = rect.yMin + border.w * textureOrigRect.size.y;

            EditorGUI.BeginChangeCheck();
            HandleBorderPointSlider(ref num, ref num3, MouseCursor.ResizeUpRight, border.x < 1f && border.w < 1f, dragBorderdot, dragBorderDotActive, color);
            HandleBorderPointSlider(ref num2, ref num3, MouseCursor.ResizeUpLeft, border.z < 1f && border.w < 1f, dragBorderdot, dragBorderDotActive, color);
            HandleBorderPointSlider(ref num, ref num4, MouseCursor.ResizeUpLeft, border.x < 1f && border.y < 1f, dragBorderdot, dragBorderDotActive, color);
            HandleBorderPointSlider(ref num2, ref num4, MouseCursor.ResizeUpRight, border.z < 1f && border.y < 1f, dragBorderdot, dragBorderDotActive, color);
            if (EditorGUI.EndChangeCheck())
            {
                border.x = (-rect.xMin + num) / textureOrigRect.size.x;
                border.z = (-num2 + rect.xMax) / textureOrigRect.size.x;
                border.y = (rect.yMax - num3) / textureOrigRect.size.y;
                border.w = (num4 - rect.yMin) / textureOrigRect.size.y;
                typeof(SVGAsset).GetField("_border", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(svgAsset, ClampSVGBorder(border));
                if(SVGAssetEditor.Instance != null)
                {
                    SVGAssetEditor.Instance.unappliedChanges = true;
                }
            }
        }
        private void HandleBorderSidePointScalingSliders()
        {
            if (svgAsset == null){return;}

            GUIStyle dragBorderdot = styles.dragBorderdot;
            GUIStyle dragBorderDotActive = styles.dragBorderDotActive;
            Color color = new Color(0f, 1f, 0f);
            Rect rect = new Rect(-textureOrigRect.size.x * 0.5f, -textureOrigRect.size.y * 0.5f, textureOrigRect.size.x, textureOrigRect.size.y);
            Vector4 border = svgAsset.border;
            float num = rect.xMin + border.x * textureOrigRect.size.x;
            float num2 = rect.xMax - border.z * textureOrigRect.size.x;
            float num3 = rect.yMax - border.y * textureOrigRect.size.y;
            float num4 = rect.yMin + border.w * textureOrigRect.size.y;

            EditorGUI.BeginChangeCheck();
            float num5 = num4 - (num4 - num3) / 2f;
            float num6 = num - (num - num2) / 2f;
            float num7 = num5;
            this.HandleBorderPointSlider(ref num, ref num7, MouseCursor.ResizeHorizontal, false, dragBorderdot, dragBorderDotActive, color);
            num7 = num5;
            this.HandleBorderPointSlider(ref num2, ref num7, MouseCursor.ResizeHorizontal, false, dragBorderdot, dragBorderDotActive, color);
            num7 = num6;
            this.HandleBorderPointSlider(ref num7, ref num3, MouseCursor.ResizeVertical, false, dragBorderdot, dragBorderDotActive, color);
            num7 = num6;
            this.HandleBorderPointSlider(ref num7, ref num4, MouseCursor.ResizeVertical, false, dragBorderdot, dragBorderDotActive, color);
            if (EditorGUI.EndChangeCheck())
            {
                border.x = (-rect.xMin + num) / textureOrigRect.size.x;
                border.z = (-num2 + rect.xMax) / textureOrigRect.size.x;
                border.y = (rect.yMax - num3) / textureOrigRect.size.y;
                border.w = (num4 - rect.yMin) / textureOrigRect.size.y;
                typeof(SVGAsset).GetField("_border", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(svgAsset, ClampSVGBorder(border));
                if(SVGAssetEditor.Instance != null)
                {
                    SVGAssetEditor.Instance.unappliedChanges = true;
                }
            }
        }

        private void HandleBorderSideScalingHandles()
        {
            if (svgAsset == null){return;}
            Rect rect = new Rect(-textureOrigRect.size.x * 0.5f, -textureOrigRect.size.y * 0.5f, textureOrigRect.size.x, textureOrigRect.size.y);
            Vector4 border = svgAsset.border;
            float num = rect.xMin + border.x * textureOrigRect.size.x;
            float num2 = rect.xMax - border.z * textureOrigRect.size.x;
            float num3 = rect.yMax - border.y * textureOrigRect.size.y;
            float num4 = rect.yMin + border.w * textureOrigRect.size.y;

            Vector2 vector = Handles.matrix.MultiplyPoint(new Vector3(rect.xMin, rect.yMin));
            Vector2 vector2 = Handles.matrix.MultiplyPoint(new Vector3(rect.xMax, rect.yMax));
            float width = Mathf.Abs(vector2.x - vector.x);
            float height = Mathf.Abs(vector2.y - vector.y);
            EditorGUI.BeginChangeCheck();

            num = this.HandleBorderScaleSlider(num, rect.yMin, width, height, true);
            num2 = this.HandleBorderScaleSlider(num2, rect.yMin, width, height, true);

            num3 = this.HandleBorderScaleSlider(rect.xMin, num3, width, height, false);
            num4 = this.HandleBorderScaleSlider(rect.xMin, num4, width, height, false);
            if (EditorGUI.EndChangeCheck())
            {
                border.x = (-rect.xMin + num) / textureOrigRect.size.x;
                border.z = (-num2 + rect.xMax) / textureOrigRect.size.x;
                border.y = (rect.yMax - num3) / textureOrigRect.size.y;
                border.w = (num4 - rect.yMin) / textureOrigRect.size.y;
                typeof(SVGAsset).GetField("_border", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(svgAsset, ClampSVGBorder(border));
                if(SVGAssetEditor.Instance != null)
                {
                    SVGAssetEditor.Instance.unappliedChanges = true;
                }
            }
        }

        private void HandlePivotHandle()
        {
            if (svgAsset == null){return;}
            EditorGUI.BeginChangeCheck();
            //selected.m_Pivot = this.ApplySpriteAlignmentToPivot(selected.m_Pivot, selected.m_Rect, selected.m_Alignment);
            Rect rect = new Rect(-textureOrigRect.size.x * 0.5f, -textureOrigRect.size.y * 0.5f, textureOrigRect.size.x, textureOrigRect.size.y);
            Vector2 pivotPoint = PivotSlider(rect, svgAsset.pivotPoint, styles.pivotdot, styles.pivotdotactive);
            if (EditorGUI.EndChangeCheck())
            {
                if(!svgAsset.customPivotPoint)
                {
                    pivotPoint.x = Mathf.Clamp01(Mathf.RoundToInt(pivotPoint.x * 2) / 2f);
                    pivotPoint.y = Mathf.Clamp01(Mathf.RoundToInt(pivotPoint.y * 2) / 2f);
                }

                typeof(SVGAsset).GetField("_pivotPoint", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(svgAsset, pivotPoint);
                if(SVGAssetEditor.Instance != null)
                {
                    SVGAssetEditor.Instance.unappliedChanges = true;
                }
            }
        }
        private void HandleBorderPointSlider(ref float x, ref float y, MouseCursor mouseCursor, bool isHidden, GUIStyle dragDot, GUIStyle dragDotActive, Color color)
        {
            Color color2 = GUI.color;
            GUI.color = color;

            Vector2 vector = SVGEditorHandles.PointSlider(new Vector2(x, y), mouseCursor, dragDot, dragDotActive);

            x = vector.x;
            y = vector.y;
            GUI.color = color2;
        }

        private float HandleBorderScaleSlider(float x, float y, float width, float height, bool isHorizontal)
        {
            if(styles == null)
                return 0f;

            float fixedWidth = styles.dragBorderdot.fixedWidth;
            Vector2 pos = Handles.matrix.MultiplyPoint(new Vector2(x, y));
            EditorGUI.BeginChangeCheck();
            float result = 0f;

            if (isHorizontal)
            {
                Rect cursorRect = new Rect(pos.x - fixedWidth * 0.5f, pos.y, fixedWidth, height);
                result = SVGEditorHandles.ScaleSlider(pos, MouseCursor.ResizeHorizontal, cursorRect).x;
            } else
            {
                Rect cursorRect2 = new Rect(pos.x, pos.y - fixedWidth * 0.5f, width, fixedWidth);
                result = SVGEditorHandles.ScaleSlider(pos, MouseCursor.ResizeVertical, cursorRect2).y;
            }

            if (EditorGUI.EndChangeCheck())
            {
                return result;
            }

            return (!isHorizontal) ? y : x;
        }

        private static Vector2 dragScreenOffset;        
//        private static int rectSelectionID = GUIUtility.GetPermanentControlID();        
        private static Vector2 currentMousePosition;
        private static Vector2 dragStartScreenPosition;

        Vector2 PivotSlider(Rect sprite, Vector2 pos, GUIStyle pivotDot, GUIStyle pivotDotActive)
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

                        currentMousePosition = current.mousePosition;
                        dragStartScreenPosition = current.mousePosition;
                        Vector2 b = Handles.matrix.MultiplyPoint(pos);
                        dragScreenOffset = currentMousePosition - b;
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
                        currentMousePosition += current.delta;
                        Vector2 a = pos;
                        Vector3 vector2 = Handles.inverseMatrix.MultiplyPoint(currentMousePosition - dragScreenOffset);
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
                        pos = Handles.inverseMatrix.MultiplyPoint(dragStartScreenPosition - dragScreenOffset);
                        GUIUtility.hotControl = 0;
                        GUI.changed = true;
                        current.Use();
                    }
                    break;
                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(position, MouseCursor.Arrow, controlID);
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
        protected void DrawScreenspaceBackground()
        {
            if (Event.current.type == EventType.Repaint)
            {
                //SpriteUtilityWindow.s_Styles.preBackground.Draw(textureWindowRect, false, false, false, false);
            }
        }

        protected float GetMinZoom()
        {
            return Mathf.Min(textureWindowRect.width / textureOrigRect.width, textureWindowRect.height / textureOrigRect.height * 0.9f);
        }

        protected Rect maxScrollRect
        {
            get
            {
                float horizontal = textureOrigRect.width * 0.5f * viewZoom;
                float vertical = textureOrigRect.height * 0.5f * viewZoom;
                return new Rect(-horizontal, -vertical, textureWindowRect.width + horizontal * 2f, textureWindowRect.height + vertical * 2f);
            }
        }

		private void DoApplyRevertGUI()
		{
			if (GUILayout.Button("Revert", EditorStyles.toolbarButton, new GUILayoutOption[0]))
			{
                SVGAssetEditor assetEditor = SVGAssetEditor.Instance;
                if(assetEditor != null)
                {
                    assetEditor.RevertChanges();
                }
                UpdateOriginalPivotPoint();
			}
			if (GUILayout.Button("Apply", EditorStyles.toolbarButton, new GUILayoutOption[0]))
            {
                SVGAssetEditor assetEditor = SVGAssetEditor.Instance;
                if(assetEditor != null)
                {
                    assetEditor.ApplyChanges();
                }
                UpdateOriginalPivotPoint();
            }
		}

        protected void DoAlphaZoomToolbarGUI()
        {
            viewAlpha = GUILayout.Toggle(viewAlpha, (!viewAlpha) ? styles.RGBIcon : styles.alphaIcon, "toolbarButton", new GUILayoutOption[0]);
            viewZoom = GUILayout.HorizontalSlider(viewZoom, GetMinZoom(), 10f, new GUILayoutOption[]{GUILayout.MaxWidth(64f)});
        }
        
        private void DoSelectedFrameInspector()
        {
            if (svgAsset != null)
            {
                EditorGUIUtility.wideMode = true;
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 135f;
                GUILayout.BeginArea(inspectorRect);
                GUILayout.BeginVertical(new GUIContent("SVG Asset"), GUI.skin.window, new GUILayoutOption[0]);
                Rect fieldLocation;
                DoBorderFields(out fieldLocation);
                DoPivotFields(fieldLocation);
                GUILayout.EndVertical();
                GUILayout.EndArea();
                EditorGUIUtility.labelWidth = labelWidth;
            }
        }

        private Rect inspectorRect
        {
            get
            {
                return new Rect(base.position.width - 330f - 8f - 16f, base.position.height - 110f - 8f - 16f, 330f, 110f);
            }
        }

        private void DoPivotFields(Rect fieldLocation)
        {
            EditorGUI.BeginChangeCheck();
            bool customPivotPoint = svgAsset.customPivotPoint;
            bool sliceMesh = svgAsset.sliceMesh;
            Vector2 pivotPoint = svgAsset.pivotPoint;

            fieldLocation.x = 5f;
            fieldLocation.y += 18f;
            fieldLocation.width = 414f;

            customPivotPoint = EditorGUI.Toggle(fieldLocation, new GUIContent("Custom Pivot", "Choose the predefined pivot point or the custom pivot point."), customPivotPoint);

            fieldLocation.y += 18f;

            sliceMesh = EditorGUI.Toggle(fieldLocation, new GUIContent("Slice Mesh", "For reducing scaling artefacts slice mesh."), sliceMesh);

            fieldLocation.y += 18f;

            if(customPivotPoint)
            { 
                EditorGUILayout.BeginHorizontal();                
                pivotPoint = EditorGUI.Vector2Field(fieldLocation, new GUIContent("Pivot", "The location of the SVG Asset center point in the original Rect, specified in percents."), pivotPoint);
                EditorGUILayout.EndHorizontal();
            } else {
                Vector2 pivotPointVector = pivotPoint;
                int selectedIndex = SVGAssetEditor.GetPivotPointIndex(pivotPointVector);
                selectedIndex = EditorGUI.Popup(fieldLocation, new GUIContent("Pivot", "The location of the SVG Asset center point in the original Rect, specified in percents."), selectedIndex, SVGAssetEditor.anchorPositionContent);
                pivotPoint = SVGAssetEditor.GetPivotPoint(selectedIndex);
            }
            if (EditorGUI.EndChangeCheck())
            {
                typeof(SVGAsset).GetField("_customPivotPoint", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(svgAsset, customPivotPoint);
                typeof(SVGAsset).GetField("_pivotPoint", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(svgAsset, pivotPoint);
                typeof(SVGAsset).GetField("_sliceMesh", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(svgAsset, sliceMesh);
                if(SVGAssetEditor.Instance != null)
                {
                    SVGAssetEditor.Instance.unappliedChanges = true;
                }
            }
        }

        private void DoBorderFields(out Rect fieldLocation)
        {
            EditorGUI.BeginChangeCheck();
            Rect rect = GUILayoutUtility.GetRect(322f, 322f, 32f, 32f);
            Vector4 vector = this.ClampSVGBorder(svgAsset.border);
            Vector4 border = vector;
            Rect position = rect;
            position.width = EditorGUIUtility.labelWidth;
            position.height = 16f;
            GUI.Label(position, "Border");
            fieldLocation = rect;
            fieldLocation.width -= EditorGUIUtility.labelWidth;
            fieldLocation.height = 16f;
            fieldLocation.x += EditorGUIUtility.labelWidth;
            fieldLocation.width /= 2f;
            fieldLocation.width -= 2f;
            EditorGUIUtility.labelWidth = 12f;
            border.x = (float)EditorGUI.FloatField(fieldLocation, "L", border.x);
            fieldLocation.x += fieldLocation.width + 3f;
            border.w = (float)EditorGUI.FloatField(fieldLocation, "T", border.w);
            fieldLocation.y += 16f;
            fieldLocation.x -= fieldLocation.width + 3f;
            border.z = (float)EditorGUI.FloatField(fieldLocation, "R", border.z);
            fieldLocation.x += fieldLocation.width + 3f;
            border.y = (float)EditorGUI.FloatField(fieldLocation, "B", border.y);
            EditorGUIUtility.labelWidth = 135f;
            if (EditorGUI.EndChangeCheck())
            {
                typeof(SVGAsset).GetField("_border", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(svgAsset, ClampSVGBorder(border));
                if(SVGAssetEditor.Instance != null)
                {
                    SVGAssetEditor.Instance.unappliedChanges = true;
                }
            }
        }

        private Vector4 ClampSVGBorder(Vector4 border)
        {
            return new Vector4(
                border.x = Mathf.Clamp(border.x, 0f, (1f-border.z)),
                border.y = Mathf.Clamp(border.y, 0f, (1f-border.w)),
                border.z = 1f - Mathf.Clamp(1f - border.z, border.x, 1f),
                border.w = 1f - Mathf.Clamp(1f - border.w, border.y, 1f)
                );
        }

        public static void InitPreview(Rect r, PreviewRenderUtility previewUtility)
        {
            typeof(PreviewRenderUtility).GetMethod("InitPreview", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(previewUtility, new object[]{ r });
		}

		protected static Mesh GetMesh(SVGAsset targetObject)
		{
			try {
				if(targetObject.useLayers)
				{
					if(_tempMesh == null) _tempMesh = new Mesh();
					Shader[] outputShaders;
					SVGMesh.CombineMeshes(targetObject.layers, _tempMesh, out outputShaders, targetObject.useGradients, targetObject.format, targetObject.compressDepth, targetObject.antialiasing);
					return _tempMesh;
				} else {
					FieldInfo _sharedMesh = typeof(SVGAsset).GetField("_sharedMesh", BindingFlags.NonPublic | BindingFlags.Instance);
					return _sharedMesh.GetValue(targetObject) as Mesh;
				}
            } catch {
                return null;
            }            
        }

        public static void DoRenderPreview(SVGAsset targetObject, PreviewRenderUtility previewUtility)
		{
			if(targetObject == null)
				return;

			bool hasGradients = targetObject.hasGradients;

			Mesh tempMesh = GetMesh(targetObject);
			if(tempMesh == null)
				return;
			Material[] sharedMaterials = targetObject.sharedMaterials;
			Material[] outputMaterials = sharedMaterials;
			if(tempMesh == null || sharedMaterials == null || sharedMaterials.Length == 0)
				return;

			Texture2D gradientAtlas = null;
			if(hasGradients)
			{
				gradientAtlas = SVGAtlas.GenerateGradientAtlasTexture(targetObject.sharedGradients, SVGAtlas.defaultGradientWidth, SVGAtlas.defaultGradientHeight);
				outputMaterials = new Material[sharedMaterials.Length];
				for(int i = 0; i < sharedMaterials.Length; i++)
				{
					if(sharedMaterials[i] == null) continue;
					outputMaterials[i] = Instantiate(sharedMaterials[i]) as Material;
					if(outputMaterials[i].HasProperty("_Params"))
					{
						outputMaterials[i].SetVector("_Params", new Vector4(gradientAtlas.width, gradientAtlas.height, SVGAtlas.defaultGradientWidth, SVGAtlas.defaultGradientHeight));
					}
					if(outputMaterials[i].HasProperty("_GradientColor"))
					{
						outputMaterials[i].SetTexture("_GradientColor", gradientAtlas);
					}
					if(outputMaterials[i].HasProperty("_GradientShape"))
					{
						outputMaterials[i].SetTexture("_GradientShape", SVGAtlas.gradientShapeTexture);
					}
				}
			}

			RenderMeshPreviewSkipCameraAndLighting(tempMesh, previewUtility, outputMaterials);

			if(hasGradients)
			{
				for(int i = 0; i < outputMaterials.Length; i++)
				{
					if(outputMaterials[i] == null) continue;
					DestroyImmediate(outputMaterials[i]);
				}
			}

			if(gradientAtlas != null)
			{
				DestroyImmediate(gradientAtlas);
			}
		}

		internal static void RenderMeshPreviewSkipCameraAndLighting(Mesh mesh, PreviewRenderUtility previewUtility, Material[] materials)
		{
			if (mesh == null || previewUtility == null)
			{
				return;
			}
			
			Bounds bounds = mesh.bounds;
			float magnitude = bounds.extents.magnitude;
			float num = 4f * magnitude;
			previewUtility.camera.transform.position = -Vector3.forward * num;
			previewUtility.camera.transform.rotation = Quaternion.identity;
			previewUtility.camera.nearClipPlane = num - magnitude * 1.1f;
			previewUtility.camera.farClipPlane = num + magnitude * 1.1f;
			
			Quaternion quaternion = Quaternion.identity;
			Vector3 pos = -bounds.center;
			bool fog = RenderSettings.fog;
			Unsupported.SetRenderSettingsUseFogNoDirty(false);
			int subMeshCount = mesh.subMeshCount;
			int meshSubset = materials.Length;
			if (materials != null && materials.Length > 0)
			{
				previewUtility.camera.clearFlags = CameraClearFlags.Nothing;
				if (meshSubset < 0 || meshSubset >= subMeshCount)
				{
					for (int i = 0; i < subMeshCount; i++)
					{
						previewUtility.DrawMesh(mesh, pos, quaternion, materials[i], i);
					}
				}
				else
				{
					previewUtility.DrawMesh(mesh, pos, quaternion, materials[0], -1);
				}
				previewUtility.camera.Render();
			}
			Unsupported.SetRenderSettingsUseFogNoDirty(fog);
		}
	}
}
