using UnityEngine;
using UnityEngine.InputSystem;

public class UserInputUI : MonoBehaviour
{
    public UINavigationReceiver receiver;

    private InputAction uiSelectAction;
    private InputAction uiMoveAction;
    
    //
    private float uiNavigateTimer;
    [Tooltip("How fast the ui should be navigated (in Hz)")]
    public float uiNavigateRepeatRate;

    void Start()
    {
        FindActions();
    }

    void FindActions() 
    {
		uiMoveAction = InputSystem.actions.FindAction("Navigate", true);
		uiSelectAction = InputSystem.actions.FindAction("Submit", true);
    }

    // Update is called once per frame
    void Update()
    {
        uiNavigateTimer -= Time.deltaTime;

        Vector2 uiDirection = uiMoveAction.ReadValue<Vector2>();
        if (uiDirection != Vector2.zero && uiNavigateTimer <= 0f) 
        {
            receiver.Navigate(uiDirection);
            uiNavigateTimer = 1f / uiNavigateRepeatRate;
        }

        if (uiDirection == Vector2.zero)
            uiNavigateTimer = 0f;


        if (uiSelectAction.WasPerformedThisFrame()) {
            // control active ui
            receiver.Submit();
        }
    }
}
