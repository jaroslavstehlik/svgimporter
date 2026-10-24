using UnityEngine;
using System.Collections;

public class InstancerSpiral : MonoBehaviour {

    public Instancer instancer;

    public float outerRadius = 1f;
    protected float _outerRadiusIntensity = 1f;
    public void OuterRadiusIntensity(float value)
    {
        _outerRadiusIntensity = value;
    }

    public float innerRadius = 0f;
    protected float _innerRadiusIntensity = 1f;
    public void InnerRadiusIntensity(float value)
    {
        _innerRadiusIntensity = value;
    }

    public float space = 30f;
    protected float _spaceIntensity = 1f;
    public void SpaceIntensity(float value)
    {
        _spaceIntensity = value;
    }

    public float speed = 1f;
    protected float _speedIntensity = 1f;
    public void SpeedIntensity(float value)
    {
        _speedIntensity = value;
    }

    Vector3 destination;

	void Update() {	
        float deltaTime = Time.deltaTime * speed * _speedIntensity;
        float angleSpace, progress, spaceRad = space * Mathf.Deg2Rad * _spaceIntensity;
        float length = instancer.instances.Length;
        float distance;

        float finalOuterRadius = outerRadius * _outerRadiusIntensity;
        float finalInnerRadius = innerRadius * _innerRadiusIntensity;

        for(int i = 0; i < instancer.instances.Length; i++)
        {
            progress = i / length;
            angleSpace = i * spaceRad;
            distance = Mathf.Lerp(finalOuterRadius, finalInnerRadius, progress);

            destination.x = Mathf.Cos(angleSpace) * distance;
            destination.y = Mathf.Sin(angleSpace) * distance;

            instancer.instances[i].transform.localPosition = Vector3.Lerp(instancer.instances[i].transform.localPosition, destination, deltaTime);
        }

	}

}
