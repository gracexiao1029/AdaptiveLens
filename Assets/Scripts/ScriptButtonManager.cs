using UnityEngine;
using UnityEngine.UI;

public class ScriptButtonManager : MonoBehaviour
{
    public Button[] buttons;       // Assign your 3 UI buttons here
    public GameObject[] objects;   // Assign your 3 GameObjects here in the same order

    void Start()
    {
        // Disable all GameObjects by default
        foreach (var obj in objects)
        {
            obj.SetActive(false);
        }

        // Add listeners to buttons
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i; // capture index for the listener
            buttons[i].onClick.AddListener(() => OnButtonClicked(index));
        }
    }

    void OnButtonClicked(int index)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].SetActive(i == index); // enable only the clicked button's GameObject
        }
        Debug.Log("Enabled: " + objects[index].name);
    }
}
