// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

namespace SVGImporter
{
    using Utils;

    [InitializeOnLoad]
    internal class SVGImporterLaunchEditor {

        static SVGImporterLaunchEditor() {
            RegisterDelegates();
        }

        static void RegisterDelegates()
        {
            // Unsubscribe first so a delegate is never registered twice.
            UnregisterDelegates();
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
            // Dropping SVG Assets into the Scene view and the Hierarchy goes through Unity's drop
            // handlers; Unity rejects drags that no handler accepts.
#if UNITY_6000_6_OR_NEWER
            DragAndDrop.AddDropHandlerV2(OnSceneDrop);
            DragAndDrop.AddDropHandlerV2(OnHierarchyDrop);
#else
            DragAndDrop.AddDropHandler(OnSceneDrop);
            DragAndDrop.AddDropHandler(OnHierarchyDrop);
#endif
            EditorApplication.update += Update;
        }

        static void Update()
        {
            // Only once something uses the atlas: projects without SVG content don't get one.
            if(!Application.isPlaying && SVGAtlas.exists && SVGAtlas.Instance.atlasHasChanged)
            {
                SVGAtlas.Instance.OnAtlasPreRender();
                SceneView.RepaintAll();
            }
        }

        static void UnregisterDelegates()
        {
            PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
#if UNITY_6000_6_OR_NEWER
            DragAndDrop.RemoveDropHandlerV2(OnSceneDrop);
            DragAndDrop.RemoveDropHandlerV2(OnHierarchyDrop);
#else
            DragAndDrop.RemoveDropHandler(OnSceneDrop);
            DragAndDrop.RemoveDropHandler(OnHierarchyDrop);
#endif
            EditorApplication.update -= Update;
        }

        /// <summary>Creates an SVG Renderer where SVG Assets are dropped in the Scene view.</summary>
        static DragAndDropVisualMode OnSceneDrop(Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform)
        {
            SVGAsset[] assets = GetSVGAssetsFromDraggedObjects();
            if (assets.Length == 0)
                return DragAndDropVisualMode.None;
            if (perform)
            {
                GameObject go = DropFramesToSceneToCreateGO(assets[0].name, assets, worldPosition);
                if (parentForDraggedObjects != null)
                    Undo.SetTransformParent(go.transform, parentForDraggedObjects, "Create SVG Renderer");
            }
            return DragAndDropVisualMode.Copy;
        }

#if UNITY_6000_6_OR_NEWER
        static DragAndDropVisualMode OnHierarchyDrop(EntityId dropTargetId, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            return OnHierarchyDrop(EditorUtility.EntityIdToObject(dropTargetId) as GameObject, dropMode, parentForDraggedObjects, perform);
        }
#else
        static DragAndDropVisualMode OnHierarchyDrop(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            return OnHierarchyDrop(EditorUtility.InstanceIDToObject(dropTargetInstanceID) as GameObject, dropMode, parentForDraggedObjects, perform);
        }
#endif

        /// <summary>
        /// Creates an SVG Renderer where SVG Assets are dropped in the Hierarchy: as a child of the
        /// object dropped upon, otherwise under the parent Unity suggests, or at the scene root.
        /// </summary>
        static DragAndDropVisualMode OnHierarchyDrop(GameObject target, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            SVGAsset[] assets = GetSVGAssetsFromDraggedObjects();
            if (assets.Length == 0)
                return DragAndDropVisualMode.None;
            if (perform)
            {
                Transform parent = (dropMode & HierarchyDropFlags.DropUpon) != 0 && target != null ? target.transform : parentForDraggedObjects;
                GameObject go = DropFramesToSceneToCreateGO(assets[0].name, assets, parent != null ? parent.position : GetDefaultInstantiatePosition());
                if (parent != null)
                {
                    Undo.SetTransformParent(go.transform, parent, "Create SVG Renderer");
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                }
            }
            return DragAndDropVisualMode.Copy;
        }

        public static GameObject DropFramesToSceneToCreateGO(string name, SVGAsset[] frames, Vector3 position)
        {
            if (frames.Length > 0)
            {
                SVGAsset asset = frames[0];
                GameObject gameObject = DropSVGAssetToSceneToCreateGO(asset, position);
                if (frames.Length > 1)
                {
                    SVGFrameAnimator svgFrameAnimator = gameObject.AddComponent<SVGFrameAnimator>();
                    svgFrameAnimator.frames = frames.Clone() as SVGAsset[];
                }
                Undo.RegisterCreatedObjectUndo(gameObject, "Create SVG Renderer");
                return gameObject;
            }
            return null;
        }

        private static Vector3 GetDefaultInstantiatePosition()
        {
            Vector3 result = Vector3.zero;
            if (SceneView.lastActiveSceneView)
            {
                if (SceneView.lastActiveSceneView.in2DMode)
                {
                    result = SceneView.lastActiveSceneView.camera.transform.position;
                    result.z = 0f;
                }
                else
                {
                    result = SceneView.lastActiveSceneView.pivot;
                }
            }
            return result;
        }
        public static SVGAsset[] GetSVGAssetsFromDraggedObjects()
        {
            List<SVGAsset> list = new List<SVGAsset>();
            UnityEngine.Object[] objectReferences = DragAndDrop.objectReferences;
            for (int i = 0; i < objectReferences.Length; i++)
            {
                UnityEngine.Object @object = objectReferences[i];
                if (@object != null && @object.GetType() == typeof(SVGAsset))
                {
                    list.Add(@object as SVGAsset);
                }
            }
            return list.ToArray();
        }

        public static GameObject DropSVGAssetToSceneToCreateGO(SVGAsset asset, Vector3 position)
        {
            GameObject go = new GameObject(asset.name);
            //Vector3 destination = Camera.current.ScreenToWorldPoint(new Vector3(mousePosition.x, scenePosition.height - mousePosition.y, 0f));
            //destination.z = 0f;
            go.transform.position = position;
            SVGRenderer renderer = go.AddComponent<SVGRenderer>();
            renderer.vectorGraphics = asset;
            if(asset.generateCollider) go.AddComponent<SVGCollider2D>();
            Selection.activeGameObject = go;
            return go;
        }

        static void OnPrefabInstanceUpdated(GameObject instance)
        {
            if(instance == null)
                return;

            SVGRenderer[] svgRenderers = instance.GetComponentsInChildren<SVGRenderer>();
            if(svgRenderers != null && svgRenderers.Length > 0)
            {
                for(int i = 0; i < svgRenderers.Length; i++)
                {
                    if(svgRenderers[i] == null)
                        continue;
                    svgRenderers[i].SetAllDirty();
                }
            }

            SVGImage[] svgImages = instance.GetComponentsInChildren<SVGImage>();
            if(svgImages != null && svgImages.Length > 0)
            {            
                for(int i = 0; i < svgImages.Length; i++)
                {
                    if(svgImages[i] == null)
                        continue;

                    svgImages[i].Refresh();
                    svgImages[i].SetAllDirty();
                }
            }
        }
    }
}
