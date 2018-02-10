// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

namespace SVGImporter.Rendering
{
    public abstract class SVGPathSegCurvetoQuadratic : SVGPathSeg
    {
        public abstract Vector2 controlPoint1 { get; }
    }
}
