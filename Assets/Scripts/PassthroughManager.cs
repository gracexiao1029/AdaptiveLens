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
        
    }

    // Update is called once per frame
    void Update()
    {
        if(OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            passthrough.hidden = !passthrough.hidden;
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) ||
            OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            canvas.SetActive(!canvas.activeSelf);
        }
    }

    public void SetOpacity(float value)
    {
        passthrough.textureOpacity = value;
    }

    public void SetColorMapGradient(int index)
    {
        passthrough.colorMapEditorGradient = colorMapGradient[index];
    }

    public void SetBrightness(float value)
    {
        passthrough.colorMapEditorBrightness = value;
    }

    public void SetContrast(float value)
    {
        passthrough.colorMapEditorContrast = value;
    }

    public void SetPosterize(float value)
    {
        passthrough.colorMapEditorPosterize = value;
    }
}
