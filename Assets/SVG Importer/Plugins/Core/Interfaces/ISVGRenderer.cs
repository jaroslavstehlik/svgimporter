

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace SVGImporter
{
    public interface ISVGRenderer
    {
        System.Action<SVGLayer[], SVGAsset, bool> OnPrepareForRendering { get; set; }
        void UpdateRenderer();
        SVGAsset vectorGraphics { get; }

        int lastFrameChanged { get; }

        void AddModifier(ISVGModify modifier);
        void RemoveModifier(ISVGModify modifier);
        List<ISVGModify> modifiers { get; }
    }
}