

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegMovetoAbs : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegMovetoAbs(float x, float y, SVGPathSeg segment) : base()
        {
    //        Debug.Log(string.Format("MovetoAbs: x: {0}, y: {1}",x, y));
            this._type = SVGPathSegTypes.MoveTo_Abs;
            if(segment != null)
            {
                _previousPoint = segment.currentPoint;
            } else {
                _previousPoint = _currentPoint;
            }
            _currentPoint = new Vector2(x, y);
        }   
    }
}
