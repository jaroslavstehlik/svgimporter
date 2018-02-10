using UnityEngine;
using System.Collections;

using SVGImporter.Utils;

public class ClipPathTest : MonoBehaviour {

    public Rect viewport = new Rect(0f, 0f, 10f, 10f);
    public Rect content = new Rect(0f, 0f, 20f, 20f);
      
    public SVGViewport.MeetOrSlice viewportMeetOrSlice;
    public SVGViewport.Align viewportAlign;

    void OnDrawGizmos()
    {
        Rect finRect = SVGViewport.GetViewport(viewport, content, viewportAlign, viewportMeetOrSlice);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        DrawRect(viewport);
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        DrawRect(finRect);
    }

    void DrawRect(Rect rect)
    {
        Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin));
        Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMax, rect.yMax));

        Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMin, rect.yMax));
        Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, rect.yMax));

    }
}
