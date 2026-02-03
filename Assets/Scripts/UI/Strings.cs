using UnityEngine;

public class Strings : MonoBehaviour
{
    public KeyValueStore<BaseStatKey, string> baseStats;
    public KeyValueStore<StatKey, string> stats;

    void Awake()
    {
        stats.Init();
        baseStats.Init();
    }
}
