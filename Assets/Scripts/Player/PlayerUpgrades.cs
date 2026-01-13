using UnityEngine;
using System;

[RequireComponent(typeof(Inventory))]
public class PlayerUpgrades : MonoBehaviour
{
    public PlayerUpgradeState levels;
    private GameObject definitions;

    public PlayerUpgrades() {
        levels = RunData.Instance.upgrades;
        GameSaver.subscribe(levels);
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
        int[] costs = defs[stat];
        if (level < defs.MaxLevel(stat)) {
            cost = costs[levels[stat]];
        } else {
            cost = -1;
        }

        return inventory.currency[Currency.XP] >= costs[levels[stat]];
    }

    public bool TryUpgrade(BaseStatKey stat)
    {
        if (CanUpgrade(stat, out int cost)) {
            levels[stat] += 1;
            GetComponent<Inventory>().currency[Currency.XP] -= cost;
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

