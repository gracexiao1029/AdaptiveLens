using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioInput : MonoBehaviour
{
    public OVRPassthroughLayer passthrough;
    private AudioSource audioSource;
    private const int sampleSize = 1024;
    private float[] samples = new float[sampleSize];
    private float[] spectrum = new float[sampleSize];
    private float sampleRate;

    private Color currentColor = Color.white;
    private float smoothColorSpeed = 6f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 1, 44100);
        audioSource.loop = true;
        while (!(Microphone.GetPosition(null) > 0)) { }
        audioSource.Play();
        sampleRate = AudioSettings.outputSampleRate;

        passthrough.colorMapEditorBrightness = 0f;
        passthrough.colorMapEditorContrast = 0f;
        passthrough.colorMapEditorGradient = MakeNeutralGradient();
    }

    void Update()
    {
        audioSource.GetOutputData(samples, 0);
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float freq = GetDominantFrequency();
        if (freq <= 0 || float.IsNaN(freq))
        {
            passthrough.colorMapEditorGradient = MakeNeutralGradient();
            return;
        }

        float hue;
        float octaveSmooth;
        float semitone;
        GetHueOctaveSemitone(freq, out hue, out octaveSmooth, out semitone);

        // Lightness: blend octave + pitch position (higher notes are brighter)
        float pitchPos = semitone / 12f; // 0¨C1 across the octave
        float l = Mathf.Lerp(0.35f, 0.8f, Mathf.InverseLerp(2f, 6f, octaveSmooth));
        l += (pitchPos - 0.5f) * 0.15f; // brighten high notes like So/La/Ti a bit
        l = Mathf.Clamp01(l);

        // Convert HSL ¡ú RGB
        Color targetColor = ColorFromHSL(hue, 0.9f, l);
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * smoothColorSpeed);

        passthrough.colorMapEditorGradient = MakeTintedGradient(currentColor);
        passthrough.colorMapEditorBrightness = 0f;
        passthrough.colorMapEditorContrast = 0f;
    }

    float GetDominantFrequency()
    {
        float maxV = 0;
        int maxN = 0;
        for (int i = 0; i < sampleSize; i++)
        {
            float freq = i * (sampleRate / 2f) / sampleSize;
            float weight = Mathf.Clamp01(freq / 4000f);
            float value = spectrum[i] * (0.3f + weight);
            if (value > maxV)
            {
                maxV = value;
                maxN = i;
            }
        }
        return maxN * (sampleRate / 2f) / sampleSize;
    }

    // Map frequency to hue, octave, and semitone
    void GetHueOctaveSemitone(float freq, out float hue, out float octave, out float semitone)
    {
        hue = 0f;
        octave = 4f;
        semitone = 0f;
        if (freq <= 0) return;

        float log_val = Mathf.Log(freq / 16.35f, 2f); // C0 = 16.35 Hz
        octave = Mathf.Clamp(log_val, 2f, 6f);
        semitone = (log_val - Mathf.Floor(log_val)) * 12f;

        hue = Mathf.Repeat(semitone * (360f / 12f), 360f);
    }

    // HSL to RGB
    Color ColorFromHSL(float h, float s, float l)
    {
        h /= 360f;
        float r = l, g = l, b = l;
        if (s != 0)
        {
            float temp2 = (l < 0.5f) ? l * (1f + s) : l + s - l * s;
            float temp1 = 2f * l - temp2;
            r = GetColorComponent(temp1, temp2, h + 1f / 3f);
            g = GetColorComponent(temp1, temp2, h);
            b = GetColorComponent(temp1, temp2, h - 1f / 3f);
        }
        return new Color(r, g, b);
    }

    float GetColorComponent(float t1, float t2, float t3)
    {
        if (t3 < 0) t3 += 1;
        if (t3 > 1) t3 -= 1;
        if (6 * t3 < 1) return t1 + (t2 - t1) * 6 * t3;
        if (2 * t3 < 1) return t2;
        if (3 * t3 < 2) return t1 + (t2 - t1) * ((2f / 3f) - t3) * 6f;
        return t1;
    }

    Gradient MakeNeutralGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }

    Gradient MakeTintedGradient(Color noteColor)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(noteColor * 0.9f, 0.5f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }
}
