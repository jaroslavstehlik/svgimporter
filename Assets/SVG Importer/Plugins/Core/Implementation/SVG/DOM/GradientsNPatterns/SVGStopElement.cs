// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;

namespace SVGImporter.Rendering
{
    using Document;
    using Utils;

    public class SVGStopElement
    {

        private float _offset;
        private SVGColor _stopColor;

        public float offset
        {
            get { return _offset; }
        }

        public SVGColor stopColor
        {
            get { return _stopColor; }
        }

        public SVGStopElement(AttributeList attrList)
        {
            string colorString = attrList.GetValue("stop-color");        
            string offsetString = attrList.GetValue("offset");
            string stopOpacity =  attrList.GetValue("stop-opacity");

            string styleValue = attrList.GetValue("style");
            if(styleValue != null)
            {
                string[] styleValues = styleValue.Split(';');
                for(int i = 0; i < styleValues.Length; i++)
                {
                    if(styleValues[i].Contains("stop-color"))
                    {
                        colorString = styleValues[i].Split(':')[1];
                    } else if(styleValues[i].Contains("stop-opacity"))
                    {
                        stopOpacity = styleValues[i].Split(':')[1];
                    } else if(styleValues[i].Contains("offset"))
                    {
                        offsetString = styleValues[i].Split(':')[1];
                    }
                }
            }

            if(colorString == null)
            {
                colorString = "black";
            }

            if(offsetString == null)
            {
                offsetString = "0%";
            }

            _stopColor = new SVGColor(colorString);

            if(!string.IsNullOrEmpty(stopOpacity))
            {
                if (stopOpacity.EndsWith("%"))
                {
                    _stopColor.color.a = float.Parse(stopOpacity.TrimEnd(new char[1] { '%' }), System.Globalization.CultureInfo.InvariantCulture) * 0.01f;
                } else {
                    _stopColor.color.a = float.Parse(stopOpacity, System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            string temp = offsetString.Trim();
            if (temp != "")
            {
                if (temp.EndsWith("%"))
                {
                    _offset = float.Parse(temp.TrimEnd(new char[1] { '%' }), System.Globalization.CultureInfo.InvariantCulture);
                } else
                {
                    _offset = float.Parse(temp, System.Globalization.CultureInfo.InvariantCulture) * 100;
                }
            }

//            Debug.Log("StopColor: "+_stopColor.color+", offset: "+_offset);
        }
    }
}
