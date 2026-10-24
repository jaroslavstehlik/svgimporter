using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(SVGDebugMesh))]
public class SVGDebugMeshEditor : Editor
{
    public enum DebugType
    {
        NONE,
        INDEXES,
        VERTICES,
        COLORS,
        UV,
        UV2,
        UV3,
        UV4,
        NORMALS,
        TANGENTS
    }

    const string SVG_IMPORTER_SVGDEBUGMESH_KEY = "SVG_IMPORTER_SVGDEBUGMESH_KEY";
    public DebugType debugType
    {
        get {
            if(EditorPrefs.HasKey(SVG_IMPORTER_SVGDEBUGMESH_KEY))
                return (DebugType)EditorPrefs.GetInt(SVG_IMPORTER_SVGDEBUGMESH_KEY);

            return DebugType.NONE;
        }
        set {
            EditorPrefs.SetInt(SVG_IMPORTER_SVGDEBUGMESH_KEY, (int)value);
        }
    }

	const string SVG_IMPORTER_SHOWLABELS_KEY = "SVG_IMPORTER_SHOWLABELS_KEY";
	public bool showLabels
	{
		get {
			if(EditorPrefs.HasKey(SVG_IMPORTER_SHOWLABELS_KEY))
				return EditorPrefs.GetBool(SVG_IMPORTER_SHOWLABELS_KEY);
			
			return false;
		}
		set {
			EditorPrefs.SetBool(SVG_IMPORTER_SHOWLABELS_KEY, value);
		}
	}

    const string SVG_IMPORTER_SHOWPOINTS_KEY = "SVG_IMPORTER_SHOWPOINTS_KEY";
    public bool showPoints
    {
        get {
            if(EditorPrefs.HasKey(SVG_IMPORTER_SHOWPOINTS_KEY))
                return EditorPrefs.GetBool(SVG_IMPORTER_SHOWPOINTS_KEY);
            
            return false;
        }
        set {
            EditorPrefs.SetBool(SVG_IMPORTER_SHOWPOINTS_KEY, value);
        }
    }

	const string SVG_IMPORTER_SHOWNORMALS_KEY = "SVG_IMPORTER_SHOWNORMALS_KEY";
	public bool showNormals
	{
		get {
			if(EditorPrefs.HasKey(SVG_IMPORTER_SHOWNORMALS_KEY))
				return EditorPrefs.GetBool(SVG_IMPORTER_SHOWNORMALS_KEY);
			
			return false;
		}
		set {
			EditorPrefs.SetBool(SVG_IMPORTER_SHOWNORMALS_KEY, value);
		}
	}

    static int startIndex = -1;
    static int endIndex = -1;

    private void OnEnable()
    {

        startIndex = -1;
        endIndex = -1;
    }

    public override void OnInspectorGUI()
    {
        SVGDebugMesh debugMesh = (SVGDebugMesh)target;
        if(debugMesh != null)
        {
            MeshFilter meshFilter = debugMesh.gameObject.GetComponent<MeshFilter>();
            if(meshFilter != null)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if(mesh != null)
                {
                    Vector3[] vertices = mesh.vertices;
                    Color32[] colors32 = mesh.colors32;
                    int[] triangles = mesh.triangles;
                    Vector2[] uv = mesh.uv;
                    Vector2[] uv2 = mesh.uv2;
					#if !UNITY_4_6 && !UNITY_4_7 && !UNITY_4_8 && !UNITY_4_9
                    Vector2[] uv3 = mesh.uv3;
                    Vector2[] uv4 = mesh.uv4;
					#endif
                    Vector3[] normals = mesh.normals;
                    Vector4[] tangents = mesh.tangents;

                    if(vertices != null)
                        EditorGUILayout.LabelField("vertices: "+vertices.Length);
                    if(colors32 != null)
                        EditorGUILayout.LabelField("colors32: "+colors32.Length);
                    if(triangles != null)
                        EditorGUILayout.LabelField("triangles: "+triangles.Length / 3);
                    if(uv != null)
                        EditorGUILayout.LabelField("uv: "+uv.Length);
                    if(uv2 != null)
                        EditorGUILayout.LabelField("uv2: "+uv2.Length);
					#if !UNITY_4_6 && !UNITY_4_7 && !UNITY_4_8 && !UNITY_4_9
                    if(uv3 != null)
                        EditorGUILayout.LabelField("uv3: "+uv3.Length);
                    if(uv4 != null)
                        EditorGUILayout.LabelField("uv4: "+uv4.Length);
					#endif
                    if(normals != null)
                        EditorGUILayout.LabelField("normals: "+normals.Length);
                    if(tangents != null)
                        EditorGUILayout.LabelField("tangents: "+tangents.Length);
                }
            }

            EditorGUI.BeginChangeCheck();
            debugType = (DebugType)EditorGUILayout.EnumPopup(debugType);
            showPoints = EditorGUILayout.Toggle("Show Points", showPoints);
			showLabels = EditorGUILayout.Toggle("Show Labels", showLabels);
			showNormals = EditorGUILayout.Toggle("Show Normals", showNormals);
            startIndex = EditorGUILayout.IntField("Start Index", startIndex);
            endIndex = EditorGUILayout.IntField("End Index", endIndex);
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
                SceneView.RepaintAll();
            }
        }
    }

    void OnSceneGUI()
    {
        SVGDebugMesh debugMesh = (SVGDebugMesh)target;
        if(debugMesh != null)
        {
            MeshFilter meshFilter = debugMesh.gameObject.GetComponent<MeshFilter>();
            if(meshFilter != null)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if(mesh != null)
                {
                    Vector3[] vertices = mesh.vertices;
                    Color32[] colors32 = mesh.colors32;
//                    int[] triangles = mesh.triangles;
                    Vector2[] uv = mesh.uv;
                    Vector2[] uv2 = mesh.uv2;
					#if !UNITY_4_6 && !UNITY_4_7 && !UNITY_4_8 && !UNITY_4_9
                    Vector2[] uv3 = mesh.uv3;
                    Vector2[] uv4 = mesh.uv4;
					#endif
                    Vector3[] normals = mesh.normals;
                    Vector4[] tangents = mesh.tangents;

                    Handles.matrix = debugMesh.transform.localToWorldMatrix;
                    int vertexCount = mesh.vertexCount;
                    int start = 0;
                    int end = vertexCount;

                    if(startIndex > -1)
                    {
                        start = Mathf.Clamp(startIndex, 0, vertexCount - 1);
                    }

                    if(endIndex > -1)
                    {
                        end = Mathf.Clamp(endIndex, startIndex, vertexCount - 1);
                    }

                    if(debugType != DebugType.NONE)
                    {

                        for(int i = start; i < end; i++)
                        {
                            if(showPoints) Handles.DrawWireDisc(vertices[i], Vector3.forward, HandleUtility.GetHandleSize(debugMesh.transform.position) * 0.1f);
                            switch(debugType)
                            {
                                case DebugType.INDEXES:
                                    if(vertices == null || vertices.Length != vertexCount) continue;
									if(showLabels) Handles.Label(vertices[i], i.ToString());
                                    break;
                                case DebugType.VERTICES:
                                    if(vertices == null || vertices.Length != vertexCount) continue;
									if(showLabels) Handles.Label(vertices[i], vertices[i].ToString());
                                    break;
                                case DebugType.COLORS:
                                    if(colors32 == null || colors32.Length != vertexCount) continue;
									if(showLabels) Handles.Label(vertices[i], colors32[i].ToString());
                                    break;
                                case DebugType.UV:
                                    if(uv == null || uv.Length != vertexCount) continue;
									if(showLabels) Handles.Label(vertices[i], uv[i].ToString());
									if(showNormals) Handles.DrawLine(vertices[i], vertices[i] + (Vector3)uv[i]);
                                    break;
                                case DebugType.UV2:
                                    if(uv2 == null || uv2.Length != vertexCount) continue;
									if(showLabels) Handles.Label(vertices[i], uv2[i].ToString());
									if(showNormals) Handles.DrawLine(vertices[i], vertices[i] + (Vector3)uv2[i]);
                                    break;
								#if !UNITY_4_6 && !UNITY_4_7 && !UNITY_4_8 && !UNITY_4_9
                                case DebugType.UV3:
                                    if(uv3 == null || uv3.Length != vertexCount) continue;
									if(showLabels) Handles.Label(vertices[i], uv3[i].ToString());
									if(showNormals) Handles.DrawLine(vertices[i], vertices[i] + (Vector3)uv3[i]);
                                    break;
                                case DebugType.UV4:
                                    if(uv4 == null || uv4.Length != vertexCount) continue;
									if(showLabels) Handles.Label(vertices[i], uv4[i].ToString());
									if(showNormals) Handles.DrawLine(vertices[i], vertices[i] + (Vector3)uv4[i]);
                                    break;
								#endif
                                case DebugType.NORMALS:
                                    if(normals == null || normals.Length != vertexCount) continue;
									if(showLabels) Handles.Label(vertices[i], normals[i].ToString());
									if(showNormals) Handles.DrawLine(vertices[i], vertices[i] + normals[i]);
                                    break;
                                case DebugType.TANGENTS:
                                    if(tangents == null || tangents.Length != vertexCount) continue;
									if(showLabels) Handles.Label(vertices[i], tangents[i].ToString());
									if(showNormals) Handles.DrawLine(vertices[i], vertices[i] + (Vector3)tangents[i]);
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }

}
