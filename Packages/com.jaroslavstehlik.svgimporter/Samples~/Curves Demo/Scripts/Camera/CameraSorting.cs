using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class CameraSorting : MonoBehaviour {

    public TransparencySortMode transparencySortMode;
    protected Camera cameraTarget;

    void OnEnable()
    {
        if(cameraTarget == null) cameraTarget = GetComponent<Camera>();
        cameraTarget.transparencySortMode = transparencySortMode;
    }

    void OnValidate()
    {
        if(cameraTarget == null) cameraTarget = GetComponent<Camera>();
        cameraTarget.transparencySortMode = transparencySortMode;
    }
}
