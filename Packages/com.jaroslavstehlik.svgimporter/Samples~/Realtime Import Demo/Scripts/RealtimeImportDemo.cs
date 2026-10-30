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

        // SVG Images draw assets imported for a Canvas.
        SVGImportOptions options = SVGImportOptions.Default;
        options.format = SVGAssetFormat.Canvas;
        string[] messages;
        if(!SVGAsset.TryLoad(svgInput.text, options, out svgAsset, out messages))
            Debug.LogWarning("Couldn't import the SVG: " + string.Join(" ", messages));
        preview.vectorGraphics = svgAsset;
    }

}
