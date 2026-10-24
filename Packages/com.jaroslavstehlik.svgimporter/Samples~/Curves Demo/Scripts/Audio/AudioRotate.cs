using UnityEngine;
using System.Collections;

public class AudioRotate : MonoBehaviour {

    public Transform target;
    public Vector3 velocity;

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
            destination.y = Mathf.PerlinNoise(Time.realtimeSinceStartup * 0.5f, Time.realtimeSinceStartup) * velocity.y * finalVelocity;
            destination.z = Mathf.PerlinNoise(Time.realtimeSinceStartup * 0.3f, Time.realtimeSinceStartup * 2f) * velocity.z * finalVelocity;
        } else {
            destination.x = velocity.x * finalVelocity;
            destination.y = velocity.y * finalVelocity;
            destination.z = velocity.z * finalVelocity;
        }

        target.localRotation = Quaternion.Lerp(target.localRotation, Quaternion.Euler(destination), Time.deltaTime * speed * _speedIntensity);
    }
}
