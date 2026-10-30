// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Linq;
using UnityEngine;
using UnityEditor;

namespace SVGImporter
{
    /// <summary>
    /// The SVG Asset Inspector: what an import produced, and its preview. The settings of an SVG
    /// file are in its importer's Inspector. SVG Assets from 1.x (.asset files) are read-only;
    /// converting them to .svg files makes their settings editable again.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SVGAsset))]
    internal class SVGAssetEditor : Editor
    {
        static class Styles
        {
            public static readonly string legacyAsset = L10n.Tr("This SVG Asset is from SVG Importer 1.x. Convert it to an .svg file to change its import settings.");
            public static readonly GUIContent convert = EditorGUIUtility.TrTextContent("Convert to SVG File", "Replaces the .asset file with an .svg file that has the same import settings, and updates references to it.");
            public static readonly string noPreview = L10n.Tr("Preview requires\nrender texture support");
        }

        PreviewRenderUtility m_PreviewUtility;

        /// <summary>True when the asset was imported from an .svg file, whose importer holds the settings.</summary>
        internal static bool IsImportedFromSVGFile(Object asset)
        {
            return AssetDatabase.GetAssetPath(asset).EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase);
        }

        void OnDisable()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
        }

        public override void OnInspectorGUI()
        {
            var asset = (SVGAsset)target;
            if (IsImportedFromSVGFile(asset))
            {
                // Shown below the importer's settings.
                EditorGUILayout.LabelField(asset._editor_Info, EditorStyles.miniLabel);
                SVGImportSettingsGUI.DrawErrors(asset._editor_errors, asset.exactGroupOpacity);
                return;
            }

            EditorGUILayout.HelpBox(Styles.legacyAsset, MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Styles.convert))
                {
                    string[] paths = targets.Select(AssetDatabase.GetAssetPath).ToArray();
                    // The conversion replaces the selected assets: leave the GUI before it does.
                    EditorApplication.delayCall += () => SVGAssetConverter.ConvertWithDialogs(paths);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.Space();

            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                SVGImportSettingsGUI.Draw(serializedObject, false);
            if (!serializedObject.isEditingMultipleObjects)
                SVGImportSettingsGUI.DrawErrors(asset._editor_errors, asset.exactGroupOpacity);
        }

        public override string GetInfoString()
        {
            return string.Join(", ", targets.Cast<SVGAsset>().Select(asset => asset._editor_Info));
        }

        public override bool HasPreviewGUI() { return target != null; }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y, rect.width, 40f), Styles.noPreview);
                return;
            }
            if (Event.current.type != EventType.Repaint)
                return;

            InitPreview();
            m_PreviewUtility.BeginPreview(rect, background);
            RenderPreview((SVGAsset)target, m_PreviewUtility);
            GUI.DrawTexture(rect, m_PreviewUtility.EndPreview(), ScaleMode.StretchToFill, false);
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return null;
            if (width <= 20 || height <= 20)
                return AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.jaroslavstehlik.svgimporter/Editor/Gizmos/SVGAsset icon.png");

            InitPreview();
            m_PreviewUtility.BeginStaticPreview(new Rect(0f, 0f, width, height));
            RenderPreview((SVGAsset)target, m_PreviewUtility);
            return m_PreviewUtility.EndStaticPreview();
        }

        void InitPreview()
        {
            if (m_PreviewUtility == null)
                m_PreviewUtility = new PreviewRenderUtility { cameraFieldOfView = 30f };
        }

        // Draws the mesh scene renderers draw, with its gradients in the shared atlas, framed by
        // the camera.
        static void RenderPreview(SVGAsset asset, PreviewRenderUtility preview)
        {
            Mesh mesh = asset != null ? asset.sharedMesh : null;
            Material[] materials = asset != null ? asset.sharedMaterials : null;
            if (mesh == null || materials == null || materials.Length == 0)
                return;
            // Build and bind the atlas now: thumbnails are rendered in an import worker process
            // (Unity's PreviewImporter), where nothing else does before this render.
            if (asset.hasGradients)
                SVGAtlas.Instance.OnAtlasPreRender();

            Bounds bounds = mesh.bounds;
            float radius = bounds.extents.magnitude;
            float distance = 4f * radius;
            Camera camera = preview.camera;
            camera.transform.position = -Vector3.forward * distance;
            camera.transform.rotation = Quaternion.identity;
            camera.nearClipPlane = distance - radius * 1.1f;
            camera.farClipPlane = distance + radius * 1.1f;
            camera.clearFlags = CameraClearFlags.Nothing;

            bool fog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);
            for (int i = 0; i < mesh.subMeshCount; i++)
                preview.DrawMesh(mesh, -bounds.center, Quaternion.identity, materials[Mathf.Min(i, materials.Length - 1)], i);
            camera.Render();
            Unsupported.SetRenderSettingsUseFogNoDirty(fog);
        }
    }
}
