// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// Leaves an object where it is when an SVG Opaque Sorting Group above it orders renderers by
    /// depth: its position along z is the object's own.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(SVGDocumentation.url)]
    [AddComponentMenu("Rendering/SVG Opaque Sort Override", 21)]
    public sealed class SVGOpaqueSortOverride : MonoBehaviour
    {
        /// <summary>Leaves the object's children where they are too.</summary>
        [Tooltip("Leaves the object's children where they are too.")]
        public bool includeChildren = false;

        // The group above, kept to re-sort it when the object leaves.
        SVGOpaqueSortingGroup _group;

        void OnEnable() { MarkGroupDirty(); }
        void OnDisable() { MarkGroupDirty(); }
        void OnValidate() { MarkGroupDirty(); }
        void OnTransformParentChanged() { MarkGroupDirty(); }
        void OnTransformChildrenChanged() { MarkGroupDirty(); }

        void MarkGroupDirty()
        {
            SVGOpaqueSortingGroup group = GetComponentInParent<SVGOpaqueSortingGroup>(true);
            if (_group != null && _group != group) _group.SetDirty();
            if (group != null) group.SetDirty();
            _group = group;
        }
    }
}
