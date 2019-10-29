

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegLinetoAbs : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegLinetoAbs(float x, float y, SVGPathSeg segment) : base()
        {
    //        Debug.Log(string.Format("LinetoAbs: x: {0}, y: {1}",x, y));
            this._type = SVGPathSegTypes.LineTo_Abs;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = new Vector2(x, y);
        }
    }
}
