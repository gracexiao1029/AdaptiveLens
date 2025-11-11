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

        passthrough.textureOpacity = 1.0f;
        passthrough.colorMapEditorGradient = MakeNeutralGradient();
        passthrough.colorMapEditorBrightness = 0.0f;
        passthrough.colorMapEditorContrast = 0.0f;
        passthrough.colorMapEditorPosterize = 0f;
    }

    void Update()
    {
        audioSource.GetOutputData(samples, 0);
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float freq = GetDominantFrequency();
        if (freq <= 0 || float.IsNaN(freq))
        {
            passthrough.colorMapEditorGradient = MakeNeutralGradient();
            passthrough.colorMapEditorBrightness = 0f;
            return;
        }

        // Get color and octave info
        Color targetColor;
        float octave;
        GetNoteColorAndOctave(freq, out targetColor, out octave);

        // Smoothly change color
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * smoothColorSpeed);

        // Brightness based on octave (octave 4 = 0 brightness)
        float brightness = Mathf.Lerp(-0.2f, 0.2f, Mathf.InverseLerp(2f, 6f, octave));

        // Apply to passthrough
        passthrough.colorMapEditorGradient = MakeTintedGradient(currentColor);
        passthrough.colorMapEditorBrightness = brightness;
        passthrough.colorMapEditorContrast = 0f;
        passthrough.colorMapEditorPosterize = 0f;
        passthrough.textureOpacity = 1.0f;
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

    // Get both color (A¨CG) and octave number
    void GetNoteColorAndOctave(float freq, out Color color, out float octave)
    {
        // Compute MIDI note number (A4 = 440 Hz)
        float noteNumber = 12f * Mathf.Log(freq / 440f, 2f) + 69f;
        int roundedNote = Mathf.RoundToInt(noteNumber) % 12;
        octave = Mathf.Floor(noteNumber / 12f) - 1f; // e.g. A4 ¡ú 4

        string noteName;
        switch (roundedNote)
        {
            case 9: noteName = "A"; break;
            case 11: noteName = "B"; break;
            case 0: noteName = "C"; break;
            case 2: noteName = "D"; break;
            case 4: noteName = "E"; break;
            case 5: noteName = "F"; break;
            case 7: noteName = "G"; break;
            default: noteName = "A"; break;
        }

        // Map note name ¡ú color
        switch (noteName)
        {
            case "A": color = new Color(0.59f, 0.29f, 0.0f); break;   // Brown
            case "B": color = Color.red; break;                       // Red
            case "C": color = new Color(1.0f, 0.55f, 0.0f); break;    // Orange
            case "D": color = Color.yellow; break;                    // Yellow
            case "E": color = Color.green; break;                     // Green
            case "F": color = Color.blue; break;                      // Blue
            case "G": color = new Color(0.56f, 0.0f, 1.0f); break;    // Violet
            default: color = Color.white; break;
        }
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
