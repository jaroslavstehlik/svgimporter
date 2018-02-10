using UnityEngine;
using UnityEngine.UI;
using System.Collections;

using SVGImporter;

public class RealtimeImportDemo : MonoBehaviour {

    public SVGImage preview;
    public InputField svgInput;

    protected SVGAsset svgAsset;

    public void Load()
    {
        if(svgInput == null || string.IsNullOrEmpty(svgInput.text)) return;
        if(svgAsset != null)
        {
            Destroy(svgAsset);
        }

        svgAsset = SVGAsset.Load(svgInput.text);
        preview.vectorGraphics = svgAsset;
    }

}
