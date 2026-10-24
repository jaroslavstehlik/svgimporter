using UnityEngine;
using System.Collections;
using SVGImporter;

public class AudioColor : MonoBehaviour {

    public SVGRenderer target;
    public Color velocity;
    public bool affectAlpha = false;

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

    Color destination;

    void Awake()
    {
        destination = target.color;
    }

    public void OnAudio(float audioVelocity)
    {
        float finalVelocity = audioVelocity * velocityMultiplier * _velocityMultiplierIntensity;

        if(random && _randomIntensity >= 0.5f)
        {
            destination.r = Mathf.PerlinNoise(Time.realtimeSinceStartup * 1.5f, Time.realtimeSinceStartup * 3f) * velocity.r * finalVelocity;
            destination.g = Mathf.PerlinNoise(Time.realtimeSinceStartup * 2f, Time.realtimeSinceStartup * 0.5f) * velocity.g * finalVelocity;
            destination.b = Mathf.PerlinNoise(Time.realtimeSinceStartup * 0.2f, Time.realtimeSinceStartup * 0.15f) * velocity.b * finalVelocity;
            if(affectAlpha)
            {
                destination.a = Mathf.PerlinNoise(Time.realtimeSinceStartup * 2.3f, Time.realtimeSinceStartup * 3.5f) * velocity.a * finalVelocity;
            } else {
                destination.a = target.color.a;
            }
        } else {
            destination.r = velocity.r * finalVelocity;
            destination.g = velocity.g * finalVelocity;
            destination.b = velocity.b * finalVelocity;
            if(affectAlpha)
            {
                destination.a = velocity.a * finalVelocity;
            } else {
                destination.a = target.color.a;
            }
        }

        target.color = Color.Lerp(target.color, destination, Time.deltaTime * speed * _speedIntensity);
    }
}
