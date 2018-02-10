// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

namespace SVGImporter.Rendering
{
    using Document;
    using Utils;

    public class SVGTransformable
    {
        private SVGTransformList _inheritTransformList;
        private SVGTransformList _currentTransformList;
        private SVGTransformList _summaryTransformList;

        public SVGTransformList inheritTransformList
        {
            get { return _inheritTransformList; }
            set
            {
                int c = 0;
                if (_inheritTransformList != null)
                    c += _inheritTransformList.Count;
                if (_currentTransformList != null)
                    c += _currentTransformList.Count;
                _inheritTransformList = value;
                _summaryTransformList = new SVGTransformList(c);
                if (_inheritTransformList != null)
                    _summaryTransformList.AppendItems(_inheritTransformList);
                if (_currentTransformList != null)
                    _summaryTransformList.AppendItems(_currentTransformList);
            }
        }

        public SVGTransformList currentTransformList
        {
            get { return _currentTransformList; }
            set
            {
                _currentTransformList = value;
                int c = 0;
                if (_inheritTransformList != null)
                    c += _inheritTransformList.Count;
                if (_currentTransformList != null)
                    c += _currentTransformList.Count;
                _summaryTransformList = new SVGTransformList(c);
                if (_inheritTransformList != null)
                    _summaryTransformList.AppendItems(_inheritTransformList);
                if (_currentTransformList != null)
                    _summaryTransformList.AppendItems(_currentTransformList);
            }
        }

        public SVGTransformList summaryTransformList
        {
            get { return _summaryTransformList; }
        }

        public float transformAngle
        {
            get
            {
                float _angle = 0.0f;
                for (int i = 0; i < _summaryTransformList.Count; i++)
                {
                    SVGTransform _temp = _summaryTransformList [i];
                    if (_temp.type == SVGTransformMode.Rotate)
                        _angle += _temp.angle;
                }
                return _angle;
            }
        }

        public SVGMatrix transformMatrix
        {
            get { return summaryTransformList.Consolidate().matrix; }
        }

        public SVGTransformable(SVGTransformList transformList)
        {
            inheritTransformList = transformList;
        }

        public static SVGMatrix GetRootViewBoxTransform(AttributeList attributeList, ref Rect viewport)
        {
            SVGMatrix matrix = SVGMatrix.identity;

            string attrXString = attributeList.GetValue("x");
            string attrYString = attributeList.GetValue("y");
            string attrWidthString = attributeList.GetValue("width");
            string attrHeightString = attributeList.GetValue("height");
            
            SVGLength   attrX = new SVGLength(SVGLengthType.PX, 0f), attrY = new SVGLength(SVGLengthType.PX, 0f), 
            attrWidth = new SVGLength(SVGLengthType.PX, 1f), attrHeight = new SVGLength(SVGLengthType.PX, 1f);
            
            if(!string.IsNullOrEmpty(attrXString))
            {
                attrX = new SVGLength(attrXString);
            }
            if(!string.IsNullOrEmpty(attrYString))
            {
                attrY = new SVGLength(attrYString);
            }
            if(!string.IsNullOrEmpty(attrWidthString))
            {
                attrWidth = new SVGLength(attrWidthString);
            }
            if(!string.IsNullOrEmpty(attrHeightString))
            {
                attrHeight = new SVGLength(attrHeightString);
            }
            
            string viewBox = attributeList.GetValue("viewBox");
            if (!string.IsNullOrEmpty(viewBox))
            {
                string[] _temp = SVGStringExtractor.ExtractTransformValue(viewBox);
                if (_temp.Length > 0)
                {
                    if(string.IsNullOrEmpty(attrXString))
                    {
                        attrX = new SVGLength(_temp [0]);
                    }
                }
                if (_temp.Length > 1)
                {
                    if(string.IsNullOrEmpty(attrYString))
                    {
                        attrY = new SVGLength(_temp [1]);
                    }
                }
                if (_temp.Length > 2)
                {
                    if(string.IsNullOrEmpty(attrWidthString))
                    {
                        attrWidth = new SVGLength(_temp [2]);
                    }
                }
                if (_temp.Length > 3)
                {
                    if(string.IsNullOrEmpty(attrHeightString))
                    {
                        attrHeight = new SVGLength(_temp [3]);
                    }
                }

                viewport = new Rect(attrX.value, attrY.value, attrWidth.value, attrHeight.value);                

                if(string.IsNullOrEmpty(attrXString))
                {        
                    viewport.x = attrX.value;
                }

                if(string.IsNullOrEmpty(attrYString))
                {        
                    viewport.y = attrY.value;
                }

                if(string.IsNullOrEmpty(attrWidthString))
                {        
                    viewport.width = attrWidth.value;
                }

                if(string.IsNullOrEmpty(attrHeightString))
                {        
                    viewport.height = attrHeight.value;
                }

            } else {
                viewport = new Rect(attrX.value, attrY.value, attrWidth.value, attrHeight.value);
            }

            return matrix;
        }

        public static SVGMatrix GetViewBoxTransform(AttributeList attributeList, ref Rect viewport, bool negotiate = false)
        {
            SVGMatrix matrix = SVGMatrix.identity;
            
            float x = 0.0f;
            float y = 0.0f;
            float w = 0.0f;
            float h = 0.0f;
            
            string preserveAspectRatio = attributeList.GetValue("preserveAspectRatio");
            string viewBox = attributeList.GetValue("viewBox");
            if (!string.IsNullOrEmpty(viewBox))
            {
                string[] viewBoxValues = SVGStringExtractor.ExtractTransformValue(viewBox);
                if(viewBoxValues.Length == 4)
                {
                    Rect contentRect = new Rect(
                        new SVGLength(viewBoxValues[0]).value,
                        new SVGLength(viewBoxValues[1]).value,
                        new SVGLength(viewBoxValues[2]).value,
                        new SVGLength(viewBoxValues[3]).value
                        );
                    
                    SVGViewport.Align align = SVGViewport.Align.xMidYMid;
                    SVGViewport.MeetOrSlice meetOrSlice = SVGViewport.MeetOrSlice.Meet;
                    
                    if(!string.IsNullOrEmpty(preserveAspectRatio))
                    {
                        string[] aspectRatioValues = SVGStringExtractor.ExtractStringArray(preserveAspectRatio);                        
                        align = SVGViewport.GetAlignFromStrings(aspectRatioValues);
                        meetOrSlice = SVGViewport.GetMeetOrSliceFromStrings(aspectRatioValues);
                    }
                    
                    Rect oldViewport = viewport;
                    viewport = SVGViewport.GetViewport(viewport, contentRect, align, meetOrSlice);
                    
                    float sizeX = 0f, sizeY = 0f;
                    if(oldViewport.size.x != 0f)
                        sizeX = viewport.size.x / oldViewport.size.x;
                    if(oldViewport.size.y != 0f)
                        sizeY = viewport.size.y / oldViewport.size.y;
                    
                    matrix.Scale(sizeX, sizeY);
                    matrix = matrix.Translate(viewport.x - oldViewport.x, viewport.y - oldViewport.y);
                }
            } else {
                if(negotiate) 
                {
                    string attrXString = attributeList.GetValue("x");
                    string attrYString = attributeList.GetValue("y");
                    string attrWidthString = attributeList.GetValue("width");
                    string attrHeightString = attributeList.GetValue("height");
                    
                    SVGLength   attrX = new SVGLength(SVGLengthType.PX, 0f), attrY = new SVGLength(SVGLengthType.PX, 0f), 
                    attrWidth = new SVGLength(SVGLengthType.PX, 1f), attrHeight = new SVGLength(SVGLengthType.PX, 1f);
                    
                    if(!string.IsNullOrEmpty(attrXString))
                    {
                        attrX = new SVGLength(attrXString);
                    }
                    if(!string.IsNullOrEmpty(attrYString))
                    {
                        attrY = new SVGLength(attrYString);
                    }
                    if(!string.IsNullOrEmpty(attrWidthString))
                    {
                        attrWidth = new SVGLength(attrWidthString);
                    }
                    if(!string.IsNullOrEmpty(attrHeightString))
                    {
                        attrHeight = new SVGLength(attrHeightString);
                    }
                    
                    
                    x = attrX.value;
                    y = attrY.value;
                    w = attrWidth.value;
                    h = attrHeight.value;
                    
                    float x_ratio = 1f;
                    if(w != 0f)
                        x_ratio = attrWidth.value / w;
                    
                    float y_ratio = 1f;
                    if(h != 0f)
                        y_ratio = attrHeight.value / h;
                    
                    matrix = matrix.Scale(x_ratio, y_ratio);
                    matrix = matrix.Translate(x, y);
                    viewport = new Rect(x, y, w, h);
                    
                    //                Debug.Log(string.Format("x: {0}, y: {1}, width: {2}, height: {3}, attrWidth: {4}, attrHeight: {5}", x, y, w, h, attrWidth, attrHeight));
                }
                //                Debug.Log(string.Format("x: {0}, y: {1}, width: {2}, height: {3}, attrWidth: {4}, attrHeight: {5}", x, y, w, h, attrWidth, attrHeight));
            }
            
            return matrix;
        }
    }
}
