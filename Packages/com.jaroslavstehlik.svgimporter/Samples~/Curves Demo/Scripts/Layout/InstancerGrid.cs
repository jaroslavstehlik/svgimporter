using UnityEngine;
using System.Collections;

public class InstancerGrid : MonoBehaviour {

    public Instancer instancer;

    public int grid = 3;
    protected float _gridIntensity = 1f;
    public void GridIntensity(float value)
    {
        _gridIntensity = value;
    }

    public float space = 1f;
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

    public bool horizontal = true;
    protected float _horizontalIntensity = 1f;
    public void HorizontalIntensity(float value)
    {
        _horizontalIntensity = value;
    }

    public bool square = false;
    protected float _squareIntensity = 1f;
    public void SquareIntensity(float value)
    {
        _squareIntensity = value;
    }

    Vector3 destination;

	void Update() {
	
        float length = instancer.instances.Length;
        int finalGrid = Mathf.RoundToInt(grid * _gridIntensity);
        bool finalSquare = square && _squareIntensity >= 0.5f;

        if(finalSquare)
            finalGrid = Mathf.RoundToInt(Mathf.Sqrt(length));

        if(finalGrid < 1)
            finalGrid = 1;

        float finalSpace = space * _spaceIntensity;

        float deltaTime = Time.deltaTime * speed * _speedIntensity;
        float halfGrid = (finalGrid - 1) * 0.5f;
        float halfSize = halfGrid * finalSpace;

        float row, collumn;
        bool finalHorizontal = horizontal && _horizontalIntensity >= 0.5f;

        for(int i = 0; i < length; i++)
        {
            if(finalHorizontal)
            {
                collumn = i % finalGrid;
                row = Mathf.Floor(i / finalGrid);
            } else {
                row = i % finalGrid;
                collumn = Mathf.Floor(i / finalGrid);
            }

            destination.x = -halfSize + row * finalSpace;
            destination.y = -halfSize + collumn * finalSpace;

            instancer.instances[i].transform.localPosition = Vector3.Lerp(instancer.instances[i].transform.localPosition, destination, deltaTime);
        }

	}

}
