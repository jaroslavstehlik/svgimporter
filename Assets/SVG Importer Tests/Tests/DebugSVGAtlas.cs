using UnityEngine;
using System.Collections;
using SVGImporter;

[ExecuteInEditMode]
public class DebugSVGAtlas : MonoBehaviour {

    public SVGAtlas atlas;

	void OnEnable()
    {
        atlas = SVGAtlas.Instance;
    }
}
