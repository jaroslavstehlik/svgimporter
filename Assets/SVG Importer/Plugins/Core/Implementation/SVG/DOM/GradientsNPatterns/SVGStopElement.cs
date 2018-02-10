// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
