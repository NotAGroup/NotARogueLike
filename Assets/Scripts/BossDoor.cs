using UnityEngine;
using UnityEngine.InputSystem;

public class BossDoor : MonoBehaviour
{
    private bool isEnabled = false;

    public string closedInteractionText;
    public string openInteractionText;

    public void Enable() {
        isEnabled = true;
        GetComponent<InteractionHint>().Text = openInteractionText;
    }

    public void Interact()
    {
    }

    void Start() {
        GetComponent<InteractionHint>().Text = closedInteractionText;
    }


    void Update() 
    {
#if UNITY_EDITOR
        // For testing save/load
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            isEnabled = true;
            Interact();
        }
#endif
    }
}
