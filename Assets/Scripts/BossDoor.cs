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
        GetComponent<Collider>().enabled = false;
    }

    void Start() {
        GetComponent<InteractionHint>().Text = closedInteractionText;
    }
}
