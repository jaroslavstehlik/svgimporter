using UnityEngine;
using System.Collections;

public class Instancer : MonoBehaviour {

    public Instanced[] prefabs;
    public int count = 10;
    public bool pickRandom = false;
    public AnimationCurve updateFallof = new AnimationCurve(new Keyframe[]{
        new Keyframe(0f, 1f), new Keyframe(1f, 1f)
    });

    [HideInInspector]
    public Instanced[] instances;

    int lastInstanceIndex;

    void OnEnable()
    {
        instances = new Instanced[count];
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        GameObject go;
        for(int i = 0; i < count; i++)
        {
            go = (GameObject)Instantiate(GetInstance(), position, rotation);
            go.transform.SetParent(transform);
            instances[i] = go.GetComponent<Instanced>();
        }

        lastInstanceIndex = 0;
    }

    GameObject GetInstance()
    {
        if(prefabs == null || prefabs.Length == 0)
            return null;

        if(prefabs.Length == 1)
        {
            return prefabs[0].gameObject;
        }

        Instanced prefab = null;


        if(pickRandom)
        {
            prefab =  prefabs[Mathf.RoundToInt(Random.value * (prefabs.Length - 1))];
        } else {
            prefab = prefabs[lastInstanceIndex];
            lastInstanceIndex = Mathf.RoundToInt(Mathf.Repeat(lastInstanceIndex + 1, prefabs.Length - 1));
        }

        return prefab.gameObject;
    }

    void OnDisable()
    {
        if(instances != null)
        {
            for(int i = 0; i < instances.Length; i++)
            {
                if(instances[i] == null)
                    continue;

                Destroy(instances[i].gameObject);
            }
        }

        instances = null;
    }

    public void OnUpdate(float value)
    {
        if(instances != null)
        {
            float instancesLength = instances.Length;
            float progress;
            for(int i = 0; i < instancesLength; i++)
            {
                progress = i / instancesLength;
                instances[i].onUpdate.Invoke(value * updateFallof.Evaluate(progress));
            }
        }
    }
}
