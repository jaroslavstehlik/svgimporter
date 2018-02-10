using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionTest : MonoBehaviour
{
    public Transform testPosition;
    public Vector3 pointA;
    public Vector3 pointB;
    public Vector3 pointC;
    public Vector3 pointD;

    private void OnDrawGizmos()
    {
        Matrix4x4 gizmoMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawLine(pointA, pointB);
        Gizmos.DrawLine(pointB, pointD);
        Gizmos.DrawLine(pointC, pointD);
        Gizmos.DrawLine(pointA, pointC);

        Vector3 ABmid = pointA + (pointB - pointA) * 0.5f;
        Vector3 CDmid = pointC + (pointD - pointC) * 0.5f;
        Vector3 CDmidABmid = CDmid - ABmid;

        if (testPosition != null)
        {            
            Vector3 localPos = transform.InverseTransformPoint(testPosition.position);
            Gizmos.DrawWireSphere(GetClosestPointOnLineSegment(ABmid, CDmid, localPos), 0.02f);
            Gizmos.DrawWireSphere(GetClosestPointOnLineSegment(pointB, pointD, localPos), 0.02f);
            Gizmos.DrawWireSphere(GetClosestPointOnLineSegment(pointA, pointC, localPos), 0.02f);
        }

        Gizmos.DrawLine(ABmid, CDmid);
        Gizmos.matrix = gizmoMatrix;
    }

    public static Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P)
    {
        Vector2 AP = P - A;       //Vector from A to P   
        Vector2 AB = B - A;       //Vector from A to B  

        float magnitudeAB = AB.SqrMagnitude();     //Magnitude of AB vector (it's length squared)     
        float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
        float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

        if (distance < 0)     //Check if P projection is over vectorAB     
        {
            return A;

        }
        else if (distance > 1)
        {
            return B;
        }
        else
        {
            return A + AB * distance;
        }
    }
}
