

using UnityEngine;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegMovetoRel : SVGPathSeg
    {
        //================================================================================
        public SVGPathSegMovetoRel(float x, float y, SVGPathSeg segment) : base()
        {


            this._type = SVGPathSegTypes.MoveTo_Rel;
            if(segment != null)
                _previousPoint = segment.currentPoint;
            _currentPoint = _previousPoint + new Vector2(x, y);

//            Debug.Log("SVGPathSegMovetoRel: "+ new Vector2(x, y) +", currentPosition: "+_currentPoint+ ", LastPosition: "+_previousPoint);
        }
    }
}
