using UnityEngine;
using System;
using System.Collections.Generic;

public enum Currency {
    Gold = 0,
    XP = 1
}

[System.Serializable]
public class ItemDefinition {
    [Header("General")]
    public string name;
    public ItemType type;

    [Header("Rendering")]
    public string displayName;
    public string description;
    // prefab for displaying item in scene (e.g. shop, loot drops)
    public GameObject itemModel;
    // prefab for displaying item in ui (e.g. hotbar)
    public GameObject itemModelUI;

    [Header("Loot")]
    [Range(0f,1f)]
    public float dropProbability;

    [Header("Shop")]
    [Range(0f,1f)]
    public float shopProbability;
    public int cost;
    public int maxPerShopSlot = 1;

    [Header("Inventory")]
    public float weight;
    public int maxPerInventorySlot = 1;

    [Header("Buffs")]
    public KeyValueStore<StatKey, ItemBuff> itemBuffs;
}

[System.Serializable]
public class ItemBuff
{
    public ScalingFormula.Operation operation;
    public float value;
}

public class ItemDefinitions : MonoBehaviour
{
    public ItemDefinition this[int i] {
        get => definitions[i];
    }

    // 
    public ItemDefinition[] definitions;

    void Awake()
    {
        foreach(ItemDefinition def in definitions) {
            def.itemBuffs.Init();
        }

        // normalize probabilities
        float probabilitySum = 0.0f;
        foreach(ItemDefinition def in definitions) {
            probabilitySum += def.shopProbability;
        }

        foreach(ItemDefinition def in definitions) {
            def.shopProbability /= probabilitySum;
        }


        // normalize probabilities
        probabilitySum = 0.0f;
        foreach(ItemDefinition def in definitions) {
            probabilitySum += def.dropProbability;
        }

        foreach(ItemDefinition def in definitions) {
            def.dropProbability /= probabilitySum;
        }
    }
}

[Flags]
public enum ItemType {
    Default = 0,
    Consumable = 1,
    Ammunition = 2,
    Buff = 4
}


