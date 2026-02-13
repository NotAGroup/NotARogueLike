using UnityEngine;
using System;
using UnityEngine.InputSystem;

// class to handle ui state:
// can switch between gameplay/inventory/death-screen (/pause-screen)
public class UIManager : MonoBehaviour
{
    // ui elements (convenient accessors for other scripts)
    public GameObject playerStats { get; private set; }
    public GameObject hotbar { get; private set; }
    public GameObject inventoryUI { get; private set; }
    public GameObject upgradesUI { get; private set; }
    public GameObject deathScreen { get; private set; }
    public GameObject pauseScreen { get; private set; }

    public Pausing pauseState { get; private set; }

    public bool inventoryOpen { get => inventoryUI != null && inventoryUI.activeInHierarchy; }
    public bool upgradesOpen { get => upgradesUI != null && upgradesUI.activeInHierarchy; }
    public bool deathScreenOpen { get => deathScreen != null && deathScreen.activeInHierarchy; }

    // for configuring ui element visibility
    [System.Serializable]
    public struct UiElementConfig {
        public GameObject uiElement;
        public UIState visibleIn;
    }

    [Header("UI elements")]
    public UiElementConfig[] uiElements;
    [Header("Cursor")]
    public UIState cursorUnlocked;
    [Header("Gameplay Pause")]
    public UIState paused;

    // for configuring action map toggling
    [System.Serializable]
    public struct ActionMapConfig {
        public string name;
        public UIState enabledIn;

        [NonSerialized]
        public InputActionMap actionMap;
    }

    [System.Serializable]
    public struct ActionConfig {
        public string name;
        public UIState enabledIn;

        [NonSerialized]
        public InputAction action;
    }

    [Header("Input Actions/Action Maps")]
    public ActionMapConfig[] actionMaps;
    [Tooltip("Will be applied after toggling action maps")]
    public ActionConfig[] actions;

    // set of possible ui states
    [Flags]
    public enum UIState : int {
        None = 0,
        Gameplay = 1,
        Inventory = 4,
        Upgrades = 8,
        Pause = 64,
        Death = 128
    }
    public UIState currentState { get; private set; }


    // 
    public bool TryGetFocusedWindow(out GameObject gameObject) {
        switch (currentState) {
            case UIState.Inventory:
                gameObject = inventoryUI;
                return true;
            case UIState.Upgrades:
                gameObject = upgradesUI;
                return true;
            case UIState.Death:
                gameObject = deathScreen;
                return true;
            case UIState.Pause:
                gameObject = pauseScreen;
                return true;
            default: 
                gameObject = null;
                return false;
        }
    }

    public void SwitchToGameplay() {
        currentState = UIState.Gameplay;
        ApplyState();
    }

    public void SwitchToPause() {
        currentState = UIState.Pause;
        ApplyState();
    }

    public void SwitchToInventory() {
        currentState = UIState.Inventory;
        ApplyState();
    }

    public void SwitchToUpgrades() {
        currentState = UIState.Upgrades;
        ApplyState();
    }

    public void SwitchToDeathScreen() {
        currentState = UIState.Death;
        ApplyState();
    }

    public void SwitchToPauseScreen() {
        currentState = UIState.Pause;
        ApplyState();
    }

    public void RedrawCurrentUI()
    {
        for (int i = 0; i < uiElements.Length; i++) {
            uiElements[i].uiElement.SetActive(false);
        }

        ApplyState();
    }

    void Awake() {
        playerStats = GameObject.Find("Player Stats");
        hotbar = GameObject.Find("Hotbar");
        inventoryUI = GameObject.Find("Inventory");
        upgradesUI = GameObject.Find("Upgrades");
        deathScreen = GameObject.Find("Death Screen");
        pauseScreen = GameObject.Find("Pause Screen");

        pauseState = GetComponent<Pausing>();

        // get actions/maps from names
        for (int i = 0; i < actionMaps.Length; i++) {
            actionMaps[i].actionMap = InputSystem.actions.FindActionMap(actionMaps[i].name, true);
        }
        for (int i = 0; i < actions.Length; i++) {
            actions[i].action = InputSystem.actions.FindAction(actions[i].name, true);
        }

    }

    private void ApplyState() {
        UpdateVisibility();
        UpdateInputSystem();

        bool p = (paused & currentState) != 0;
        if (p != pauseState.isPaused)
        {
            if (p)
                pauseState.Pause();
            else
                pauseState.Resume();
        }

    }

    private void UpdateVisibility () {
        for (int i = 0; i < uiElements.Length; i++) {
            uiElements[i].uiElement.SetActive((uiElements[i].visibleIn & currentState) != UIState.None);
        }

        Cursor.lockState = ((cursorUnlocked & currentState) != UIState.None) ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
    }

    private void UpdateInputSystem() {
        for (int i = 0; i < actionMaps.Length; i++) {
            if ((actionMaps[i].enabledIn & currentState) != UIState.None) {
                actionMaps[i].actionMap.Enable();
            } else {
                actionMaps[i].actionMap.Disable();
            }
        }

        for (int i = 0; i < actions.Length; i++) {
            if ((actions[i].enabledIn & currentState) != UIState.None) {
                actions[i].action.Enable();
            } else {
                actions[i].action.Disable();
            }
        }
    }

}

