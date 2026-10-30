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
    /// In play mode the group sorts again when its children change, and when SVG Renderers and
    /// overrides below it are moved, change their own children or change asset. Reordering the
    /// children of other objects below it isn't noticed: call Sort() after doing that. In edit mode
    /// it checks every frame.
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
        }

        internal bool isDirty
        {
            get { return _dirty; }
        }

        void OnEnable()
        {
            _dirty = true;
        }

        void OnValidate()
        {
            _dirty = true;
        }

        void OnTransformChildrenChanged()
        {
            _dirty = true;
        }

        void LateUpdate()
        {
            // In edit mode objects can be reordered anywhere below the group: check every frame.
            // Positions are written only when they change.
            if (_dirty || !Application.isPlaying) Sort();
        }

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
