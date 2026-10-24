using UnityEngine;
using System.Collections;

public class HologramController : MonoBehaviour {

    [System.Serializable]
    public struct HologramLayer
    {
        public Transform transform;
        public Vector3 startLocalPosition;
        public float rotation;
    }

    public HologramLayer[] layers;
    public float depth;
    public float depthSpeed = 1f;
    public AnimationCurve depthAnimation;

    float elapsedTime;

    // Use this for initialization
    void Start ()
    {	
        for(int i = 0; i < layers.Length; i++)
        {
            if (layers[i].transform == null) continue;
            layers[i].startLocalPosition = layers[i].transform.localPosition;
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
        elapsedTime += Time.deltaTime * depthSpeed;
        depth = depthAnimation.Evaluate(Mathf.PingPong(elapsedTime, 1f));

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].transform == null) continue;
            Vector3 position = layers[i].transform.localPosition;
            position.z = Mathf.LerpUnclamped(0f, layers[i].startLocalPosition.z, depth);
            layers[i].transform.localPosition = position;
            layers[i].transform.localRotation *= Quaternion.Euler(0f, 0f, layers[i].rotation * Time.deltaTime);
        }
    }
}
