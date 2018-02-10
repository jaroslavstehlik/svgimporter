using UnityEngine;
using System.Collections;

public class changescene : MonoBehaviour {

    public int scene;

	void OnMouseDown()
    {
        Application.LoadLevel(scene);
    }
}
