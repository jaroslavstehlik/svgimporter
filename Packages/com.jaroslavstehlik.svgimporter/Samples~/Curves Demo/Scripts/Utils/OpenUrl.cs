using UnityEngine;
using System.Collections;

public class OpenUrl : MonoBehaviour {

	public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
}
