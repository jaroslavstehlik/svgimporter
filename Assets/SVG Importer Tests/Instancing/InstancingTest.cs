using UnityEngine;
using System.Collections;
using SVGImporter;

public class InstancingTest : MonoBehaviour {

    public GameObject[] assetPrefabsOpaque;
    public GameObject[] assetPrefabsTransparent;
    public int totalInstances = 100;

    protected GameObject[] instances;

    public void TestTransparent()
    {
        if(instances != null)
        {
            for(int i = 0; i < totalInstances; i++)
            {
                if(instances[i] != null) DestroyImmediate(instances[i]);
            }
        } else {
            instances = new GameObject[totalInstances];
        }

        for(int i = 0; i < totalInstances; i++)
        {
            instances[i] = Instantiate(assetPrefabsTransparent[(int)Random.Range(0, assetPrefabsTransparent.Length - 1)], Random.insideUnitSphere * 10f, Quaternion.identity) as GameObject;
        }
    }

    public void TestOpaque()
    {
        if(instances != null)
        {
            for(int i = 0; i < totalInstances; i++)
            {
                if(instances[i] != null) DestroyImmediate(instances[i]);
            }
        } else {
            instances = new GameObject[totalInstances];
        }
        
        for(int i = 0; i < totalInstances; i++)
        {
            instances[i] = Instantiate(assetPrefabsOpaque[(int)Random.Range(0, assetPrefabsOpaque.Length - 1)], Random.insideUnitSphere * 10f, Quaternion.identity) as GameObject;
        }
    }

    public void TestMixed()
    {
        if(instances != null)
        {
            for(int i = 0; i < totalInstances; i++)
            {
                if(instances[i] != null) DestroyImmediate(instances[i]);
            }
        } else {
            instances = new GameObject[totalInstances];
        }
        
        for(int i = 0; i < totalInstances; i++)
        {
            if(Random.value > 0.5)
            {
                instances[i] = Instantiate(assetPrefabsOpaque[(int)Random.Range(0, assetPrefabsOpaque.Length - 1)], Random.insideUnitSphere * 10f, Quaternion.identity) as GameObject;
            } else {
                instances[i] = Instantiate(assetPrefabsTransparent[(int)Random.Range(0, assetPrefabsTransparent.Length - 1)], Random.insideUnitSphere * 10f, Quaternion.identity) as GameObject;
            }
        }
    }

    void OnGUI()
    {
        if(GUILayout.Button("Test Transparent"))
        {
            TestTransparent();
        }
        if(GUILayout.Button("Test Opaque"))
        {
            TestOpaque();
        }
        if(GUILayout.Button("Test Mixed"))
        {
            TestMixed();
        }
    }
}
