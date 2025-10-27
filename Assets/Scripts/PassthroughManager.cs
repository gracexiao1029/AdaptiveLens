using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassthroughManager : MonoBehaviour
{
    public OVRPassthroughLayer passthrough;
    public List<Gradient> colorMapGradient;
    public GameObject canvas;

    // Start is called before the first frame update
    void Start()
    {
        passthrough.textureOpacity = 1.0f;
        passthrough.colorMapEditorGradient = MakeNeutralGradient();
        passthrough.colorMapEditorBrightness = 0.0f;
        passthrough.colorMapEditorContrast = 0.0f;
        passthrough.colorMapEditorPosterize = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        // Handle button shortcuts
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            passthrough.hidden = !passthrough.hidden;

        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) ||
            OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            canvas.SetActive(!canvas.activeSelf);

        /*
        // Audio-based brightness control
        if (micClip == null || !Microphone.IsRecording(micDevice)) return;

        int micPos = Microphone.GetPosition(micDevice) - sampleLength + 1;
        if (micPos < 0) return;

        micClip.GetData(samples, micPos);

        // Loudness (RMS)
        float sum = 0f;
        for (int i = 0; i < sampleLength; i++)
            sum += samples[i] * samples[i];
        float rms = Mathf.Sqrt(sum / sampleLength);
        float targetBrightness = Mathf.Clamp01(rms * brightnessScale * 10f);
        currentBrightness = Mathf.Lerp(currentBrightness, targetBrightness, smoothSpeed);
        passthrough.colorMapEditorBrightness = currentBrightness;

        // Frequency analysis
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        int maxIndex = 0;
        float maxValue = 0f;
        for (int i = 0; i < spectrum.Length; i++)
        {
            if (spectrum[i] > maxValue)
            {
                maxValue = spectrum[i];
                maxIndex = i;
            }
        }

        // Convert bin index to frequency
        float freq = maxIndex * AudioSettings.outputSampleRate / 2f / spectrum.Length;
        dominantFrequency = Mathf.Lerp(dominantFrequency, freq, 0.2f);

        // Map frequency to normalized 0¨C1 range
        float t = Mathf.InverseLerp(minFreq, maxFreq, dominantFrequency);
        t = Mathf.Clamp01(t);

        // Smooth color transitions
        lastColorValue = Mathf.Lerp(lastColorValue, t, smoothSpeed);

        // Apply color mapping
        if (colorMapGradient.Count > 0)
        {
            // If one gradient, use it directly
            Gradient g = colorMapGradient[0];
            passthrough.colorMapEditorGradient = g;
            passthrough.edgeColor = g.Evaluate(lastColorValue).linear; // optional

            // if multiple gradients (e.g., blue¡úgreen¡úred), interpolate between them
            int idx = Mathf.FloorToInt(t * (colorMapGradient.Count - 1));
            passthrough.colorMapEditorGradient = colorMapGradient[idx];
        }
        */
    }

    // UI control methods
    public void SetOpacity(float value) => passthrough.textureOpacity = value;
    public void SetColorMapGradient(int index) => passthrough.colorMapEditorGradient = colorMapGradient[index];
    public void SetBrightness(float value) => passthrough.colorMapEditorBrightness = value;
    public void SetContrast(float value) => passthrough.colorMapEditorContrast = value;
    public void SetPosterize(float value) => passthrough.colorMapEditorPosterize = value;

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
}
