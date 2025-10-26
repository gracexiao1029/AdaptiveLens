using System.Collections;
using UnityEngine;
using Unity.Barracuda;

/// <summary>
/// Captures real-time Quest 2 microphone input,
/// runs emotion inference using a Barracuda ONNX model (valence/arousal),
/// and maps the results to passthrough color gradients.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EmotionAudioInput : MonoBehaviour
{
    public OVRPassthroughLayer passthrough;
    public NNModel emotionModelAsset;   // drag your emotion_model.onnx (imported as NNModel)
    private Model runtimeModel;
    private IWorker worker;

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

        // Initialize Model
        runtimeModel = ModelLoader.Load(emotionModelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

        passthrough.colorMapEditorBrightness = 0f;
        passthrough.colorMapEditorContrast = 0f;
        passthrough.colorMapEditorGradient = MakeNeutralGradient();
    }

    void Update()
    {
        // === 4️⃣ Capture live mic samples ===
        audioSource.GetOutputData(samples, 0);
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float freq = GetDominantFrequency();
        if (freq <= 0 || float.IsNaN(freq))
        {
            passthrough.colorMapEditorGradient = MakeNeutralGradient();
            return;
        }

        // === 5️⃣ Prepare simple mel-like input tensor ===
        float[] melInput = new float[64 * 128];
        for (int i = 0; i < melInput.Length && i < spectrum.Length; i++)
            melInput[i] = spectrum[i] * 10f;

        Tensor input = new Tensor(1, 64, 128, 1, melInput);

        // === 6️⃣ Run ONNX model ===
        worker.Execute(input);
        Tensor output = worker.PeekOutput();

        // Expect 1 value for valence (0 = cold/negative, 1 = warm/positive)
        float valence = Mathf.Clamp01(output[0]);

        // === 7️⃣ Map valence → color ===
        // low valence (cold) → blue, high valence → red
        Color targetColor = Color.Lerp(Color.blue, Color.red, valence);
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * smoothColorSpeed);

        // === 8️⃣ Apply to passthrough ===
        passthrough.colorMapEditorGradient = MakeTintedGradient(currentColor);
        passthrough.colorMapEditorBrightness = 0f;
        passthrough.colorMapEditorContrast = 0f;

        input.Dispose();
        output.Dispose();
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
}
