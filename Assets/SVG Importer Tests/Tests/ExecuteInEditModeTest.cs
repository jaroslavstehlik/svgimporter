using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class ExecuteInEditModeTest : MonoBehaviour {

    void Awake()
    {
        Debug.Log("Awake, isPlaying:" +Application.isPlaying);
        hideFlags = HideFlags.DontSave;
        gameObject.hideFlags = HideFlags.DontSave;
        //DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        Debug.Log("OnEnable, isPlaying:" +Application.isPlaying);
    }

	// Use this for initialization
	void Start () {
        Debug.Log("Start, isPlaying:" +Application.isPlaying);
	}
	
	// Update is called once per frame
	void Update () 
    {
        Debug.Log("Update, isPlaying:" +Application.isPlaying);
	}

    void LateUpdate () 
    {
        Debug.Log("LateUpdate, isPlaying:" +Application.isPlaying);
    }

    void OnDisable()
    {
        Debug.Log("OnDisable, isPlaying:" +Application.isPlaying);
    }

    void OnDestroy()
    {
        Debug.Log("OnDestroy, isPlaying:" +Application.isPlaying);
    }
}
