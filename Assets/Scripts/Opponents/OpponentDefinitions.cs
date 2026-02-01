using System.Collections.Generic;
using UnityEngine;

public class OpponentDefinitions : MonoBehaviour
{
    private Dictionary<string, OpponentClassDefinition> classByName;
    public OpponentClassDefinition[] classes;

    public OpponentClassDefinition this[int c]
    {
        get => classes[c];
    }

    public OpponentClassDefinition this[string className]
    {
        get => classByName[className];
    }

    void Awake()
    {
        classByName = new Dictionary<string, OpponentClassDefinition>();

        // normalize probabilities
        float probabilitySum = 0.0f;

        foreach(OpponentClassDefinition def in classes)
        {
            def.stats.Init();
            probabilitySum += def.spawnProbability;
        }

        foreach(OpponentClassDefinition def in classes)
        {
            def.spawnProbability /= probabilitySum;

            if(!classByName.TryAdd(def.className, def))
            {
                Debug.LogError("Duplicate Opponent className: " + def.className);
            }
        }
    }
}

[System.Serializable]
public class OpponentClassDefinition {
    public string className;
    public float spawnProbability;
    public GameObject prefab;
    public bool bossEnemy;
    [Tooltip("Defines scaling of stats in the opponent level")]
    public KeyValueStore<OpponentStatKey, InterpolationScaling> stats;

    public InterpolationScaling this[OpponentStatKey key] {
        get => stats[key];
    }
};

