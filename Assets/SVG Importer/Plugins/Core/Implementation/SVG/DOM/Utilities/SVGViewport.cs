// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;

namespace SVGImporter.Utils
{
    public class SVGViewport 
    {
        const string None = "none";
        const string xMinYMin = "xminymin";
        const string xMidYMin = "xmidymin";       
        const string xMaxYMin = "xmaxymin";
        const string xMinYMid = "xminymid";
        const string xMidYMid = "xmidymid";
        const string xMaxYMid = "xmaxymid";
        const string xMinYMax = "xminymax";
        const string xMidYMax = "xmidymax";
        const string xMaxYMax = "xmaxymax";

        const string Meet = "meet";
        const string Slice = "slice";

        public enum Align
        {
            None,
            xMinYMin,
            xMidYMin,
            xMaxYMin,
            xMinYMid,
            xMidYMid,
            xMaxYMid,
            xMinYMax,
            xMidYMax,
            xMaxYMax
        }
        
        public enum MeetOrSlice
        {
            Meet, // Inside viewport
            Slice // Outside viewport
        }

        public static MeetOrSlice GetMeetOrSliceFromStrings(string[] inputStrings)
        {
            if(inputStrings == null || inputStrings.Length == 0)
                return MeetOrSlice.Meet;

            for(int i = 0; i < inputStrings.Length; i++)
            {
                if(string.IsNullOrEmpty(inputStrings[i]))
                    continue;

                switch(inputStrings[i].ToLower())
                {
                    case Meet:
                        return MeetOrSlice.Meet;
                    case Slice:
                        return MeetOrSlice.Slice;
                }
            }

            return MeetOrSlice.Meet;
        }

        public static MeetOrSlice GetMeetOrSliceFromString(string inputText)
        {
            if(string.IsNullOrEmpty(inputText))
                return MeetOrSlice.Meet;

            switch(inputText.ToLower())
            {
                case Meet:
                    return MeetOrSlice.Meet;
                case Slice:
                    return MeetOrSlice.Slice;
                    
            }

            return MeetOrSlice.Meet;
        }

        public static string GetStringFromMeetOrSlice(MeetOrSlice meetOrSlice)
        {
            switch(meetOrSlice)
            {
                case MeetOrSlice.Meet:
                    return Meet;
                case MeetOrSlice.Slice:
                    return Slice;
            }

            return Meet;
        }

        public static Align GetAlignFromStrings(string[] inputStrings)
        {
            if(inputStrings == null || inputStrings.Length == 0)
                return Align.xMidYMid;
            
            for(int i = 0; i < inputStrings.Length; i++)
            {
                if(string.IsNullOrEmpty(inputStrings[i]))
                    continue;
                
                switch(inputStrings[i].ToLower())
                {
                    case None:
                        return Align.None;
                    case xMinYMin:
                        return Align.xMinYMin;
                    case xMidYMin:
                        return Align.xMidYMin;
                    case xMaxYMin:
                        return Align.xMaxYMin;
                    case xMinYMid:
                        return Align.xMinYMid;
                    case xMidYMid:
                        return Align.xMidYMid;
                    case xMaxYMid:
                        return Align.xMaxYMid;
                    case xMinYMax:
                        return Align.xMinYMax;
                    case xMidYMax:
                        return Align.xMidYMax;
                    case xMaxYMax:
                        return Align.xMaxYMax;
                }
            }
            
            return Align.xMidYMid;
        }
        public static Align GetAlignFromString(string inputText)
        {
            if(string.IsNullOrEmpty(inputText))
                return Align.xMidYMid;

            switch(inputText.ToLower())
            {
                case None:
                    return Align.None;
                case xMinYMin:
                    return Align.xMinYMin;
                case xMidYMin:
                    return Align.xMidYMin;
                case xMaxYMin:
                    return Align.xMaxYMin;
                case xMinYMid:
                    return Align.xMinYMid;
                case xMidYMid:
                    return Align.xMidYMid;
                case xMaxYMid:
                    return Align.xMaxYMid;
                case xMinYMax:
                    return Align.xMinYMax;
                case xMidYMax:
                    return Align.xMidYMax;
                case xMaxYMax:
                    return Align.xMaxYMax;
            }

            return Align.xMidYMid;
        }

        public static string GetStringFromAlign(Align align)
        {
            switch(align)
            {
                case Align.None:
                    return None;
                case Align.xMinYMin:
                    return xMinYMin;
                case Align.xMidYMin:
                    return xMidYMin;
                case Align.xMaxYMin:
                    return xMaxYMin;      
                case Align.xMinYMid:
                    return xMinYMid;
                case Align.xMidYMid:
                    return xMidYMid;
                case Align.xMaxYMid:
                    return xMaxYMid;
                case Align.xMinYMax:
                    return xMinYMax;
                case Align.xMidYMax:
                    return xMidYMax;
                case Align.xMaxYMax:
                    return xMaxYMax;
            }

            return null;
        }

        public static Rect GetViewport(Rect viewport, Rect content, Align viewportAlign = Align.xMidYMid, MeetOrSlice viewportMeetOrSlice = MeetOrSlice.Meet)
        {
            viewport.x -= content.x;
            viewport.y -= content.y;

            if(viewportAlign != Align.None)
            {
                Vector2 sizeRatio = new Vector2(viewport.width / content.width, viewport.height / content.height);
                Vector2 size;
                Vector2 align;
                float scale;

                switch(viewportMeetOrSlice)
                {
                    case MeetOrSlice.Meet:
                        scale = Mathf.Min(sizeRatio.x, sizeRatio.y);
                        size.x = content.width * scale;
                        size.y = content.height * scale;
                        align = Getalign(viewport, size, viewportAlign);
                        return new Rect(align.x, align.y, size.x, size.y);
                    case MeetOrSlice.Slice:
                        scale = Mathf.Max(sizeRatio.x, sizeRatio.y);
                        size.x = content.width * scale;
                        size.y = content.height * scale;
                        align = Getalign(viewport, size, viewportAlign);
                        return new Rect(align.x, align.y, size.x, size.y);
                }
            }
            
            return viewport;
        }
        
        protected static Vector2 Getalign(Rect viewport, Vector2 size, Align align)
        {
            switch(align)
            {
                case Align.xMinYMin:
                    return new Vector2(viewport.x, viewport.y);
                case Align.xMidYMin:
                    return new Vector2(viewport.x + (viewport.width - size.x) * 0.5f, viewport.y);
                case Align.xMaxYMin:
                    return new Vector2(viewport.x + (viewport.width - size.x), viewport.y);
                case Align.xMinYMid:
                    return new Vector2(viewport.x, viewport.y + (viewport.height - size.y) * 0.5f);
                case Align.xMidYMid:
                    return new Vector2(viewport.x + (viewport.width - size.x) * 0.5f, viewport.y + (viewport.height - size.y) * 0.5f);
                case Align.xMaxYMid:
                    return new Vector2(viewport.x + (viewport.width - size.x), viewport.y + (viewport.height - size.y) * 0.5f);
                case Align.xMinYMax:
                    return new Vector2(viewport.x, viewport.y + (viewport.height - size.y));
                case Align.xMidYMax:
                    return new Vector2(viewport.x + (viewport.width - size.x) * 0.5f, viewport.y + (viewport.height - size.y));
                case Align.xMaxYMax:
                    return new Vector2(viewport.x + (viewport.width - size.x), viewport.y + (viewport.height - size.y));
            }
            
            return new Vector2(viewport.x, viewport.y);
        }
    }
}
