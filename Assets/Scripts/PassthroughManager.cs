using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassthroughManager : MonoBehaviour
{
    public OVRPassthroughLayer passthrough;
    public List<Gradient> colorMapGradient;
    public GameObject canvas;

    // Audio variables
    private AudioClip micClip;
    private string micDevice;
    private const int sampleLength = 256;
    private float[] samples = new float[sampleLength];

    // Brightness control parameters
    [Range(0f, 2f)] public float brightnessScale = 1.0f; // multiplier
    [Range(0f, 1f)] public float smoothSpeed = 0.1f;     // smoothing factor
    private float currentBrightness = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize microphone
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 1, 44100);
            Debug.Log("Microphone started: " + micDevice);
        }
        else
        {
            Debug.LogWarning("No microphone detected!");
        }
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

        // Audio-based brightness control
        if (micClip != null && Microphone.IsRecording(micDevice))
        {
            int micPos = Microphone.GetPosition(micDevice) - sampleLength + 1;
            if (micPos < 0) return;

            micClip.GetData(samples, micPos);

            float sum = 0f;
            for (int i = 0; i < sampleLength; i++)
                sum += samples[i] * samples[i];

            float rms = Mathf.Sqrt(sum / sampleLength); // Root Mean Square amplitude
            float targetBrightness = Mathf.Clamp01(rms * brightnessScale * 10f); // adjust multiplier if needed

            // Smooth transition
            currentBrightness = Mathf.Lerp(currentBrightness, targetBrightness, smoothSpeed);

            passthrough.colorMapEditorBrightness = currentBrightness;
        }
    }

    // UI control methods
    public void SetOpacity(float value) => passthrough.textureOpacity = value;
    public void SetColorMapGradient(int index) => passthrough.colorMapEditorGradient = colorMapGradient[index];
    public void SetBrightness(float value) => passthrough.colorMapEditorBrightness = value;
    public void SetContrast(float value) => passthrough.colorMapEditorContrast = value;
    public void SetPosterize(float value) => passthrough.colorMapEditorPosterize = value;
}
