using UnityEngine;
using UnityEngine.InputSystem;

public class BossDoor : MonoBehaviour
{
    private bool isEnabled = false;

    public string closedInteractionText;
    public string openInteractionText;

    public Animator openAnimation;

    private bool open = false;

    public void Enable() {
        isEnabled = true;
        GetComponent<InteractionHint>().Text = openInteractionText;
    }

    public void Awake()
    {
    }

    public void Interact()
    { 
        if (!isEnabled) return;

        Debug.Log("Floor cleared, opening Boss Room");
        openAnimation.Play("Open",0,0.0f);
        open=true;
    }

    void Start() {
        GetComponent<InteractionHint>().Text = closedInteractionText;
    }

    void Update() 
    {
        AnimatorStateInfo stateInfo = openAnimation.GetCurrentAnimatorStateInfo(0);
        if(open && stateInfo.normalizedTime > 1f && stateInfo.IsName("Open"))
        {
            GetComponent<Collider>().enabled = false;
            Debug.Log("Door finish opening");
        }
            
    }
}
