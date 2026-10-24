using System;
using System.Collections;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class AudioEvent : MonoBehaviour {

    [Range(0, 1)]
    public float spectrumStart = 0.5f;
    [Range(0, 1)]
    public float spectrumLength = 0.25f;
    [Range(0, 1)]
    public float stereoPan = 0.5f;
    public AnimationCurve spectrumFalloff = new AnimationCurve(new Keyframe[]{
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    });
    public float amplifier = 1f;

    [System.Serializable]
    public class TriggerEvent : UnityEvent<float> { }
    
    [FormerlySerializedAs("onAudio")]
    [SerializeField]
    protected TriggerEvent m_onAudio = new TriggerEvent();    
    public TriggerEvent onAudio
    {
        get { return m_onAudio; }
        set { m_onAudio = value; }
    }


    void Update()
    {
        int resolution = AudioSpectrum.Instance.resolution;
        int localStart = Mathf.Clamp(Mathf.RoundToInt(spectrumStart * resolution) - Mathf.RoundToInt(resolution * 0.5f), 0, resolution - 1);
        int localLength = Mathf.Clamp(localStart + Mathf.RoundToInt(resolution * spectrumLength), 0, resolution - 1);

        float audioOutput;
        if(stereoPan == 0f)
        {
            audioOutput = GetVelocity(AudioSpectrum.Instance.leftChannel, localStart, localLength, spectrumFalloff);
        } else if(stereoPan == 1f)
        {
            audioOutput = GetVelocity(AudioSpectrum.Instance.rightChannel, localStart, localLength, spectrumFalloff);
        } else {
            audioOutput = Mathf.Lerp(
                GetVelocity(AudioSpectrum.Instance.leftChannel, localStart, localLength, spectrumFalloff),
                GetVelocity(AudioSpectrum.Instance.leftChannel, localStart, localLength, spectrumFalloff),
                stereoPan
                );
        }

        onAudio.Invoke(audioOutput * amplifier);

    }

    float GetVelocity(float[] channel, int start, int end, AnimationCurve falloff)
    {
        if(start == end)
            return 0f;

        float output = 0f, progress = 0f;
        float distance = end - start;
        float index = 0;

        for(int i = start; i < end; i++)
        {
            progress = index / distance;
            output += channel[i] * falloff.Evaluate(progress);
            index ++;
        }


        return output / distance;
    }
}
