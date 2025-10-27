using System.Collections;
using UnityEngine;
using Unity.Barracuda;

/// <summary>
/// Records real-time audio, converts to mel spectrogram (crude version),
/// feeds ONNX emotion model via Barracuda, and maps valence to passthrough color.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EmotionAudioInput : MonoBehaviour
{
    public OVRPassthroughLayer passthrough;
    public NNModel emotionModelAsset;

    private Model runtimeModel;
    private IWorker worker;
    private AudioSource audioSource;

    private const int sampleSize = 4096; // read 4096 samples per frame
    private float[] samples = new float[sampleSize];
    private float sampleRate;

    private Color currentColor = Color.white;
    private float smoothColorSpeed = 4f;

    private const int N_MELS = 64;
    private const int TARGET_FRAMES = 1292; // matches model

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 5, 44100);
        audioSource.loop = true;
        while (!(Microphone.GetPosition(null) > 0)) { }
        audioSource.Play();
        sampleRate = AudioSettings.outputSampleRate;

        runtimeModel = ModelLoader.Load(emotionModelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

        passthrough.textureOpacity = 1.0f;
        passthrough.colorMapEditorGradient = MakeNeutralGradient();
        passthrough.colorMapEditorBrightness = 0.0f;
        passthrough.colorMapEditorContrast = 0.0f;
        passthrough.colorMapEditorPosterize = 0.0f;
    }

    void Update()
    {
        int micPos = Microphone.GetPosition(null);
        if (micPos < sampleSize) return;
        audioSource.GetOutputData(samples, 0);

        // Crude mel spectrogram placeholder
        float[] mel = new float[N_MELS * TARGET_FRAMES];
        for (int m = 0; m < N_MELS; m++)
        {
            for (int t = 0; t < TARGET_FRAMES; t++)
            {
                int src = Mathf.Clamp(t * sampleSize / TARGET_FRAMES, 0, sampleSize - 1);
                mel[m * TARGET_FRAMES + t] = Mathf.Abs(samples[src]) * 10f;
            }
        }

        // Feed into Barracuda
        using (var input = new Tensor(1, N_MELS, TARGET_FRAMES, 1, mel))
        {
            worker.Execute(input);
            using (var output = worker.PeekOutput())
            {
                float valence = 0f;
                float arousal = 0f;

                if (output.length >= 2)
                {
                    valence = output[0];
                    arousal = output[1];
                }

                Debug.Log($"Valence: {valence}, Arousal: {arousal}");

                // Normalize valence to 0–1 (DEAM range 1–9)
                valence = Mathf.InverseLerp(1f, 9f, valence);

                Color targetColor = ValenceToGradientColor(valence);
                currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * smoothColorSpeed);

                passthrough.colorMapEditorGradient = MakeTintedGradient(currentColor);
                passthrough.colorMapEditorBrightness = 0f;
                passthrough.colorMapEditorContrast = 0f;
            }
        }
    }

    Color ValenceToGradientColor(float valence)
    {
        // Detailed 1–9 mapping (valence normalized 0–1)
        if (valence < 0.025f)
            return Color.Lerp(new Color(0f, 0f, 0.8f), new Color(0f, 0.8f, 1f), valence / 0.125f);
        else if (valence < 0.05f)
            return Color.Lerp(new Color(0f, 0.8f, 1f), new Color(0.3f, 1f, 0.5f), (valence - 0.125f) / 0.125f);
        else if (valence < 0.075f)
            return Color.Lerp(new Color(0.3f, 1f, 0.5f), new Color(0.7f, 1f, 0.3f), (valence - 0.25f) / 0.125f);
        else if (valence < 0.10f)
            return Color.Lerp(new Color(0.7f, 1f, 0.3f), new Color(1f, 1f, 0.2f), (valence - 0.375f) / 0.125f);
        else if (valence < 0.15f)
            return Color.Lerp(new Color(1f, 1f, 0.2f), new Color(1f, 0.6f, 0f), (valence - 0.5f) / 0.125f);
        else if (valence < 0.20f)
            return Color.Lerp(new Color(1f, 0.6f, 0f), new Color(1f, 0.3f, 0.1f), (valence - 0.625f) / 0.125f);
        else if (valence < 0.25f)
            return Color.Lerp(new Color(1f, 0.3f, 0.1f), new Color(1f, 0f, 0f), (valence - 0.75f) / 0.125f);
        else
            return Color.Lerp(new Color(1f, 0f, 0f), new Color(1f, 0.4f, 0.6f), (valence - 0.875f) / 0.125f);
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

    Gradient MakeTintedGradient(Color c)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(c * 0.9f, 0.5f),
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

    void OnDestroy()
    {
        worker?.Dispose();
    }
}
