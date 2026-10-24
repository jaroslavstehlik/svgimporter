using UnityEngine;
using System.Collections;
using SVGImporter;

public class AudioCameraZoom : MonoBehaviour {

    public Camera target;
    public float velocity;

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

    float destination;

    void Awake()
    {
        if(target.orthographic)
        {
            destination = target.orthographicSize;
        } else {
            destination = target.fieldOfView;
        }
    }

    public void OnAudio(float audioVelocity)
    {
        float finalVelocity = velocity * audioVelocity * velocityMultiplier * _velocityMultiplierIntensity;

        if(random && _randomIntensity >= 0.5f)
        {
            destination = Mathf.PerlinNoise(Time.realtimeSinceStartup * 1.5f, Time.realtimeSinceStartup * 3f) * finalVelocity;
        } else {
            destination = finalVelocity;
        }

        if(target.orthographic)
        {
            target.orthographicSize = Mathf.Lerp(target.orthographicSize, destination, Time.deltaTime * speed * _speedIntensity);
        } else {
            target.fieldOfView = Mathf.Lerp(target.fieldOfView, destination, Time.deltaTime * speed * _speedIntensity);
        }
    }
}
