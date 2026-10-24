using UnityEngine;
using System.Collections;

public class AudioSpectrum : MonoBehaviour {

    protected static AudioSpectrum _Instance;
    public static AudioSpectrum Instance
    {
        get {
            return _Instance;
        }
    }

    public AudioSource audioSource;
    public int resolution = 32;

    [HideInInspector]
    public float[] leftChannel;
    [HideInInspector]
    public float[] rightChannel;

    void Awake()
    {
        _Instance = this;
        leftChannel = new float[resolution];
        rightChannel = new float[resolution];
    }

	// Update is called once per frame
	void Update () {

        if(leftChannel == null || leftChannel.Length != resolution)
        {
            leftChannel = new float[resolution];
        }
        if(rightChannel == null || rightChannel.Length != resolution)
        {
            rightChannel = new float[resolution];
        }

        audioSource.GetSpectrumData(leftChannel, 0, FFTWindow.BlackmanHarris);
        audioSource.GetSpectrumData(rightChannel, 1, FFTWindow.BlackmanHarris);
	}
}
