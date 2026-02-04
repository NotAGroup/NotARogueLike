using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TrapDoor : MonoBehaviour
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
        if (!isEnabled) return;

        Debug.Log("Floor cleared, going to next level");
        RunData.Instance.LevelDone();
        GameSaver.save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
