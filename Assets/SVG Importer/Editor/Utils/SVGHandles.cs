// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace SVGImporter.Rendering
{
    using Utils;

    public class SVGHandles : System.Object
    {
        const float PI_2 = Mathf.PI * 2f;
        const float PI = Mathf.PI;
        const float PI_05 = Mathf.PI * 0.5f;
        const float PI_025 = Mathf.PI * 0.25f;
    //    static Color COLOR_SEMI_SELECTED = new Color(1f, 0.5f, 0f, .3f);
        static Color COLOR_SELECTED = new Color(1f, 0.5f, 0f, 1f);
        static Color COLOR_HIGHLIGHTED = new Color(1f, 1f, 1f, 1f);
        static float handleSize;
        static float transformCapSize;
        static float transformCursorRadius = 15f;
        static float transformRotationHandleSize = 15f * 1.1f;
        static float transformRotationOffset;
        static Vector2 _startExtents;
        static Vector2 _startScale;
        static Vector2 _anchor;
        static SVGTransform2D origVrTransform;
        static int masterControlID;
        static Matrix4x4 lastLocalToWorld;

        static public Vector2 anchor
        {
            get
            {
                return _anchor;
            }
        }

        static bool _move;

        static public bool move
        {
            get
            {
                return _move;
            }
        }

        // p0
        static bool _p0_scale;

        static public bool p0_scale
        {
            get
            {
                return _p0_scale;
            }
        }

        static bool _p0_rotate;

        static public bool p0_rotate
        {
            get
            {
                return _p0_rotate;
            }
        }    
        // p1
        static bool _p1_scale;

        static public bool p1_scale
        {
            get
            {
                return _p1_scale;
            }
        }

        static bool _p1_rotate;

        static public bool p1_rotate
        {
            get
            {
                return _p1_rotate;
            }
        }
        // p2
        static bool _p2_scale;

        static public bool p2_scale
        {
            get
            {
                return _p2_scale;
            }
        }

        static bool _p2_rotate;

        static public bool p2_rotate
        {
            get
            {
                return _p2_rotate;
            }
        }    
        // p3
        static bool _p3_scale;

        static public bool p3_scale
        {
            get
            {
                return _p3_scale;
            }
        }

        static bool _p3_rotate;

        static public bool p3_rotate
        {
            get
            {
                return _p3_rotate;
            }
        }    
        // p0-p1
        static bool _p0p1_scale;

        static public bool p0p1_scale
        {
            get
            {
                return _p0p1_scale;
            }
        }

        static bool _p0p1_rotate;

        static public bool p0p1_rotate
        {
            get
            {
                return _p0p1_rotate;
            }
        }
        // p2-p0
        static bool _p2p0_scale;

        static public bool p2p0_scale
        {
            get
            {
                return _p2p0_scale;
            }
        }

        static bool _p2p0_rotate;

        static public bool p2p0_rotate
        {
            get
            {
                return _p2p0_rotate;
            }
        }
        // p3-p2
        static bool _p3p2_scale;

        static public bool p3p2_scale
        {
            get
            {
                return _p3p2_scale;
            }
        }

        static bool _p3p2_rotate;

        static public bool p3p2_rotate
        {
            get
            {
                return _p3p2_rotate;
            }
        }    
        // p1-p3
        static bool _p1p3_scale;

        static public bool p1p3_scale
        {
            get
            {
                return _p1p3_scale;
            }
        }

        static bool _p1p3_rotate;

        static public bool p1p3_rotate
        {
            get
            {
                return _p1p3_rotate;
            }
        }

    //    static Matrix4x4 worldToLocalMatrix;
        static Vector3 worldTransformPosition;
    //    static Quaternion worldTransformRotation;

        static public float gizmoRotation;
        static bool _editorHoldingTransform;
        static bool _scaling;
        static bool _rotating;
        static Vector2 mouseStartWorldPosition;
        static Vector2 mouseStartLocalPosition;
        static Vector2 p0, p1, p2, p3, p4, center, p0p1, p1p3, p3p2, p2p0;

        public static void TransformHandle(SVGTransform2D vrTrs)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !SceneView.lastActiveSceneView.in2DMode)
                return;

            masterControlID = GUIUtility.GetControlID(FocusType.Passive);

            handleSize = HandleUtility.GetHandleSize(worldTransformPosition);
            transformCapSize = handleSize * 0.04f;
            transformRotationOffset = handleSize * 0.2f;

            Event current = Event.current;
            EventType type = current.GetTypeForControl(masterControlID);

            if (type == EventType.MouseDown && current.button == 0)
            {
                if (origVrTransform == null)
                {
                    origVrTransform = new SVGTransform2D(vrTrs);
                } else {
                    origVrTransform.SetTransform(vrTrs);
                }

                OnMouseDown(vrTrs);
                if (_editorHoldingTransform)
                {
                    GUIUtility.hotControl = masterControlID;
                    current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);
                }
            }

            if (_editorHoldingTransform)
            {
                if (type == EventType.MouseUp || Event.current.rawType == EventType.MouseUp)
                {
                    if (GUIUtility.hotControl == masterControlID)
                    {
                        Reset();
                        GUIUtility.hotControl = 0;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                }

                if (type == EventType.MouseDrag)
                {
                    OnMouseDrag(vrTrs);
                    GUI.changed = true;
                    SceneView.RepaintAll();
                }

                // Cancel Operation
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    vrTrs.SetTransform(origVrTransform);
                    Reset();
                    GUIUtility.hotControl = 0;
                    current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(0);
                }
            }

            if (type == EventType.Repaint)
            {
                RenderTransformationGizmos(vrTrs);
            }
        }

        protected static void Reset()
        {
            gizmoRotation = 0f;
            _editorHoldingTransform = false;
            _scaling = false;
            _rotating = false;
            _p0_scale = _p0_rotate = _p1_scale = _p1_rotate = _p2_scale = _p2_rotate = _p3_scale = _p3_rotate = _p0p1_scale = _p0p1_rotate = _p2p0_scale = _p2p0_rotate = _p3p2_scale = _p3p2_rotate = _p1p3_scale = _p1p3_rotate = false;
        }

        protected static void OnMouseDown(SVGTransform2D selectionTransform)
        {
            Matrix4x4 selectionTransformMatrix = Matrix4x4.TRS(selectionTransform.position, Quaternion.Euler(0f, 0f, selectionTransform.rotation), new Vector3(selectionTransform.scale.x, selectionTransform.scale.y, 1f));
            Camera editorCamera = Camera.current;

            p0 = selectionTransformMatrix.MultiplyPoint(new Vector2(-1f, -1f));
            p1 = selectionTransformMatrix.MultiplyPoint(new Vector2(1f, -1f));
            p2 = selectionTransformMatrix.MultiplyPoint(new Vector2(-1f, 1f));
            p3 = selectionTransformMatrix.MultiplyPoint(new Vector2(1f, 1f));

            center = Vector2.Lerp(p0, p3, 0.5f);

            p0p1 = Vector2.Lerp(p0, p1, 0.5f);
            p1p3 = Vector2.Lerp(p1, p3, 0.5f);
            p3p2 = Vector2.Lerp(p3, p2, 0.5f);
            p2p0 = Vector2.Lerp(p2, p0, 0.5f);
            
            Vector2 diagonalA = (p0 - p3).normalized;
            Vector2 diagonalB = (p1 - p2).normalized;
            Vector2 horizontal = (p2p0 - p1p3).normalized;
            Vector2 vertical = (p3p2 - p0p1).normalized;

            //Vector3 localMousePosition = transform.worldToLocalMatrix.MultiplyPoint(Camera.current.ScreenToWorldPoint(new Vector3(Event.current.mousePosition.x, Event.current.mousePosition.y, Mathf.Abs(transform.position.z - Camera.current.transform.position.z))));

            _startScale.x = Vector2.Distance(p2p0, p1p3);
            _startScale.y = Vector2.Distance(p0p1, p3p2);
            
            // p0
            _p0_scale = SVGGizmos.MouseTestScreenRect(p0, transformCursorRadius);
            _p0_rotate = SVGGizmos.MouseTestScreenRect(p0 + diagonalA * transformRotationOffset, transformRotationHandleSize);
            
            // p1
            _p1_scale = SVGGizmos.MouseTestScreenRect(p1, transformCursorRadius);
            _p1_rotate = SVGGizmos.MouseTestScreenRect(p1 + diagonalB * transformRotationOffset, transformRotationHandleSize);
            
            // p2
            _p2_scale = SVGGizmos.MouseTestScreenRect(p2, transformCursorRadius);
            _p2_rotate = SVGGizmos.MouseTestScreenRect(p2 - diagonalB * transformRotationOffset, transformRotationHandleSize);
            
            // p3
            _p3_scale = SVGGizmos.MouseTestScreenRect(p3, transformCursorRadius);
            _p3_rotate = SVGGizmos.MouseTestScreenRect(p3 - diagonalA * transformRotationOffset, transformRotationHandleSize);
            
            // p0-p1
            _p0p1_scale = SVGGizmos.MouseTestScreenRect(p0p1, transformCursorRadius);
            _p0p1_rotate = SVGGizmos.MouseTestScreenRect(p0p1 - vertical * transformRotationOffset, transformRotationHandleSize);
            
            // p2-p0
            _p2p0_scale = SVGGizmos.MouseTestScreenRect(p2p0, transformCursorRadius);
            _p2p0_rotate = SVGGizmos.MouseTestScreenRect(p2p0 + horizontal * transformRotationOffset, transformRotationHandleSize);
            
            // p3-p2
            _p3p2_scale = SVGGizmos.MouseTestScreenRect(p3p2, transformCursorRadius);
            _p3p2_rotate = SVGGizmos.MouseTestScreenRect(p3p2 + vertical * transformRotationOffset, transformRotationHandleSize);
            
            // p1-p3
            _p1p3_scale = SVGGizmos.MouseTestScreenRect(p1p3, transformCursorRadius);
            _p1p3_rotate = SVGGizmos.MouseTestScreenRect(p1p3 - horizontal * transformRotationOffset, transformRotationHandleSize);

            _editorHoldingTransform = _p0_scale || _p0_rotate || _p1_scale || _p1_rotate || _p2_scale || _p2_rotate || _p3_scale || _p3_rotate || 
                _p0p1_scale || _p0p1_rotate || _p2p0_scale || _p2p0_rotate || _p3p2_scale || _p3p2_rotate || _p1p3_scale || _p1p3_rotate;

            _scaling = _p0_scale || _p1_scale || _p2_scale || _p3_scale || _p0p1_scale || _p2p0_scale || _p3p2_scale || _p1p3_scale;
            _rotating = _p0_rotate || _p1_rotate || _p2_rotate || _p3_rotate || _p0p1_rotate || _p2p0_rotate || _p3p2_rotate || _p1p3_rotate;

            float distanceFromCamera = Mathf.Abs(editorCamera.transform.position.z - worldTransformPosition.z);
            mouseStartWorldPosition = editorCamera.ScreenToWorldPoint(new Vector3(Event.current.mousePosition.x, editorCamera.pixelHeight - Event.current.mousePosition.y, distanceFromCamera));
            mouseStartLocalPosition = selectionTransformMatrix.inverse.MultiplyPoint(mouseStartWorldPosition);
            lastLocalToWorld = selectionTransformMatrix;

            if (!_editorHoldingTransform)
            {
                Vector2 mouseTest = selectionTransformMatrix.inverse.MultiplyPoint(mouseStartWorldPosition);
                if (Mathf.Abs(mouseTest.x) <= 1f && Mathf.Abs(mouseTest.y) <= 1f)
                {
                    _editorHoldingTransform = true;
                    _move = true;
                }
            }


            UpdateAnchor();
        }

        protected static void OnMouseDrag(SVGTransform2D selectionTransform)
        {
    //        Matrix4x4 localMatrixInverse = localMatrix.inverse;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, origVrTransform.rotation), Vector3.one).inverse;
            Matrix4x4 selectionTransformMatrix = Matrix4x4.TRS(selectionTransform.position, Quaternion.Euler(0f, 0f, selectionTransform.rotation), new Vector3(selectionTransform.scale.x, selectionTransform.scale.y, 1f));
    //        Matrix4x4 selectionTransformMatrixInverse = selectionTransformMatrix.inverse;

            Camera editorCamera = Camera.current;

            float distanceFromCamera = Mathf.Abs(editorCamera.transform.position.z - worldTransformPosition.z);
            Vector2 worldCenter = selectionTransformMatrix.MultiplyPoint(Vector2.zero);

            Vector2 mouseWorldPosition = editorCamera.ScreenToWorldPoint(new Vector3(Event.current.mousePosition.x, editorCamera.pixelHeight - Event.current.mousePosition.y, distanceFromCamera));
            Vector2 mouseLocalPosition = lastLocalToWorld.inverse.MultiplyPoint(mouseWorldPosition);

            Vector2 minDistance = new Vector2(mouseStartLocalPosition.x, mouseStartLocalPosition.y);
            Vector2 mouseLocalDifference = new Vector2(mouseLocalPosition.x - mouseStartLocalPosition.x, mouseLocalPosition.y - mouseStartLocalPosition.y);
            Vector2 mouseWorldDifference = new Vector2(mouseWorldPosition.x - mouseStartWorldPosition.x, mouseWorldPosition.y - mouseStartWorldPosition.y);

            Vector2 startDir = (mouseStartWorldPosition - worldCenter).normalized;
            Vector2 finDir = (mouseWorldPosition - worldCenter).normalized;
            float finalRotation = Mathf.Atan2(finDir.y, finDir.x) - Mathf.Atan2(startDir.y, startDir.x);

            UpdateAnchor();

            if (Event.current.shift)
            {
                if (finalRotation != 0f)
                    finalRotation = Mathf.Round(finalRotation / PI_025) * PI_025;
            }

            if (_scaling && !_rotating)
            {
                Vector2 destinationScale = Vector2.zero;
                if (_p0_scale || _p1_scale || _p2_scale || _p3_scale)
                {
                    if (minDistance.x * minDistance.y != 0f)
                    {
                        destinationScale.x = mouseLocalDifference.x / minDistance.x;
                        destinationScale.y = mouseLocalDifference.y / minDistance.y;
                        if (Event.current.shift)
                            destinationScale.y = destinationScale.x;
                    }
                } else if (_p2p0_scale || _p1p3_scale)
                {                   
                    if (minDistance.x != 0f)
                    {
                        destinationScale.x = mouseLocalDifference.x / minDistance.x;
                        if (Event.current.shift)
                            destinationScale.y = destinationScale.x;
                    }
                } else if (_p0p1_scale || _p3p2_scale)
                {
                    if (minDistance.y != 0f)
                    {
                        destinationScale.y = mouseLocalDifference.y / minDistance.y;
                        if (Event.current.shift)
                            destinationScale.x = destinationScale.y;
                    }
                }

                if (!Event.current.alt)
                {
                    Vector2 tempAnchor = rotationMatrix.MultiplyPoint(anchor);
                    tempAnchor = Vector2.Scale(tempAnchor, destinationScale * 0.5f);
                    tempAnchor = rotationMatrix.inverse.MultiplyPoint(tempAnchor);
                    selectionTransform.position = origVrTransform.position - tempAnchor;
                    selectionTransform.scale = Vector2.Scale(origVrTransform.scale, Vector2.one + destinationScale * 0.5f);
                } else
                {            
                    selectionTransform.position = origVrTransform.position;
                    selectionTransform.scale = Vector2.Scale(origVrTransform.scale, Vector2.one + destinationScale);
                }
            }

            if (!_scaling && _rotating)
            {
                if (_p0_rotate || _p1_rotate || _p2_rotate || _p3_rotate || _p0p1_rotate || _p2p0_rotate || _p3p2_rotate || _p1p3_rotate)
                {
                    selectionTransform.rotation = origVrTransform.rotation + finalRotation * Mathf.Rad2Deg;
                }
            }
            if (!_scaling && !_rotating && _move)
            {
                Vector2 localPositionDifference = mouseWorldDifference;

                if (Event.current.shift)
                {
                    float tempAngle = Mathf.Atan2(localPositionDifference.y, localPositionDifference.x);
                    if(tempAngle != 0f)
                        tempAngle = Mathf.Round(tempAngle / PI_025) * PI_025;
                    
                    float tempDistance = localPositionDifference.magnitude;
                    localPositionDifference.x = Mathf.Cos(tempAngle) * tempDistance;
                    localPositionDifference.y = Mathf.Sin(tempAngle) * tempDistance;
                }

                selectionTransform.position = origVrTransform.position + localPositionDifference;
            }
        }

        protected static void UpdateAnchor()
        {        
            _anchor = Vector2.zero;
            
            if (_scaling)
            {
                if (p0_scale)
                {
                    _anchor = p3 - center;
                } else if (p1_scale)
                {
                    _anchor = p2 - center;
                } else if (p2_scale)
                {
                    _anchor = p1 - center;
                } else if (p3_scale)
                {
                    _anchor = p0 - center;
                } else if (_p0p1_scale)
                {
                    _anchor = p3p2 - center;
                } else if (_p1p3_scale)
                {
                    _anchor = p2p0 - center;
                } else if (_p2p0_scale)
                {
                    _anchor = p1p3 - center;
                } else if (_p3p2_scale)
                {
                    _anchor = p0p1 - center;
                }
            }
        }

        protected static void RenderTransformationGizmos(SVGTransform2D selectionTransform)
        {
            Handles.color = COLOR_SELECTED;
            Quaternion capRotation = Quaternion.identity;
    //        Vector2 transformAnchor = Vector2.zero;
                    
            Quaternion rotation = Quaternion.Euler(0f, 0f, selectionTransform.rotation + gizmoRotation);
//            Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
            /*
            Vector2 offset = Vector2.zero;

            if (_scaling && !Event.current.alt)
            {
                if (p0_scale)
                {
                    offset = rotationMatrix.MultiplyVector(Vector2.Scale(_startExtents, Vector2.one - selectionTransform.scale));
                } else if (p1_scale)
                {
                    offset = rotationMatrix.MultiplyVector(Vector2.Scale(new Vector2(-_startExtents.x, _startExtents.y), Vector2.one - selectionTransform.scale));
                } else if (p2_scale)
                {
                    offset = rotationMatrix.MultiplyVector(Vector2.Scale(new Vector2(_startExtents.x, -_startExtents.y), Vector2.one - selectionTransform.scale));
                } else if (p3_scale)
                {
                    offset = rotationMatrix.MultiplyVector(Vector2.Scale(-_startExtents, Vector2.one - selectionTransform.scale));
                } else  if (!Event.current.shift && selectionTransform.scale.x != selectionTransform.scale.y)
                { 
                    if (_p0p1_scale || _p2p0_scale)
                    {
                        offset = rotationMatrix.MultiplyVector(Vector2.Scale(_startExtents, Vector2.one - selectionTransform.scale));
                    } else if (_p1p3_scale || _p3p2_scale)
                    {
                        offset = rotationMatrix.MultiplyVector(Vector2.Scale(-_startExtents, Vector2.one - selectionTransform.scale));
                    } 
                } else
                {
                    if (_p0p1_scale)
                    {
                        offset = rotationMatrix.MultiplyVector(Vector2.Scale(new Vector2(0f, _startExtents.y), Vector2.one - selectionTransform.scale));
                    } else if (_p1p3_scale)
                    {
                        offset = rotationMatrix.MultiplyVector(Vector2.Scale(new Vector2(-_startExtents.x, 0f), Vector2.one - selectionTransform.scale));
                    } else if (_p2p0_scale)
                    {
                        offset = rotationMatrix.MultiplyVector(Vector2.Scale(new Vector2(_startExtents.x, 0f), Vector2.one - selectionTransform.scale));
                    } else if (_p3p2_scale)
                    {
                        offset = rotationMatrix.MultiplyVector(Vector2.Scale(new Vector2(0f, -_startExtents.y), Vector2.one - selectionTransform.scale));
                    }
                }
            }
            */
                   
//            Debug.Log(offset);
            Matrix4x4 transMatrix = Matrix4x4.TRS(selectionTransform.position, rotation, selectionTransform.scale);
            Matrix4x4 selectionTransformMatrix = transMatrix;

            Vector2 p0 = selectionTransformMatrix.MultiplyPoint(new Vector2(-1f, -1f));
            Vector2 p1 = selectionTransformMatrix.MultiplyPoint(new Vector2(1f, -1f));
            Vector2 p2 = selectionTransformMatrix.MultiplyPoint(new Vector2(-1f, 1f));
            Vector2 p3 = selectionTransformMatrix.MultiplyPoint(new Vector2(1f, 1f));

            // Center
            //Handles.color = Color.blue;
            //Handles.CircleCap(0, new Vector3(selectedBounds.center.x, selectedBounds.center.y, 0f), Quaternion.identity, handleSize * 0.1f);

            Vector2 p0p1 = Vector2.Lerp(p0, p1, 0.5f);
            Vector2 p1p3 = Vector2.Lerp(p1, p3, 0.5f);
            Vector2 p3p2 = Vector2.Lerp(p3, p2, 0.5f);
            Vector2 p2p0 = Vector2.Lerp(p2, p0, 0.5f);
            
            Vector2 diagonalA = (p0 - p3).normalized;
            Vector2 diagonalB = (p1 - p2).normalized;
            Vector2 horizontal = (p2p0 - p1p3).normalized;
            Vector2 vertical = (p3p2 - p0p1).normalized;

            Handles.DotHandleCap(0, Vector2.Lerp(p2p0, p1p3, 0.5f), capRotation, transformCapSize, EventType.Repaint);
            
            // p0
            SVGGizmos.ShowScaleCursor(p0, p3 - p0, transformCursorRadius);
            SVGGizmos.ShowCursor(p0 + diagonalA * transformRotationOffset, transformRotationHandleSize, MouseCursor.RotateArrow);
            if (_p0_scale || _p0_rotate)
                Handles.color = COLOR_HIGHLIGHTED;
            else
                Handles.color = COLOR_SELECTED;
            Handles.DotHandleCap(0, p0, capRotation, transformCapSize, EventType.Repaint);

            // p1
            SVGGizmos.ShowScaleCursor(p1, p2 - p1, transformCursorRadius);
            SVGGizmos.ShowCursor(p1 + diagonalB * transformRotationOffset, transformRotationHandleSize, MouseCursor.RotateArrow);
            if (_p1_scale || _p1_rotate)
                Handles.color = COLOR_HIGHLIGHTED;
            else
                Handles.color = COLOR_SELECTED;
            Handles.DotHandleCap(0, p1, capRotation, transformCapSize, EventType.Repaint);
            
            // p2
            SVGGizmos.ShowScaleCursor(p2, p1 - p2, transformCursorRadius);
            SVGGizmos.ShowCursor(p2 - diagonalB * transformRotationOffset, transformRotationHandleSize, MouseCursor.RotateArrow);
            if (_p2_scale || _p2_rotate)
                Handles.color = COLOR_HIGHLIGHTED;
            else
                Handles.color = COLOR_SELECTED;
            Handles.DotHandleCap(0, p2, capRotation, transformCapSize, EventType.Repaint);
            
            // p3
            SVGGizmos.ShowScaleCursor(p3, p0 - p3, transformCursorRadius);
            SVGGizmos.ShowCursor(p3 - diagonalA * transformRotationOffset, transformRotationHandleSize, MouseCursor.RotateArrow);
            if (_p3_scale || _p3_rotate)
                Handles.color = COLOR_HIGHLIGHTED;
            else
                Handles.color = COLOR_SELECTED;
            Handles.DotHandleCap(0, p3, capRotation, transformCapSize, EventType.Repaint);
            
            // p0-p1
            SVGGizmos.ShowScaleCursor(p0p1, p3p2 - p0p1, transformCursorRadius);
            SVGGizmos.ShowCursor(p0p1 - vertical * transformRotationOffset, transformRotationHandleSize, MouseCursor.RotateArrow);
            if (_p0p1_scale || _p0p1_scale)
                Handles.color = COLOR_HIGHLIGHTED;
            else
                Handles.color = COLOR_SELECTED;
            Handles.DotHandleCap(0, p0p1, capRotation, transformCapSize, EventType.Repaint);
            
            // p2-p0
            SVGGizmos.ShowScaleCursor(p2p0, p1p3 - p2p0, transformCursorRadius);
            SVGGizmos.ShowCursor(p2p0 + horizontal * transformRotationOffset, transformRotationHandleSize, MouseCursor.RotateArrow);
            if (_p2p0_scale || _p2p0_scale)
                Handles.color = COLOR_HIGHLIGHTED;
            else
                Handles.color = COLOR_SELECTED;
            Handles.DotHandleCap(0, p2p0, capRotation, transformCapSize, EventType.Repaint);
            
            // p3-p2
            SVGGizmos.ShowScaleCursor(p3p2, p0p1 - p3p2, transformCursorRadius);
            SVGGizmos.ShowCursor(p3p2 + vertical * transformRotationOffset, transformRotationHandleSize, MouseCursor.RotateArrow);
            if (_p3p2_scale || _p3p2_rotate)
                Handles.color = COLOR_HIGHLIGHTED;
            else
                Handles.color = COLOR_SELECTED;
            Handles.DotHandleCap(0, p3p2, capRotation, transformCapSize, EventType.Repaint);
            
            // p1-p3
            SVGGizmos.ShowScaleCursor(p1p3, p2p0 - p1p3, transformCursorRadius);
            SVGGizmos.ShowCursor(p1p3 - horizontal * transformRotationOffset, transformRotationHandleSize, MouseCursor.RotateArrow);
            if (p1p3_scale || p1p3_scale)
                Handles.color = COLOR_HIGHLIGHTED;
            else
                Handles.color = COLOR_SELECTED;
            Handles.DotHandleCap(0, p1p3, capRotation, transformCapSize, EventType.Repaint);
            
            Handles.color = COLOR_SELECTED;
            SVGGizmos.Line(new Vector2[]{p0, p1, p3, p2, p0});
            /*
            Handles.Label(p0, "   p0");
            Handles.Label(p1, "   p1");
            Handles.Label(p2, "   p2");
            Handles.Label(p3, "   p3");
            */
        }
    }
}
