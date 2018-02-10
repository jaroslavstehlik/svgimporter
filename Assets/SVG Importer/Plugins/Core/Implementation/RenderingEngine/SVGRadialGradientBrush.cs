// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SVGImporter.Rendering 
{
    using Utils;

    public class SVGRadialGradientBrush
    {
        private SVGRadialGradientElement _radialGradElement;
        private SVGLength _cx, _cy, _r;
        private List<Color> _stopColorList;
        private List<float> _stopOffsetList;

        protected bool _alphaBlended = false;
        public bool alphaBlended
        {
            get {
                return _alphaBlended;
            }
        }

        protected SVGFill _fill;
        public SVGFill fill
        {
            get {
                return _fill;
            }
        }

        protected SVGMatrix _gradientTransform;
        protected SVGMatrix _transform;
        protected Rect _viewport;

        /*********************************************************************************/
        public SVGRadialGradientBrush(SVGRadialGradientElement radialGradElement)
        {
            _transform = SVGMatrix.identity;
            _radialGradElement = radialGradElement;
            Initialize();
            CreateFill();
        }

        public SVGRadialGradientBrush(SVGRadialGradientElement radialGradElement, Rect bounds, SVGMatrix matrix, Rect viewport)
        {
            _viewport = viewport;
            _transform = matrix;
            _radialGradElement = radialGradElement;
            Initialize();
            CreateFill(bounds);
        }
        
        protected Color GetColor(SVGColor svgColor)
        {
            if(svgColor.color.a != 1)
            {
                _alphaBlended = true;
            }
            return svgColor.color;
        }

        /*********************************************************************************/
        private void Initialize()
        {
            _cx = _radialGradElement.cx;
            _cy = _radialGradElement.cy;

            _r = _radialGradElement.r;
    //        _fx = _radialGradElement.fx.value;
    //        _fy = _radialGradElement.fy.value;

            _stopColorList = new List<Color>();
            _stopOffsetList = new List<float>();
    //        _spreadMethod = _radialGradElement.spreadMethod;

            GetStopList();
            /*
            FixF();
            _vitriOffset = 0;
            PreColorProcess(_vitriOffset);
            */
        }

        private void CreateFill()
        {                
            if(_alphaBlended)
            {
                _fill = new SVGFill(Color.white, FILL_BLEND.ALPHA_BLENDED, FILL_TYPE.GRADIENT, GRADIENT_TYPE.RADIAL);
            } else {
                _fill = new SVGFill(Color.white, FILL_BLEND.OPAQUE, FILL_TYPE.GRADIENT, GRADIENT_TYPE.RADIAL);
            }

            _gradientTransform = _radialGradElement.gradientTransform.Consolidate().matrix;
            _fill.gradientColors = SVGAssetImport.atlasData.AddGradient(ParseGradientColors());
            _fill.viewport = _viewport;
            /*
            _fill.transform = _transform;
            _fill.gradientStartX = _cx;
            _fill.gradientStartY = _cy;
            _fill.gradientEndX = _r;
            _fill.gradientEndY = _r;
            */
        }

        private void CreateFill(Rect bounds)
        {        
            CreateFill();
            _fill.transform = SVGSimplePath.GetFillTransform(_fill, bounds, new SVGLength[]{_cx, _cy}, new SVGLength[]{_r, _r}, _transform, _gradientTransform);
        }
        
        public CCGradient ParseGradientColors()
        {
            int length = _stopColorList.Count;
            CCGradientColorKey[] colorKeys = new CCGradientColorKey[length];
            CCGradientAlphaKey[] alphaKeys = new CCGradientAlphaKey[length];
           
            float currentStopOffset = 0f;

            for(int i = 0; i < length; i++)
            {
                currentStopOffset = Mathf.Clamp01(_stopOffsetList[i] * 0.01f);
                colorKeys[i] = new CCGradientColorKey(_stopColorList[i], currentStopOffset);
                alphaKeys[i] = new CCGradientAlphaKey(_stopColorList[i].a, currentStopOffset);
            }
            
            return new CCGradient(colorKeys, alphaKeys);
        }

        private void GetStopList()
        {
            List<SVGStopElement> _stopList = _radialGradElement.stopList;
            int _length = _stopList.Count;
            if (_length == 0)
                return;

            _stopColorList.Add(GetColor(_stopList [0].stopColor));
            _stopOffsetList.Add(0f);
            int i = 0;
            for (i = 0; i < _length; i++)
            {
                float t_offset = _stopList [i].offset;
                if ((t_offset > _stopOffsetList [_stopOffsetList.Count - 1]) && (t_offset <= 100f))
                {
                    _stopColorList.Add(GetColor(_stopList [i].stopColor));
                    _stopOffsetList.Add(t_offset);
                } else if (t_offset == _stopOffsetList [_stopOffsetList.Count - 1])
                    _stopColorList [_stopOffsetList.Count - 1] = GetColor(_stopList [i].stopColor);
            }

            if (_stopOffsetList [_stopOffsetList.Count - 1] != 100f)
            {
                _stopColorList.Add(_stopColorList [_stopOffsetList.Count - 1]);
                _stopOffsetList.Add(100f);
            }
        }

    }
}
