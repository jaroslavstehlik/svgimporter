// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SVGImporter
{
	public class SVGAssetSnapshot
	{
		public SVGAssetFormat format;
        public SVGUseGradients useGradients;
        public bool antialiasing;
		public SVGMeshCompression meshCompression;
		public float scale;
		public float vpm;
		public float depthOffset;
		public bool compressDepth;
		public bool customPivotPoint;
		public Vector2 pivotPoint;
        public Vector4 border;
        public bool sliceMesh;
        public bool generateCollider;
		public bool keepSVGFile;
		public bool useLayers;
        public bool ignoreSVGCanvas;
        public bool optimizeMesh;
        public bool generateNormals;
        public bool generateTangents;

		public SVGAssetSnapshot(){}
		
		public SVGAssetSnapshot(SVGAsset svgAsset)
		{
			Apply(svgAsset);
		}

		public SVGAssetSnapshot(SVGAssetSnapshot snapshot)
		{
			Apply(snapshot);
		}

		public SVGAssetSnapshot(SerializedObject serializedObject)
		{
			Apply(serializedObject);
		}

		public SVGAssetSnapshot Clone()
		{
			SVGAssetSnapshot snapshot = new SVGAssetSnapshot();
			snapshot.format = this.format;
            snapshot.useGradients = this.useGradients;
            snapshot.antialiasing = this.antialiasing;
			snapshot.meshCompression = this.meshCompression;
			snapshot.scale = this.scale;
			snapshot.vpm = this.vpm;
			snapshot.depthOffset = this.depthOffset;
			snapshot.compressDepth = this.compressDepth;
			snapshot.customPivotPoint = this.customPivotPoint;
			snapshot.pivotPoint = this.pivotPoint;
            snapshot.border = this.border;
            snapshot.sliceMesh = this.sliceMesh;
            snapshot.generateCollider = this.generateCollider;
			snapshot.keepSVGFile = this.keepSVGFile;
			snapshot.useLayers = this.useLayers;
            snapshot.ignoreSVGCanvas = this.ignoreSVGCanvas;
            snapshot.optimizeMesh = this.optimizeMesh;
            snapshot.generateNormals = this.generateNormals;
            snapshot.generateTangents = this.generateTangents;
			return snapshot;
		}

		public void Apply(SVGAsset svgAsset)
		{
			this.format = svgAsset.format;
            this.useGradients = svgAsset.useGradients;
            this.antialiasing = svgAsset.antialiasing;
			this.meshCompression = svgAsset.meshCompression;
			this.scale = svgAsset.scale;
			this.vpm = svgAsset.vpm;
			this.depthOffset = svgAsset.depthOffset;
			this.compressDepth = svgAsset.compressDepth;
			this.customPivotPoint = svgAsset.customPivotPoint;
			this.pivotPoint = svgAsset.pivotPoint;
            this.border = svgAsset.border;
            this.sliceMesh = svgAsset.sliceMesh;
            this.generateCollider = svgAsset.generateCollider;
			this.keepSVGFile = svgAsset.keepSVGFile;
			this.useLayers = svgAsset.useLayers;
            this.ignoreSVGCanvas = svgAsset.ignoreSVGCanvas;
            this.optimizeMesh = svgAsset.optimizeMesh;
            this.generateNormals = svgAsset.generateNormals;
            this.generateTangents = svgAsset.generateTangents;
		}

		public void Apply(SVGAssetSnapshot snapshot)
		{
			this.format = snapshot.format;
            this.useGradients = snapshot.useGradients;
            this.antialiasing = snapshot.antialiasing;
			this.meshCompression = snapshot.meshCompression;
			this.scale = snapshot.scale;
			this.vpm = snapshot.vpm;
			this.depthOffset = snapshot.depthOffset;
			this.compressDepth = snapshot.compressDepth;
			this.customPivotPoint = snapshot.customPivotPoint;
			this.pivotPoint = snapshot.pivotPoint;
            this.border = snapshot.border;
            this.sliceMesh = snapshot.sliceMesh;
            this.generateCollider = snapshot.generateCollider;
			this.keepSVGFile = snapshot.keepSVGFile;
			this.useLayers = snapshot.useLayers;
            this.ignoreSVGCanvas = snapshot.ignoreSVGCanvas;
            this.optimizeMesh = snapshot.optimizeMesh;
            this.generateNormals = snapshot.generateNormals;
            this.generateTangents = snapshot.generateTangents;
		}
		
		public void Apply(SerializedObject serializedObject)
		{
			this.format = (SVGAssetFormat)serializedObject.FindProperty("_format").enumValueIndex;
            this.useGradients = (SVGUseGradients)serializedObject.FindProperty("_useGradients").enumValueIndex;
            this.antialiasing = serializedObject.FindProperty("_antialiasing").boolValue;
			this.meshCompression = (SVGMeshCompression)serializedObject.FindProperty("_meshCompression").enumValueIndex;
			this.scale = serializedObject.FindProperty("_scale").floatValue;
			this.vpm = serializedObject.FindProperty("_vpm").floatValue;
			this.depthOffset = serializedObject.FindProperty("_depthOffset").floatValue;
			this.compressDepth = serializedObject.FindProperty("_compressDepth").boolValue;
			this.customPivotPoint = serializedObject.FindProperty("_customPivotPoint").boolValue;
			this.pivotPoint = serializedObject.FindProperty("_pivotPoint").vector2Value;
            this.border = serializedObject.FindProperty("_border").vector4Value;
            this.sliceMesh = serializedObject.FindProperty("_sliceMesh").boolValue;
            this.generateCollider = serializedObject.FindProperty("_generateCollider").boolValue;
			this.keepSVGFile = serializedObject.FindProperty("_keepSVGFile").boolValue;
			this.useLayers = serializedObject.FindProperty("_useLayers").boolValue;
            this.ignoreSVGCanvas = serializedObject.FindProperty("_ignoreSVGCanvas").boolValue;
            this.optimizeMesh = serializedObject.FindProperty("_optimizeMesh").boolValue;
            this.generateNormals = serializedObject.FindProperty("_generateNormals").boolValue;
            this.generateTangents = serializedObject.FindProperty("_generateTangents").boolValue;
		}

		public void ModifySerializedObject(SerializedObject serializedObject)
		{
			serializedObject.FindProperty("_format").enumValueIndex = (int)this.format;
            serializedObject.FindProperty("_useGradients").enumValueIndex = (int)this.useGradients;
            serializedObject.FindProperty("_antialiasing").boolValue = this.antialiasing;
			serializedObject.FindProperty("_meshCompression").enumValueIndex = (int)this.meshCompression;
			serializedObject.FindProperty("_scale").floatValue = this.scale;
			serializedObject.FindProperty("_vpm").floatValue = this.vpm;
			serializedObject.FindProperty("_depthOffset").floatValue = this.depthOffset;
			serializedObject.FindProperty("_compressDepth").boolValue = this.compressDepth;
			serializedObject.FindProperty("_customPivotPoint").boolValue = this.customPivotPoint;
			serializedObject.FindProperty("_pivotPoint").vector2Value = this.pivotPoint;
            serializedObject.FindProperty("_border").vector4Value = this.border;
            serializedObject.FindProperty("_sliceMesh").boolValue = this.sliceMesh;
            serializedObject.FindProperty("_generateCollider").boolValue = this.generateCollider;
			serializedObject.FindProperty("_keepSVGFile").boolValue = this.keepSVGFile;
			serializedObject.FindProperty("_useLayers").boolValue = this.useLayers;
            serializedObject.FindProperty("_ignoreSVGCanvas").boolValue = this.ignoreSVGCanvas;
            serializedObject.FindProperty("_optimizeMesh").boolValue = this.optimizeMesh;
            serializedObject.FindProperty("_generateNormals").boolValue = this.generateNormals;
            serializedObject.FindProperty("_generateTangents").boolValue = this.generateTangents;
		}

		public void ModifySVGAssetSnapshot(SVGAssetSnapshot snapshot)
		{
			snapshot.format = this.format;
            snapshot.useGradients = this.useGradients;
            snapshot.antialiasing = this.antialiasing;
			snapshot.meshCompression = this.meshCompression;
			snapshot.scale = this.scale;
			snapshot.vpm = this.vpm;
			snapshot.depthOffset = this.depthOffset;
			snapshot.compressDepth = this.compressDepth;
			snapshot.customPivotPoint = this.customPivotPoint;
			snapshot.pivotPoint = this.pivotPoint;
            snapshot.border = this.border;
            snapshot.sliceMesh = this.sliceMesh;
            snapshot.generateCollider = this.generateCollider;
			snapshot.keepSVGFile = this.keepSVGFile;
			snapshot.useLayers = this.useLayers;
            snapshot.ignoreSVGCanvas = this.ignoreSVGCanvas;
            snapshot.optimizeMesh = this.optimizeMesh;
            snapshot.generateNormals = this.generateNormals;
            snapshot.generateTangents = this.generateTangents;
		}
	}

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SVGAsset))]
    public class SVGAssetEditor : Editor
    {
        public static SVGAssetEditor Instance;

        const string SVGAsset_LastSVGRecoveryKey = "SVGAsset_LastSVGRecoveryKey";
        public string lastSVGRecoveryPath
        {
            get {
                string output = null;
                if(EditorPrefs.HasKey(SVGAsset_LastSVGRecoveryKey))
                {
                    output = EditorPrefs.GetString(SVGAsset_LastSVGRecoveryKey);
                    if(string.IsNullOrEmpty(output))
                        return null;

                    if(!Directory.Exists(output))
                        return null;
                }
                return output;
            }
            set {
                EditorPrefs.SetString(SVGAsset_LastSVGRecoveryKey, value);
            }
        }

        const string SVGAsset_LastMeshSaveKey = "SVGAsset_LastMeshSaveKey";
        public string lastMeshSavePath
        {
            get {
                string output = null;
                if(EditorPrefs.HasKey(SVGAsset_LastMeshSaveKey))
                {
                    output = EditorPrefs.GetString(SVGAsset_LastMeshSaveKey);
                    if(string.IsNullOrEmpty(output))
                        return null;
                    
                    if(!Directory.Exists(output))
                        return null;
                }
                return output;
            }
            set {
                EditorPrefs.SetString(SVGAsset_LastMeshSaveKey, value);
            }
        }

		SVGAssetSnapshot[] svgAssetSnapshots;

        SVGAsset asset;
        SVGAsset[] assets;

        SerializedProperty format;
        SerializedProperty useGradients;
        SerializedProperty antialiasing;
		SerializedProperty meshCompression;
        SerializedProperty scale;
        SerializedProperty vpm;
        SerializedProperty depthOffset;
        SerializedProperty compressDepth;
        SerializedProperty customPivotPoint;
        SerializedProperty pivotPoint;
        //SerializedProperty border;
        SerializedProperty generateCollider;
		SerializedProperty keepSVGFile;
		SerializedProperty useLayers;
        SerializedProperty ignoreSVGCanvas;
        SerializedProperty optimizeMesh;
        SerializedProperty generateNormals;
        SerializedProperty generateTangents;

        public bool unappliedChanges;
        bool filesValid;

        public static string[] anchorPosition = new string[]{
            "Top left",
            "Top",
            "Top right",
            "Left",
            "Center",
            "Right",
            "Bottom left",
            "Bottom",
            "Bottom Right"
        };

		public static GUIContent[] anchorPositionContent;

        static SVGAsset CreateAsset()
        {       
            SVGAsset tempAsset = SVGAsset.CreateInstance<SVGAsset>();
            return tempAsset;
        }

        private PreviewRenderUtility m_PreviewUtility;
        private void Init()
        {
            if (this.m_PreviewUtility == null)
            {
                this.m_PreviewUtility = new PreviewRenderUtility();
                this.m_PreviewUtility.cameraFieldOfView = 30f;
            }
        }

        void OnEnable()
        {
            Instance = this;
            asset = (SVGAsset)serializedObject.targetObject;

			anchorPositionContent = new GUIContent[anchorPosition.Length];
			for(int i = 0; i < anchorPosition.Length; i++)
			{
				anchorPositionContent[i] = new GUIContent(anchorPosition[i]);
			}

            if(serializedObject.isEditingMultipleObjects)
            {
                assets = new SVGAsset[serializedObject.targetObjects.Length];
                for(int i = 0; i < serializedObject.targetObjects.Length; i++)
                {
                    assets[i] = (SVGAsset)serializedObject.targetObjects[i];
                }
            }

            filesValid = true;
            if(serializedObject.isEditingMultipleObjects)
            {
                for(int i = 0; i < assets.Length; i++)
                {
                    if(string.IsNullOrEmpty(assets[i].svgFile))
                        filesValid = false;
                }
            } else {
                if(string.IsNullOrEmpty(asset.svgFile))
                    filesValid = false;
            }

            format = serializedObject.FindProperty("_format");
            useGradients = serializedObject.FindProperty("_useGradients");
            antialiasing = serializedObject.FindProperty("_antialiasing");
			meshCompression = serializedObject.FindProperty("_meshCompression");
            scale = serializedObject.FindProperty("_scale");
            vpm = serializedObject.FindProperty("_vpm");
            depthOffset = serializedObject.FindProperty("_depthOffset");
            compressDepth = serializedObject.FindProperty("_compressDepth");
            customPivotPoint = serializedObject.FindProperty("_customPivotPoint");
            pivotPoint = serializedObject.FindProperty("_pivotPoint");
            //border = serializedObject.FindProperty("_border");
            generateCollider = serializedObject.FindProperty("_generateCollider");
			keepSVGFile = serializedObject.FindProperty("_keepSVGFile");
			useLayers = serializedObject.FindProperty("_useLayers");
            ignoreSVGCanvas = serializedObject.FindProperty("_ignoreSVGCanvas");
            optimizeMesh = serializedObject.FindProperty("_optimizeMesh");
            generateNormals = serializedObject.FindProperty("_generateNormals");
            generateTangents = serializedObject.FindProperty("_generateTangents");

            CreateSnapshot();
            unappliedChanges = false;
        }

        void OnDisable()
        {
            if(unappliedChanges)
            {
                string assetPaths = "";
                if(serializedObject.isEditingMultipleObjects)
                {
                    for(int i = 0; i < assets.Length; i++)
                    {
                        assetPaths += "'"+AssetDatabase.GetAssetPath(assets[i])+"'\n";
                    }
                } else {
                    assetPaths = AssetDatabase.GetAssetPath(asset);
                }

                if(EditorUtility.DisplayDialog("Unapplied import settings", "Unapplied import settings for "+assetPaths, "Apply", "Revert"))
                {
                    ApplyChanges();
                } else {
                    RevertChanges();
                }
                unappliedChanges = false;
            }
            Instance = null;
        }

        void OnDestroy()
        {
            if (this.m_PreviewUtility != null)
            {
                this.m_PreviewUtility.Cleanup();
                this.m_PreviewUtility = null;
            }
            Instance = null;
        }

        /// <summary>True when the asset was imported from an .svg file, whose importer holds the settings.</summary>
        internal static bool IsImportedFromSVGFile(UnityEngine.Object asset)
        {
            return AssetDatabase.GetAssetPath(asset).EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase);
        }

        public override void OnInspectorGUI()
        {
            if(IsImportedFromSVGFile(asset))
            {
                // Shown below the importer's settings: the asset itself is read-only.
                EditorGUILayout.LabelField(asset._editor_Info, EditorStyles.miniLabel);
                SVGImportSettingsGUI.DrawErrors(GetEditorErrors(asset), asset.exactGroupOpacity);
                return;
            }

            Instance = this;
            if (filesValid)
            {
                OnFilesValid();
            } else
            {
                EditorGUILayout.HelpBox("File is invalid or corrupted!", MessageType.Error);
            }
        }

        public static int GetPivotPointIndex(Vector2 pivotPointVector)
        {
            return Mathf.RoundToInt(pivotPointVector.x * 2 + Mathf.Clamp(pivotPointVector.y * 6, 0, 8));
        }

        public static Vector2 GetPivotPoint(int index)
        {
            int x = index % 3;
            int y = Mathf.FloorToInt(index / 3);

            return new Vector2(x / 2f, y / 2f);
        }

        public static GUIContent FORMAT_LABEL = new GUIContent("Format", "The rendering format of the SVG Asset.");
        public static GUIContent USE_GRADIENTS_LABEL = new GUIContent("Gradients", "Simplify shaders and mesh for specific use case");
        public static GUIContent ANTIALIASING_LABEL = new GUIContent("Soft Edges", "Adds a soft one-pixel edge to every shape in the mesh. For MSAA instead, turn this off and enable MSAA on the camera.");
        public static string SHARP_EDGES_HINT = "Edges are sharp unless the camera uses MSAA.";
		public static GUIContent ANTIALIASING_WIDTH_LABEL = new GUIContent("Soft Edge Width", "Width of the soft edge, in screen pixels.");
        public static GUIContent MESH_COMPRESSION_LABEL = new GUIContent("Mesh Compression", "Reduce file size of the mesh, but might introduce irregularities and visual artefacts.");
        public static string MESH_COMPRESSION_HELPBOX_LABEL = "Mesh compression can introduce unwanted visual artefacts.\nThe higher the compression, the higher the risk.";
        public static GUIContent OPTIMIZE_MESH_LABEL = new GUIContent("Optimize Mesh", "The vertices and indices will be reorderer for better GPU performance.");
        public static GUIContent SCALE_LABEL = new GUIContent("Scale", "The scale of the mesh relative to the SVG Asset. Does not affect the quality of the mesh");
        public static GUIContent QUALITY_LABEL = new GUIContent("Quality", "Larger number means better but more complex mesh, Vertex Per Meter represents number of vertices in the SVG Asset that correspond to one unit in world space.");
        public static GUIContent DEPTH_OFFSET_LABEL = new GUIContent("Depth Offset", "The minimal z-offset in WorldSpace for Opaque Rendering.");
        public static GUIContent COMPRESS_DEPTH_LABEL = new GUIContent("Compress Depth", "Compresses the overlapping objects to reduce z-offset requirements.");
        public static GUIContent CUSTOM_PIVOT_LABEL = new GUIContent("Custom Pivot", "Choose the predefined pivot point or the custom pivot point.");
        public static GUIContent PIVOT_LABEL = new GUIContent("Pivot", "The location of the SVG Asset center point in the original Rect, specified in percents.");
        public static GUIContent GENERATE_COLLIDER_LABEL = new GUIContent("Generate Collider", "Automatically generates polygon colliders.");
        public static GUIContent KEEP_SVG_FILE_LABEL = new GUIContent("Keep SVG File", "Keep the SVG file in the final build. This increases the file size.");
		public static GUIContent USE_LAYERS_LABEL = new GUIContent("Use Layers", "Store individual SVG Layers for further modification. This discards stored Mesh.");
        public static GUIContent EXACT_GROUP_OPACITY_LABEL = new GUIContent("Exact Group Opacity", "Stops shapes in a semi-transparent group from showing through each other. Adds vertices.");
        public static GUIContent IGNORE_SVG_CANVAS_LABEL = new GUIContent("Ignore SVG Canvas", "Trim the document canvas to object bounding box.");
        public static GUIContent GENERATE_NORMALS_LABEL = new GUIContent("Normals", "Generate normals for lighting effects.");
        public static GUIContent GENERATE_TANGENTS_LABEL = new GUIContent("Tangents", "Generate Tangents for advanced lighting effects.");

        void OnFilesValid()
        {
			bool valueChanged = false;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            if(SVGImportSettingsGUI.Draw(serializedObject, !serializedObject.isEditingMultipleObjects))
            {
                SVGEditorWindow.GetWindow();
            }

            if(EditorGUI.EndChangeCheck())
            {
                valueChanged = true;
            }

            GUILayout.BeginHorizontal();
			if(GUILayout.Button(new GUIContent("Recover SVG File", "Save the original SVG Document to a specified directory.")))
            {            
                if(serializedObject.isEditingMultipleObjects)
                {
                    for(int i = 0; i < assets.Length; i++)
                    {
                        RecoverSVGFile(assets[i]);
                    }
                } else {
                    RecoverSVGFile(asset);
                }
            }

			if(GUILayout.Button(new GUIContent("Save Mesh File", "Save the mesh asset to a specified directory.")))
            {            
                if(serializedObject.isEditingMultipleObjects)
                {
                    for(int i = 0; i < assets.Length; i++)
                    {
                        SaveMeshFile(assets[i]);
                    }
                } else {
                    SaveMeshFile(asset);
                }
            }

			if(valueChanged)
            {
                unappliedChanges = true;
                serializedObject.ApplyModifiedProperties();
                RepaintSVGEditorWindow();
            }
            GUILayout.EndHorizontal();

			EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = unappliedChanges && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;

			if(GUILayout.Button(new GUIContent("Revert", "Revert all changes.")))
            {
				RevertChanges();
            }

			if (GUILayout.Button(new GUIContent("Apply", "Apply all changes.")))
            {
                serializedObject.ApplyModifiedProperties();
                ApplyChanges();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            SVGImportSettingsGUI.DrawErrors(GetEditorErrors(asset), asset.exactGroupOpacity);

            GUILayout.FlexibleSpace();
            SVGImportSettingsGUI.DrawReportIssueButton(asset.name);
        }

        protected void RecoverSVGFile(SVGAsset recoverAsset)
        {
            string assetPath = AssetDatabase.GetAssetPath(recoverAsset);
            string lastPath = lastSVGRecoveryPath;
            if(string.IsNullOrEmpty(lastPath))
            {
                lastPath = assetPath;           
            } else {
                lastPath += "/";
            }
            string path = EditorUtility.SaveFilePanel("Recover SVG File", Path.GetDirectoryName(lastPath), Path.GetFileNameWithoutExtension(assetPath), "svg" );
            if(!string.IsNullOrEmpty(path))
            {
                lastSVGRecoveryPath = Path.GetDirectoryName(path);
                File.WriteAllText(path, recoverAsset.svgFile);
                EditorUtility.RevealInFinder(path);
            }
        }

        protected void SaveMeshFile(SVGAsset meshAsset)
        {
            string assetPath = AssetDatabase.GetAssetPath(meshAsset);
            string lastPath = lastMeshSavePath;
            if(string.IsNullOrEmpty(lastPath))
            {
                lastPath = assetPath;           
            } else {
                lastPath += "/";
            }
            string path = EditorUtility.SaveFilePanel("Save Mesh File", Path.GetDirectoryName(lastPath), Path.GetFileNameWithoutExtension(assetPath)+"-mesh", "asset" );
            if(!string.IsNullOrEmpty(path))
            {
                System.Uri assetFolderPath = new Uri(Application.dataPath);
                System.Uri outputPath = new Uri(path);
                path = assetFolderPath.MakeRelativeUri(outputPath).ToString();

                lastSVGRecoveryPath = Path.GetDirectoryName(path);

                Mesh sharedMesh = meshAsset.sharedMesh;
                Mesh mesh = new Mesh();
                mesh.name = sharedMesh.name;
                mesh.vertices = (Vector3[])sharedMesh.vertices.Clone();
                mesh.triangles = (int[])sharedMesh.triangles.Clone();
                if(sharedMesh.uv != null || sharedMesh.uv.Length > 0)
                    mesh.uv = (Vector2[])sharedMesh.uv.Clone();
                if(sharedMesh.colors32 != null || sharedMesh.colors32.Length > 0)
                    mesh.colors32 = (Color32[])sharedMesh.colors32.Clone();
                AssetDatabase.CreateAsset(mesh, path);
                EditorUtility.RevealInFinder(path);
            }
        }

        public void ApplyChanges()
        {
            if(serializedObject.isEditingMultipleObjects)
            {
                int importTotalAssets = assets.Length;
                int currentAssetIndex = 0;
                for(int i = 0; i < importTotalAssets; i++)
                {
                    string assetPath = AssetDatabase.GetAssetPath(assets[i]);
                    float importProgress = (float)currentAssetIndex / (float)importTotalAssets;
                    if(EditorUtility.DisplayCancelableProgressBar("Importing SVG Assets", "Importing SVG Asset: "+assetPath+"...", importProgress))
                    {
                        EditorUtility.ClearProgressBar();
                        break;
                    } else {
                        assets[i]._editor_ApplyChanges();
                        currentAssetIndex++;
                    }
                }

                EditorUtility.ClearProgressBar();
            } else {
                asset._editor_ApplyChanges();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Canvas.ForceUpdateCanvases();
            UnityEditor.SceneView.RepaintAll();
            unappliedChanges = false;

            GUI.FocusControl(null);
            EditorGUI.FocusTextInControl(null);

            UpdateInstances(this.serializedObject);
            UpdateSVGEditorWindow();
        }

        private void UpdateSVGEditorWindow()
        {
            SVGEditorWindow svgEditorWindow = SVGEditorWindow.s_Instance;
            if(svgEditorWindow != null)
            {
                svgEditorWindow.ManualUpdate();
            }
        }

        private void RepaintSVGEditorWindow()
        {
            SVGEditorWindow svgEditorWindow = SVGEditorWindow.s_Instance;
            if(svgEditorWindow != null)
            {
                svgEditorWindow.Repaint();
            }
        }

        private string Format(long ts)
        {
            return String.Format("{0} ms", ts);
        }

        public override bool HasPreviewGUI() { return this.target != null; }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y, rect.width, 40f), "Preview requires\nrender texture support");
                }
                return;
            }
            if (this.target == null)
            {
                return;
            }
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            this.Init();
            this.m_PreviewUtility.BeginPreview(rect, background);
            this.DoRenderPreview();
            Texture image = this.m_PreviewUtility.EndPreview();
            GUI.DrawTexture(rect, image, ScaleMode.StretchToFill, false);
        }
        
        public override void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                return;
            }
            GUI.enabled = true;
            this.Init();
        }

		void DoRenderPreview()
        {            
			SVGEditorWindow.DoRenderPreview(target as SVGAsset, m_PreviewUtility);
        }

        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                return null;
            }
            if(width <= 20 || height <= 20)
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.jaroslavstehlik.svgimporter/Editor/Gizmos/SVGAsset icon.png");
            } else {
                this.Init();
                this.m_PreviewUtility.BeginStaticPreview(new Rect(0f, 0f, (float)width, (float)height));
                this.DoRenderPreview();
                return this.m_PreviewUtility.EndStaticPreview();
            }
        }

        public override string GetInfoString()
        {        
            string editorInfo = "";

            if(serializedObject.isEditingMultipleObjects)
            {
                int assetLength = assets.Length - 1;
                for(int i = 0; i < assetLength; i++)
                {
                    editorInfo += GetEditorInfo(assets[i]) + ", ";
                }

                editorInfo += GetEditorInfo(assets[assetLength]);;
            } else {
                editorInfo = GetEditorInfo(asset);
            }

            return editorInfo;
        }

		public void RevertChanges()
		{
			for(int i = 0; i < svgAssetSnapshots.Length; i++)
			{
				SerializedObject serializedObject = new SerializedObject(targets[i]);
				svgAssetSnapshots[i].ModifySerializedObject(serializedObject);
				serializedObject.ApplyModifiedProperties();
				serializedObject.Update();
			}

			this.serializedObject.SetIsDifferentCacheDirty();
			this.serializedObject.Update();
			unappliedChanges = false;

			GUI.FocusControl(null);
			EditorGUI.FocusTextInControl(null);

            UpdateInstances(this.serializedObject);
            UpdateSVGEditorWindow();
		}

		public void CreateSnapshot()
		{			
			if(serializedObject.isEditingMultipleObjects)
			{
				int targetLength = serializedObject.targetObjects.Length;
				svgAssetSnapshots = new SVGAssetSnapshot[targetLength];
				for(int i = 0; i < targetLength; i++)
				{
					svgAssetSnapshots[i] = new SVGAssetSnapshot((SVGAsset)targets[i]);
				}
			} else {
				svgAssetSnapshots = new SVGAssetSnapshot[]{ new SVGAssetSnapshot(serializedObject) };
			}
		}

        protected string GetEditorInfo(SVGAsset asset)
        {
			string output = asset._editor_Info;

			var fileInfo = new System.IO.FileInfo(UnityEditor.AssetDatabase.GetAssetPath(asset));
			if(fileInfo != null)
			{
				output += ", FileSize: "+string.Format(new SVGImporter.Utils.FileSizeFormatProvider(), "{0:fs}", fileInfo.Length);
			}

			return output;
        }

        protected SVGError[] GetEditorErrors(SVGAsset asset)
        {
            return asset._editor_errors;
        }


        public static void UpdateInstances(SerializedObject serializedObject)
        {            
            if(serializedObject == null)
                return;

            if(serializedObject.targetObjects != null && serializedObject.targetObjects.Length > 0)
            {
                SVGAsset[] svgAssets = new SVGAsset[serializedObject.targetObjects.Length];
                for(int i = 0; i < svgAssets.Length; i++)
                {
                    svgAssets[i] = serializedObject.targetObjects[i] as SVGAsset;
                }
                
				UpdateInstances(svgAssets);
            }
        }

        public static void UpdateInstances(SVGAsset[] svgAssets)
        {
            if(svgAssets == null || svgAssets.Length == 0)
                return;

            SVGImage[] svgImages = Resources.FindObjectsOfTypeAll<SVGImage>();
            if(svgImages != null && svgImages.Length > 0)
            {
                for(int i = 0; i < svgImages.Length; i++)
                {
                    if(AssetDatabase.Contains(svgImages[i]))
                        continue;

                    for(int j = 0; j < svgAssets.Length; j++)
                    {
                        if(svgAssets[j] == null)
                            continue;

                        if(svgImages[i] == null || svgImages[i].vectorGraphics != svgAssets[j]) continue;

                        typeof(SVGImage).GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(svgImages[i], null);
                        typeof(SVGImage).GetMethod("UpdateMaterial", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(svgImages[i], null);
                        svgImages[i].SetAllDirty();
                    }
                }
            }

			SVGRenderer[] svgRenderers = Resources.FindObjectsOfTypeAll<SVGRenderer>();
			if(svgRenderers != null && svgRenderers.Length > 0)
			{
				for(int i = 0; i < svgRenderers.Length; i++)
				{
					if(AssetDatabase.Contains(svgRenderers[i]))
						continue;
					
					for(int j = 0; j < svgAssets.Length; j++)
					{
						if(svgAssets[j] == null)
							continue;
						
						if(svgRenderers[i] == null || svgRenderers[i].vectorGraphics != svgAssets[j]) continue;
						svgRenderers[i].UpdateRenderer();
					}
				}
			}
        }
    }
}
