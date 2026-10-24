using UnityEngine;
using UnityEditor;
using System.Collections;

using SVGImporter;
using SVGImporter.Rendering;

[CustomEditor(typeof(SVGUVModifier))]
public class SVGUVModifierEditor : Editor {

    SVGUVModifier uvModifier;
    //    bool rendererFoldout = false;

    const string SVGUVModifierKey = "SVG_IMPORTER_SVG_UV_MODIFIER_KEY";
    protected bool _editMode
    {
        get {
            if(EditorPrefs.HasKey(SVGUVModifierKey))
            {
                return EditorPrefs.GetBool(SVGUVModifierKey);
            } else {
                return false;
            }
        }
        set {
            EditorPrefs.SetBool(SVGUVModifierKey, value);
        }
    }

    void OnEnable()
    {
        uvModifier = (SVGUVModifier)target;
    }
    
    public override void OnInspectorGUI()
    {
        _editMode = EditorGUILayout.Toggle(new GUIContent("Edit"), _editMode);
        if(uvModifier == null)
        {
            uvModifier = (SVGUVModifier)target;
            if(uvModifier == null) return;
        }

        base.OnInspectorGUI();
		/*
        if(GUILayout.Button("Center"))
        {
            Undo.RecordObject(uvModifier, "Center UV Transform");
            if(uvModifier.worldSpace)
            {
                uvModifier.svgTransform.position = uvModifier.transform.position;
            } else {
                uvModifier.svgTransform.position = Vector2.zero;
            }
            SceneView.RepaintAll();
            Repaint();
        }
        */
    }

    void OnSceneGUI()
    {
		/*
        if(uvModifier == null)
        {
            uvModifier = (SVGUVModifier)target;
            if(uvModifier == null) return;
        }

        if(!uvModifier.enabled) return;
        if(uvModifier.svgTransform == null) return;

        var e = Event.current;
        if (e.type == EventType.ValidateCommand && e.commandName == "FrameSelected")
        {
            e.Use();
        }

        SVGTransform2D trs = new SVGTransform2D(uvModifier.svgTransform);

        if(_editMode)
        {
            if(!uvModifier.worldSpace)
            {
                trs = SVGTransform2D.DecomposeMatrix(uvModifier.transform.localToWorldMatrix * uvModifier.svgTransform.matrix);
            }
            SVGHandles.TransformHandle(trs);        
            if(!uvModifier.worldSpace)
            {
                trs = SVGTransform2D.DecomposeMatrix(uvModifier.transform.worldToLocalMatrix * uvModifier.svgTransform.matrix);
            }
        }

        if(!trs.Compare(uvModifier.svgTransform))
        {
            Undo.RecordObject(target, "SVG UV Modify");
            uvModifier.svgTransform.SetTransform(trs);
            EditorUtility.SetDirty(target);
            GUI.changed = true;
        }
        
        if (Event.current.type == EventType.ExecuteCommand)
        {            
            if (Event.current.commandName == "UndoRedoPerformed")
            {
                Repaint();
                SceneView.RepaintAll();
            }
        }
        */
    }
}
