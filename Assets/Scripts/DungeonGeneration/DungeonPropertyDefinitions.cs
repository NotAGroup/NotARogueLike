using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;

public class DungeonPropertyDefinitions : KeyValueStoreComponent<DungeonPropertyKey, InterpolationScaling>
{
    // returns the dungeon properties for the given level
    public DungeonProperties ComputeFrom(int level) {
        Dictionary<DungeonPropertyKey, float> result = new();
        
        for (int i = 0; i < data._defs.Length; i++) {
            result.Add((DungeonPropertyKey) i, data._defs[i].ComputeFrom(level));
        }

        return new DungeonProperties(result);
    }
}

public class DungeonProperties : Dictionary<DungeonPropertyKey, float> {
    public DungeonProperties(Dictionary<DungeonPropertyKey, float> b) : base(b) { }

    public override string ToString() {
        StringBuilder builder = new();

        foreach(var k in base.Keys)
        {
            builder.AppendFormat("{0}:{1},", k, base[k]);
        }
        return builder.ToString();
    }
}


[System.Serializable]
public class InterpolationScaling {
    [System.Serializable]
    public struct Point {
        public int   level;
        public float value;
    }

    [Tooltip("factor to apply")]
    public float baseValue = 1.0f;

    [Tooltip("interpolation points")]
    public Point[] points;

    [Tooltip("if true, cycles through the given points")]
    public bool cyclic;

    public float ComputeFrom(int level) {
        if (points == null || points.Length == 0)
            return baseValue;

        int index = 0;

        if (cyclic) {
            level = level % (points[points.Length - 1].level + 1);
        }

        for (; index + 1 < points.Length && points[index + 1].level <= level; index++);

        // index+1 still has to point into the array
        index = Math.Clamp(index, 0, points.Length - 2);

        // linear interpolation
        float diff = (points[index + 1].value - points[index].value) / (points[index + 1].level - points[index].level);
        float value = points[index].value + (level - points[index].level) * diff;

        return baseValue * value;
    }
};

public enum DungeonPropertyKey {
    // dungeon size (maybe expected number of rooms, or width)
    Size,
    // number of traps (maximum)
    TrapCount,
    // 
    EnemyCount,
    EncounterCount,
    EnemyLevel,
    EnemyMaxRangedCount,
    // probability for having a boss
    HasBoss,
    // allows balancing of (scarce) resources 
    AvailableXP,
    AvailableGold,
    AvailableAmmoPerEnemy,

    // 
    AvailableItemsPerArea,
    ChestsPerArea,
    // either a probability(0..1) or an expected number
    ShopsPerLevel
}

