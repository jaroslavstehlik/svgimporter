// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// Orders the SVG Renderers below it by depth, in hierarchy order: each one is placed behind
    /// the previous one along z, by the previous asset's thickness plus depthOffset.
    ///
    /// For assets in the Opaque + Transparent format: they draw with the depth test, so Sorting
    /// Layer and Order in Layer (and Sorting Group) don't order them, their position along z does.
    /// Renderers with an SVG Opaque Sort Override are left where they are.
    ///
    /// The group sorts again only when something changes: its children, SVG Renderers and overrides
    /// below it being moved or having their children changed, an asset changing. In play mode,
    /// reordering the children of other objects below it isn't noticed: call Sort() after doing
    /// that. In the Editor every hierarchy change, undo and redo, and moving an object below the
    /// group by hand are noticed too, so renderers moved along z are put back.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(SVGDocumentation.url)]
    [AddComponentMenu("Rendering/SVG Opaque Sorting Group", 20)]
    public sealed class SVGOpaqueSortingGroup : MonoBehaviour
    {
        /// <summary>Distance along z between one asset and the next. Negative values stack towards -z.</summary>
        [Tooltip("Distance along z between one asset and the next. Negative values stack towards -z.")]
        public float depthOffset = 0.01f;

        bool _dirty = true;

        /// <summary>Places the renderers now. Happens by itself when the hierarchy or an asset changes.</summary>
        public void Sort()
        {
            float z = transform.position.z;
            Sort(transform, ref z);
            _dirty = false;
        }

        internal void SetDirty()
        {
            _dirty = true;
#if UNITY_EDITOR
            // In edit mode, updates run only when something asks for one.
            if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        internal bool isDirty
        {
            get { return _dirty; }
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            s_Groups.Add(this);
#endif
            SetDirty();
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            s_Groups.Remove(this);
#endif
        }

        void OnValidate()
        {
            SetDirty();
        }

        void OnTransformChildrenChanged()
        {
            SetDirty();
        }

        void LateUpdate()
        {
            if (_dirty) Sort();
        }

#if UNITY_EDITOR
        // The enabled groups, for the Editor events below.
        static readonly System.Collections.Generic.HashSet<SVGOpaqueSortingGroup> s_Groups = new System.Collections.Generic.HashSet<SVGOpaqueSortingGroup>();

        // What the Editor can change that the components don't hear about: any hierarchy change
        // (also below objects that aren't renderers), undo and redo, and objects moved by hand.
        [UnityEditor.InitializeOnLoadMethod]
        static void WatchEditor()
        {
            UnityEditor.EditorApplication.hierarchyChanged += SetAllDirty;
            UnityEditor.Undo.undoRedoPerformed += SetAllDirty;
            UnityEditor.Undo.postprocessModifications += OnModifications;
        }

        static void SetAllDirty()
        {
            foreach (SVGOpaqueSortingGroup group in s_Groups)
                if (group != null) group.SetDirty();
        }

        static UnityEditor.UndoPropertyModification[] OnModifications(UnityEditor.UndoPropertyModification[] modifications)
        {
            foreach (UnityEditor.UndoPropertyModification modification in modifications)
            {
                var moved = modification.currentValue != null ? modification.currentValue.target as Transform : null;
                if (moved == null) continue;
                SVGOpaqueSortingGroup group = moved.GetComponentInParent<SVGOpaqueSortingGroup>(true);
                if (group != null && group.transform != moved) group.SetDirty();
            }
            return modifications;
        }
#endif

        void Sort(Transform parent, ref float z)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                var sortOverride = child.GetComponent<SVGOpaqueSortOverride>();
                if (sortOverride != null && sortOverride.enabled)
                {
                    if (!sortOverride.includeChildren) Sort(child, ref z);
                    continue;
                }

                var renderer = child.GetComponent<SVGRenderer>();
                if (renderer != null && renderer.vectorGraphics != null)
                {
                    z += renderer.vectorGraphics.bounds.size.z * Mathf.Sign(depthOffset);
                    Vector3 position = child.position;
                    if (position.z != z)
                    {
                        position.z = z;
                        child.position = position;
                    }
                    z += depthOffset;
                }
                Sort(child, ref z);
            }
        }
    }
}
