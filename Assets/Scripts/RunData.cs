using UnityEngine;
using System;

// class to store run data, allows defining persistence
// can be saved to disk entirely (inventory gets reset when starting new run)
public class RunData {
    public int level = -1;
    public CurrencyContainer currencies {get; private set;} = new();
    public ItemContainer items {get; private set;} = new();
    public PlayerUpgradeState upgrades {get; private set;} = new();
    public ConstitutionState constitution {get; private set;} = new();
    public Player.AttackType selectedAttack;


    public bool Initialized {
        get => level >= 0;
    }

    // 
    public void LevelDone() {
        Debug.Log("Level " + level + " done");
        level += 1;
        currencies[Currency.XP] += 100;
        currencies[Currency.Gold] += 5;
    }

    // reset rundata and keep persistent stuff
    public void NewRun() {
        Debug.Log("starting new run");
        GameObject defs = GameObject.Find("Definitions");
        StatScalingDefinitions statDef = defs.GetComponent<StatScalingDefinitions>();
        Constants constants = defs.GetComponent<Constants>();

        // back to first level
        level = 0;

        // reset currencies other than xp
        foreach (Currency c in Enum.GetValues(typeof(Currency)))
            if (c != Currency.XP) currencies[c] = 0;

        // create new and empty item container
        // here we could define persistence for specific items
        items.Clear();
        items.Resize(constants.itemSlots);

        // player upgrades are kept

        // reset constitution
        constitution.health = PlayerStats.ComputeInitial(StatKey.MaxHealth, statDef, upgrades);
        constitution.stamina = PlayerStats.ComputeInitial(StatKey.MaxStamina, statDef, upgrades);
        constitution.mana = PlayerStats.ComputeInitial(StatKey.MaxMana, statDef, upgrades);

        // add initial items and currencies
        items.AddItem(defs.GetComponent<ItemDefinitions>()[3], constants.initialAmmo, constants.inventoryMask);
        currencies[Currency.Gold] = constants.initialGold;

        Debug.Log("has XP: " + currencies[Currency.XP]);
    }
    
    public static RunData Instance = new();
}





