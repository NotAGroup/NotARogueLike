using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class UserInput : MonoBehaviour
{
    private Player player;
    private PlayerUpgrades playerUpgrades;
    private UIManager uiController;
    private InventoryUI inventoryUI;
    private UpgradeUI upgradeUI;

    private Vector2 direction;
    private Vector2 rotation;

    private bool controlPlayer {
        get => !uiController.upgradesOpen && !uiController.inventoryOpen && !player.isDead;
    }

    // movement actions
    private InputAction moveAction;
    private InputAction rotateAction;

    // movement modifiers
    private InputAction sneakAction;
    private InputAction sprintAction;
    private InputAction slideAction;
    private InputAction jumpAction;

    private InputAction interactAction;

    // combat
    private InputAction changeAttackAction;
    private InputAction attackAction;
    private InputAction strikeAction;
    private InputAction aimAction;
    private InputAction shootAction;

    // *-state -> gameplay, gameplay -> inventory
    private InputAction toggleUIAction;
    // inventory <-> upgrades
    private InputAction switchUIAction;

    // item usage action
    private InputAction item1Action;
    private InputAction item2Action;
    private InputAction item3Action;
    private InputAction item4Action;

    // any state -> selected-ui -> gameplay
    private InputAction toggleUpgradesAction;
    private InputAction toggleInventoryAction;
    private InputAction togglePauseAction;

    // ui actions
    private InputAction uiSelectAction;
    private InputAction uiMoveAction;
    private InputAction uiCloseAction;

    public float lookSensitivity = 1f;

    //
    private float uiNavigateTimer;
    [Tooltip("How fast the ui should be navigated (in Hz)")]
    public float uiNavigateRepeatRate;

    void Awake()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        playerUpgrades = GameObject.Find("Player").GetComponent<PlayerUpgrades>();

        uiController = GetComponent<UIManager>();
        inventoryUI = uiController.inventoryUI.GetComponent<InventoryUI>();
        upgradeUI = uiController.upgradesUI.GetComponent<UpgradeUI>();

        FindActions();
    }

    void FindActions() 
    {
        // find input actions
		moveAction = InputSystem.actions.FindAction("Move", true);
		rotateAction = InputSystem.actions.FindAction("Look", true);

		jumpAction = InputSystem.actions.FindAction("Jump", true);
		sneakAction = InputSystem.actions.FindAction("Crouch", true);
		sprintAction = InputSystem.actions.FindAction("Sprint", true);
		slideAction = InputSystem.actions.FindAction("Slide", true);
		interactAction = InputSystem.actions.FindAction("Interact", true);
		strikeAction = InputSystem.actions.FindAction("Strike", true);
		attackAction = InputSystem.actions.FindAction("Attack", true);
		changeAttackAction = InputSystem.actions.FindAction("ChangeAttack", true);
		aimAction = InputSystem.actions.FindAction("Aim", true);
		shootAction = InputSystem.actions.FindAction("Shoot", true);

		toggleUIAction = InputSystem.actions.FindAction("ToggleUI", true);
		switchUIAction = InputSystem.actions.FindAction("SwitchUI", true);

		item1Action = InputSystem.actions.FindAction("Item1", true);
		item2Action = InputSystem.actions.FindAction("Item2", true);
		item3Action = InputSystem.actions.FindAction("Item3", true);
		item4Action = InputSystem.actions.FindAction("Item4", true);

		toggleUpgradesAction = InputSystem.actions.FindAction("ToggleUpgrades", true);
		toggleInventoryAction = InputSystem.actions.FindAction("ToggleInventory", true);
		togglePauseAction = InputSystem.actions.FindAction("TogglePause", true);
  
		uiMoveAction = InputSystem.actions.FindAction("Navigate", true);
		uiSelectAction = InputSystem.actions.FindAction("Submit", true);
		uiCloseAction = InputSystem.actions.FindAction("Cancel", true);
    }

    // Update is called once per frame
    void Update()
    {
        // Apply inputs
        if (controlPlayer) 
        {
            player.Run(sprintAction.IsPressed());

            player.Slide(slideAction.IsPressed());
            player.Sneak(sneakAction.IsPressed());

            player.Aim(aimAction.IsPressed());

            Vector2 direction = moveAction.ReadValue<Vector2>();
            Vector2 rotation  = lookSensitivity * rotateAction.ReadValue<Vector2>();

            player.Rotate(rotation);
            player.Move(direction);
        }

        if (!controlPlayer) 
        {
            uiNavigateTimer -= Time.deltaTime;

            Vector2 uiDirection = uiMoveAction.ReadValue<Vector2>();
            if (uiDirection != Vector2.zero && uiNavigateTimer <= 0f) 
            {
                // control upgrades ui
                if (uiController.upgradesOpen) 
                {
                    upgradeUI.MoveSelection(uiDirection);
                    uiNavigateTimer = 1f / uiNavigateRepeatRate;
                }
    
                // control inventory ui
                if (uiController.inventoryOpen) 
                {
                    inventoryUI.MoveSelection(uiDirection);
                    uiNavigateTimer = 1f / uiNavigateRepeatRate;
                }
            }

            if (uiDirection == Vector2.zero)
                uiNavigateTimer = 0f;
        } 

        // items
        if (item1Action.WasPerformedThisFrame()) { this.player.UseItem(0); }
        if (item2Action.WasPerformedThisFrame()) { this.player.UseItem(1); }
        if (item3Action.WasPerformedThisFrame()) { this.player.UseItem(2); }
        if (item4Action.WasPerformedThisFrame()) { this.player.UseItem(3); }

        if (jumpAction.WasPerformedThisFrame()) { this.player.Jump(); }

        if (interactAction.WasPerformedThisFrame()) {this.player.Interact(); }

        // input using selected attack
        if (changeAttackAction.WasPerformedThisFrame()) {this.player.ChangeAttack(); }
        if (attackAction.WasPerformedThisFrame()) {this.player.Attack(); }

        // attack-type-specific inputs 
        if (strikeAction.WasPerformedThisFrame()) {
            this.player.ChangeAttack(Player.AttackType.Hit);
            this.player.Attack();
        }
        if (shootAction.WasPerformedThisFrame()) {
            this.player.ChangeAttack(Player.AttackType.Shoot);
            this.player.Attack();
        }


        if (uiSelectAction.WasPerformedThisFrame()) {
            if (this.uiController.upgradesOpen) 
            {
                // control upgrades ui
                this.playerUpgrades.TryUpgrade(this.upgradeUI.selectedStat);
            }
            else if (this.uiController.inventoryOpen) 
            {
                // control inventory ui
                this.inventoryUI.ToggleItemGrabbed();
            }
        }

        if (switchUIAction.WasPerformedThisFrame()) {
            // handle switching between UIs
            if (uiController.upgradesOpen) {
                this.uiController.SwitchToInventory();
            } else if (uiController.inventoryOpen) {
                this.uiController.SwitchToUpgrades();
            }
        }
        
        if (uiCloseAction.WasPerformedThisFrame()) {
            // handle closing of ui
            this.uiController.SwitchToGameplay();
        }

        if (toggleUIAction.WasPerformedThisFrame()) {
            // handle toggling of ui
            if (this.uiController.upgradesOpen || this.uiController.inventoryOpen) {
                this.uiController.SwitchToGameplay();
            } else {
                this.uiController.SwitchToInventory();
            }
        }

        if (toggleUpgradesAction.WasPerformedThisFrame()) {
            // toggle upgrade ui
            if (this.uiController.upgradesOpen) {
                this.uiController.SwitchToGameplay();
            } else {
                this.uiController.SwitchToUpgrades();
            }
        }

        if (toggleInventoryAction.WasPerformedThisFrame()) {
            // toggle inventory ui
            if (this.uiController.inventoryOpen) {
                this.uiController.SwitchToGameplay();
            } else {
                this.uiController.SwitchToInventory();
            }
        }


        // 
        Keyboard keyboard = Keyboard.current;

#if UNITY_EDITOR
        // For testing save/load
        if (keyboard.f5Key.wasPressedThisFrame)
        {
            GameSaver.save();
        }
        if (keyboard.f8Key.wasPressedThisFrame)
        {
            GameSaver.load();
        }
#endif
    }

}
