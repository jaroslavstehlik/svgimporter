using UnityEngine;
using System.Collections;

public class AudioMove : MonoBehaviour {

    public Transform target;
    public Vector2 velocity;

    public float velocityMultiplier = 1f;
    protected float _velocityMultiplierIntensity = 1f;
    public void VelocityMultiplierIntensity(float value)
    {
        _velocityMultiplierIntensity = value;
    }

    public float speed = 1f;
    protected float _speedIntensity = 1f;
    public void SpeedIntensity(float value)
    {
        _speedIntensity = value;
    }

    public bool random = true;
    protected float _randomIntensity = 1f;
    public void RandomIntensity(float value)
    {
        _randomIntensity = value;
    }

    Vector3 destination;

    public void OnAudio(float audioVelocity)
    {
        float finalVelocity = audioVelocity * velocityMultiplier * _velocityMultiplierIntensity;

        if(random && _randomIntensity >= 0.5f)
        {
            destination.x = Mathf.PerlinNoise(Time.realtimeSinceStartup * 1.5f, Time.realtimeSinceStartup * 3f) * velocity.x * finalVelocity;
            destination.y = Mathf.PerlinNoise(Time.realtimeSinceStartup * 2f, Time.realtimeSinceStartup * 0.5f) * velocity.y * finalVelocity;
        } else {
            destination.x = velocity.x * finalVelocity;
            destination.y = velocity.y * finalVelocity;
        }
        target.localPosition = Vector3.Lerp(target.localPosition, destination, Time.deltaTime * speed * _speedIntensity);
    }
}
