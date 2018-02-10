// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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