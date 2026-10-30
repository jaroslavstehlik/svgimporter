// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using UnityEngine;

namespace SVGImporter
{
    /// <summary>
    /// Leaves an object where it is when an SVG Opaque Sorting Group above it orders renderers by
    /// depth: its position along z is the object's own.
    /// </summary>
    [DisallowMultipleComponent]
    [HelpURL(SVGDocumentation.url)]
    [AddComponentMenu("Rendering/SVG Opaque Sort Override", 21)]
    public sealed class SVGOpaqueSortOverride : MonoBehaviour
    {
        /// <summary>Leaves the object's children where they are too.</summary>
        [Tooltip("Leaves the object's children where they are too.")]
        public bool includeChildren = false;

        void OnEnable() { MarkGroupDirty(); }
        void OnDisable() { MarkGroupDirty(); }
        void OnValidate() { MarkGroupDirty(); }

        void MarkGroupDirty()
        {
            SVGOpaqueSortingGroup group = GetComponentInParent<SVGOpaqueSortingGroup>();
            if (group != null) group.SetDirty();
        }
    }
}
