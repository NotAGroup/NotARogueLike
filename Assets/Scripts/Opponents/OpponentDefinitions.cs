using UnityEngine;

public class OpponentDefinitions : MonoBehaviour
{
    public OpponentClassDefinition[] classes;

    public OpponentClassDefinition this[int c] {
        get => classes[c];
    }

    void Awake() {
        // normalize probabilities
        float probabilitySum = 0.0f;
        foreach(OpponentClassDefinition def in classes) {
            def.stats.Init();
            probabilitySum += def.spawnProbability;
        }

        foreach(OpponentClassDefinition def in classes)
            def.spawnProbability /= probabilitySum;
    }
}

[System.Serializable]
public class OpponentClassDefinition {
    public string className;
    public float spawnProbability;
    public GameObject prefab;
    [Tooltip("Defines scaling of stats in the opponent level")]
    public KeyValueStore<OpponentStatKey, InterpolationScaling> stats;

    public InterpolationScaling this[OpponentStatKey key] {
        get => stats[key];
    }
};

