using UnityEngine;
using System;
using System.Collections.Generic;

/// provides definitions on how to compute gameplay-stats from base-stats
[DisallowMultipleComponent]
public class StatScalingDefinitions : KeyValueStoreComponent<StatKey, ScalingFormula>
{ }


[System.Serializable]
public class KeyValueStoreComponent<Key, Value> : MonoBehaviour
    where Key : System.Enum
{
    public KeyValueStore<Key, Value> data;

    void Awake() { data.Init(); }
    void OnValidate() { data.OnValidate(); }

    public Value this[Key key] { get => data[key]; }
}

/// <summary>
/// An enum-indexed dictionary, that can be modified via the Unity Editor
/// </summary>
/// <typeparam name="Key">an enum type</typeparam>
/// <typeparam name="Value"></typeparam>
[System.Serializable]
public class KeyValueStore<Key, Value>
    where Key : System.Enum
{
    [System.Serializable]
    public struct Def {
        [Tooltip("property key")]
        public string key;
        [Tooltip("definition")]
        public Value def;
    };

    // mappings between enums values and their names
    static Dictionary<string, Key> stringToKey = new();
    static Dictionary<Key, string> keyToString = new();

    static KeyValueStore() {
        var names = Enum.GetNames(typeof(Key));
        var values = Enum.GetValues(typeof(Key));

        for (int i = 0; i < names.Length; i++) {
            string n = (string)names.GetValue(i);
            Key k = (Key)values.GetValue(i);
            stringToKey.Add(n, k);
            keyToString.Add(k, n);
        }
    }

    // for editor
    [SerializeField]
    public Def[] definitions;

    // optimized datastructure
    [HideInInspector, NonSerialized]
    public Value[] _defs;

    private static int ToIndex(Key key) {
        Type underlyingType = Enum.GetUnderlyingType(key.GetType());
        return (int)Convert.ChangeType(key, underlyingType);
    }

    private static int ToIndex(string name) {
        return ToIndex(stringToKey[name]);
    }

    public void OnValidate()
    {
        Init();
    }

    // put definitions into an array
    public void Init() {
        _defs = new Value[Enum.GetValues(typeof(Key)).Length];
        foreach (Def d in definitions) {
            _defs[ToIndex(d.key)] = d.def;
        }

        // check for missing definitions
        for (int i = 0; i < _defs.Length; i++) {
            if (_defs[i] == null) {
                Debug.LogError("scaling undefined for " + Enum.GetValues(typeof(Key)).GetValue(i));
                _defs[i] = default;
            }
        }
    }

    public Value this[Key key] {
        get => _defs[ToIndex(key)];
    }
}

[System.Serializable]
public class ScalingFormula {
    [System.Serializable]
    public struct Operand {
        [Tooltip("The base stat to depend on")]
        public BaseStatKey stat;
        [Tooltip("scaling in level of base stat")]
        public InterpolationScaling value;
    }

    // defines how the values should be combined
    public enum Operation {
        Addition, Multiplication
    };

    [Tooltip("base factor")]
    public float baseValue = 1f;

    [Tooltip("how to combine values from base stats")]
    public Operation operation = Operation.Multiplication;
    [Tooltip("scaling in different base stats (base value should be 1 for multiplication, 0 for addition)")]
    public Operand[] scalingInBaseStat = null;

    public float ComputeFrom(PlayerUpgradeState upgradeLevels) {
        float result = baseValue;
        if (scalingInBaseStat != null) {
            foreach (Operand s in scalingInBaseStat) {
                int level = upgradeLevels[s.stat];
                float value = s.value.ComputeFrom(level);
                if (operation == Operation.Multiplication) {
                    result *= value;
                } else {
                    result += value;
                }
            }
        }
        return result;
    }
}

// base stats that can be upgraded
public enum BaseStatKey {
    Health,
    Armour,
    Strength,
    Agility,
    Stamina,
    Dexterity,
    Perception
}

// stats that are relevant to gameplay and depend on base stats
public enum StatKey {
    FireRate,
    HitRate,
    SwingDuration,
    StrikeDuration,
    StrikeDamage,
    ArrowDamage,
    ArrowSpeed,
    ReturnDuration,
    MaxMana,
    ManaRegRate,
    RunSpeed,
    WalkSpeed,
    SneakSpeed,
    MaxSlideTime,
    SlideSpeed,
    MaxHealth,
    HealthRegRate,
    JumpHeight,
    MaxStamina,
    StaminaRegRate,
    RunConsRate,
    SlideConsRate,
    JumpConsRate
}

