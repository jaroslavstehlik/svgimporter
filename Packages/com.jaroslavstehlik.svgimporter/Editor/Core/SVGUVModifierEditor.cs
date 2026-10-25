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
    }

    void OnSceneGUI()
    {
    }
}
