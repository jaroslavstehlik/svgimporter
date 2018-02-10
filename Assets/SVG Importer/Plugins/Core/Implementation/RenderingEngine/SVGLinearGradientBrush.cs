// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SVGImporter.Rendering 
{
    using Utils;

    public class SVGLinearGradientBrush
    {
        private SVGLinearGradientElement _linearGradElement;
        //-----
        //Gradient Vector
        private SVGLength _x1, _y1, _x2, _y2;
        //-----
        private List<Color> _stopColorList;
        private List<float> _stopOffsetList;
        //-----
    //    private SVGSpreadMethod _spreadMethod;
    //    private Rect bounds;

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
        public SVGLinearGradientBrush(SVGLinearGradientElement linearGradElement)
        {
            _transform = SVGMatrix.identity;
            _linearGradElement = linearGradElement;
            Initialize();
            CreateFill();
        }

        public SVGLinearGradientBrush(SVGLinearGradientElement linearGradElement, Rect bounds, SVGMatrix matrix, Rect viewport)
        {
            _viewport = viewport;
            _transform = matrix;
            _linearGradElement = linearGradElement;
            Initialize();
            CreateFill(bounds);
        }

        /*********************************************************************************/
        private void Initialize()
        {
            _x1 = _linearGradElement.x1;
            _y1 = _linearGradElement.y1;
            _x2 = _linearGradElement.x2;
            _y2 = _linearGradElement.y2;

            _stopColorList = new List<Color>();
            _stopOffsetList = new List<float>();
    //        _spreadMethod = _linearGradElement.spreadMethod;

            GetStopList();
            /*
            _vitriOffset = 0;
            PreColorProcess(_vitriOffset);
            */
        }

        private void CreateFill()
        {        
            if(_alphaBlended)
            {
                _fill = new SVGFill(Color.white, FILL_BLEND.ALPHA_BLENDED, FILL_TYPE.GRADIENT, GRADIENT_TYPE.LINEAR);
            } else {
                _fill = new SVGFill(Color.white, FILL_BLEND.OPAQUE, FILL_TYPE.GRADIENT, GRADIENT_TYPE.LINEAR);
            }

            _gradientTransform = _linearGradElement.gradientTransform.Consolidate().matrix;
            _fill.gradientColors = SVGAssetImport.atlasData.AddGradient(ParseGradientColors());
            _fill.viewport = _viewport;
            /*
            _fill.transform = _transform;
            _fill.gradientStartX = _x1;
            _fill.gradientStartY = _y1;
            _fill.gradientEndX = _x2;
            _fill.gradientEndY = _y2;
            */
        }

        private void CreateFill(Rect bounds)
        {
            CreateFill();
            _fill.transform = SVGSimplePath.GetFillTransform(_fill, bounds, new SVGLength[]{_x1, _y1}, new SVGLength[]{_x2, _y2}, _transform, _gradientTransform);
        }

        public CCGradient ParseGradientColors()
        {
            int length = _stopColorList.Count;

            CCGradientColorKey[] colorKeys = new CCGradientColorKey[length];
            CCGradientAlphaKey[] alphaKeys = new CCGradientAlphaKey[length];
            /*
            string debugColor = "";
            for(int i = 0; i < length; i++)
            {
                debugColor += _stopColorList[i].ToString()+" ";
            }
            Debug.Log("GradientColors, count: "+length+" Colors: "+debugColor);
            */
            float currentStopOffset = 0f;
            
            for(int i = 0; i < length; i++)
            {
                currentStopOffset = Mathf.Clamp01(_stopOffsetList[i] * 0.01f);
                colorKeys[i] = new CCGradientColorKey(_stopColorList[i], currentStopOffset);
                alphaKeys[i] = new CCGradientAlphaKey(_stopColorList[i].a, currentStopOffset);
            }
            
            return new CCGradient(colorKeys, alphaKeys);
        }

        //-----
        private void GetStopList()
        {
            List<SVGStopElement> _stopList = _linearGradElement.stopList;
            int _length = _stopList.Count;
            if (_length == 0)
                return;

            _stopColorList.Add(GetColor(_stopList [0].stopColor));
            _stopOffsetList.Add(0f);

            for (int i = 0; i < _length; i++)
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

        protected Color GetColor(SVGColor svgColor)
        {
            if(svgColor.color.a != 1)
            {
                _alphaBlended = true;
            }
            return svgColor.color;
        }
    }
}
