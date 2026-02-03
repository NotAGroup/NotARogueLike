using UnityEngine;
using UnityEngine.Events;
using System;

[RequireComponent(typeof(Inventory))]
public class PlayerUpgrades : MonoBehaviour
{
    public PlayerUpgradeState levels;
    private GameObject definitions;

    public UnityEvent onStatsChanged;

    public void GetFromRunData(RunData runData) {
        levels = runData.upgrades;
    }

    void Awake() {
        definitions = GameObject.Find("Definitions");
    }

    public ref int this[BaseStatKey stat] {
        get => ref levels[stat];
    }

    public bool CanUpgrade(BaseStatKey stat, out int cost)
    {
        var defs = definitions.GetComponent<UpgradeCostDefinitions>();
        var inventory = GetComponent<Inventory>();

        int level = levels[stat];
        if (level >= defs.MaxLevel(stat)) {
            cost = -1;
            return false;
        } 
        
        int[] costs = defs[stat];
        cost = costs[levels[stat]];
        return inventory.currency[Currency.XP] >= costs[levels[stat]];
    }

    public bool TryUpgrade(BaseStatKey stat)
    {
        if (CanUpgrade(stat, out int cost)) {
            levels[stat] += 1;
            GetComponent<Inventory>().AddCurrency(Currency.XP, -cost);
            onStatsChanged.Invoke();
            return true;
        }

        return false;
    }
}

[Serializable]
public class PlayerUpgradeState {
    [SaveAble]
    public int[] levels;

    public PlayerUpgradeState() {
        levels = new int[Enum.GetValues(typeof(BaseStatKey)).Length];
        foreach (BaseStatKey key in Enum.GetValues(typeof(BaseStatKey))) {
            levels[(int)key] = 0;
        }
    }

    public ref int this[BaseStatKey stat] {
        get => ref levels[(int)stat];
    }
}

