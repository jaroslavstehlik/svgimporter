// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;

namespace SVGImporter
{
    /// <summary>
    /// Lets Unity's Sprite Editor (Window > 2D > Sprite Editor, from the 2D Sprite package) edit an
    /// SVG file's pivot and slicing border. The Sprite Editor shows the SVG rendered into a texture
    /// covering the rectangle the pivot refers to: the content's bounds, or the SVG canvas when
    /// Ignore SVG Canvas is off. Apply writes the pivot and border back to the import settings.
    /// </summary>
    internal sealed class SVGSpriteEditorDataProvider : ISpriteEditorDataProvider, ITextureDataProvider
#if SVGIMPORTER_SPRITE_EDIT_CAPABILITY
        , ISpriteFrameEditCapability
#endif
    {
        // The longest side of the texture the Sprite Editor shows.
        const int k_TextureSize = 1024;

        readonly SVGFileImporter m_Importer;
        SpriteRect m_Rect;
        Texture2D m_Texture;

        public SVGSpriteEditorDataProvider(SVGFileImporter importer)
        {
            m_Importer = importer;
        }

        public SpriteImportMode spriteImportMode { get { return SpriteImportMode.Single; } }
        public float pixelsPerUnit { get { return 100f; } }
        public UnityEngine.Object targetObject { get { return m_Importer; } }

        public void InitSpriteEditorDataProvider()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SVGAsset>(m_Importer.assetPath);
            var serialized = new SerializedObject(m_Importer);
            bool custom = serialized.FindProperty("_customPivotPoint").boolValue;
            Vector2 pivot = serialized.FindProperty("_pivotPoint").vector2Value;
            Vector4 border = serialized.FindProperty("_border").vector4Value;

            m_Texture = asset != null ? Render(asset, pivot) : null;
            int width = m_Texture != null ? m_Texture.width : k_TextureSize, height = m_Texture != null ? m_Texture.height : k_TextureSize;
            // SVG Assets store the pivot with y down and the border as fractions; sprites use y up and pixels.
            Vector2 spritePivot = new Vector2(pivot.x, 1f - pivot.y);
            m_Rect = new SpriteRect {
                name = asset != null ? asset.name : "SVG",
                rect = new Rect(0f, 0f, width, height),
                pivot = spritePivot,
                alignment = custom ? SpriteAlignment.Custom : Alignment(spritePivot),
                border = new Vector4(border.x * width, border.y * height, border.z * width, border.w * height),
                spriteID = new GUID(AssetDatabase.AssetPathToGUID(m_Importer.assetPath)),
            };
        }

        public SpriteRect[] GetSpriteRects()
        {
            return new[] { m_Rect };
        }

        public void SetSpriteRects(SpriteRect[] spriteRects)
        {
            if (spriteRects != null && spriteRects.Length > 0)
                m_Rect = spriteRects[0];
        }

        public void Apply()
        {
            float width = m_Rect.rect.width, height = m_Rect.rect.height;
            if (width <= 0f || height <= 0f) return;
            Vector2 spritePivot = m_Rect.alignment == SpriteAlignment.Custom ? m_Rect.pivot : AlignmentPivot(m_Rect.alignment);
            var serialized = new SerializedObject(m_Importer);
            serialized.FindProperty("_customPivotPoint").boolValue = m_Rect.alignment == SpriteAlignment.Custom;
            serialized.FindProperty("_pivotPoint").vector2Value = new Vector2(spritePivot.x, 1f - spritePivot.y);
            Vector4 border = m_Rect.border;
            serialized.FindProperty("_border").vector4Value = new Vector4(border.x / width, border.y / height, border.z / width, border.w / height);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public T GetDataProvider<T>() where T : class
        {
            return this as T;
        }

        public bool HasDataProvider(Type type)
        {
            return type.IsInstanceOfType(this);
        }

#if SVGIMPORTER_SPRITE_EDIT_CAPABILITY
        // Newer Sprite Editors lock what a provider doesn't allow: an SVG file is one sprite, so
        // its pivot and border can be edited, not its rectangle or name.
        EditCapability m_Capability = new EditCapability(EEditCapability.EditPivot, EEditCapability.EditBorder);

        public EditCapability GetEditCapability()
        {
            return m_Capability;
        }

        public void SetEditCapability(EditCapability editCapability)
        {
            m_Capability = editCapability;
        }
#endif

        public Texture2D texture { get { return m_Texture; } }
        public Texture2D previewTexture { get { return m_Texture; } }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            width = m_Texture != null ? m_Texture.width : k_TextureSize;
            height = m_Texture != null ? m_Texture.height : k_TextureSize;
        }

        public Texture2D GetReadableTexture2D()
        {
            return m_Texture;
        }

        // The rectangle the pivot refers to, in mesh units: the content's bounds, or the canvas,
        // placed so that the pivot is at the origin.
        static Rect PivotRect(SVGAsset asset, Vector2 pivot)
        {
            if (asset.ignoreSVGCanvas)
            {
                Bounds bounds = asset.bounds;
                return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
            }
            Vector2 size = asset.canvasRectangle.size;
            return new Rect(-size.x * pivot.x, -size.y * (1f - pivot.y), size.x, size.y);
        }

        // The asset rendered into a readable texture covering PivotRect, its longest side k_TextureSize.
        static Texture2D Render(SVGAsset asset, Vector2 pivot)
        {
            Mesh mesh = asset.sharedMesh;
            Material[] materials = asset.sharedMaterials;
            Rect rect = PivotRect(asset, pivot);
            if (mesh == null || materials == null || rect.width <= 0f || rect.height <= 0f) return null;

            float scale = k_TextureSize / Mathf.Max(rect.width, rect.height);
            int width = Mathf.Max(1, Mathf.RoundToInt(rect.width * scale)), height = Mathf.Max(1, Mathf.RoundToInt(rect.height * scale));
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            if (asset.hasGradients) SVGAtlas.Instance.OnAtlasPreRender();

            var commands = new CommandBuffer { name = "SVG Importer Sprite Editor" };
            commands.SetRenderTarget(target);
            commands.ClearRenderTarget(true, true, Color.clear);
            Matrix4x4 projection = Matrix4x4.Ortho(rect.xMin, rect.xMax, rect.yMin, rect.yMax, -1000f, 1000f);
            // Not flipped for the render texture: ReadPixels below reads it back the right way up.
            commands.SetViewProjectionMatrices(Matrix4x4.identity, GL.GetGPUProjectionMatrix(projection, false));
            for (int i = 0; i < mesh.subMeshCount && i < materials.Length; i++)
                if (materials[i] != null) commands.DrawMesh(mesh, Matrix4x4.identity, materials[i], i);
            Graphics.ExecuteCommandBuffer(commands);
            commands.Release();

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = asset.name, hideFlags = HideFlags.DontSave };
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return texture;
        }

        static readonly Vector2[] s_AlignmentPivots = {
            new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f),
        };

        static Vector2 AlignmentPivot(SpriteAlignment alignment)
        {
            int i = (int)alignment;
            return i >= 0 && i < s_AlignmentPivots.Length ? s_AlignmentPivots[i] : new Vector2(0.5f, 0.5f);
        }

        static SpriteAlignment Alignment(Vector2 pivot)
        {
            for (int i = 0; i < s_AlignmentPivots.Length; i++)
                if (Vector2.Distance(pivot, s_AlignmentPivots[i]) < 0.001f) return (SpriteAlignment)i;
            return SpriteAlignment.Custom;
        }
    }

    /// <summary>Gives the Sprite Editor the data provider for SVG files and their import settings.</summary>
    internal sealed class SVGSpriteEditorDataProviderFactory : ISpriteDataProviderFactory<SVGAsset>, ISpriteDataProviderFactory<SVGFileImporter>
    {
        public ISpriteEditorDataProvider CreateDataProvider(SVGAsset asset)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(asset)) as SVGFileImporter;
            return importer != null ? new SVGSpriteEditorDataProvider(importer) : null;
        }

        public ISpriteEditorDataProvider CreateDataProvider(SVGFileImporter importer)
        {
            return new SVGSpriteEditorDataProvider(importer);
        }
    }
}
