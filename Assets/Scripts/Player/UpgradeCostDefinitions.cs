using UnityEngine;
using System;

public class UpgradeCostDefinitions : KeyValueStoreComponent<BaseStatKey, int[]>
{
    public int MaxLevel(BaseStatKey stat) {
        return data[stat].Length;
    }
}




