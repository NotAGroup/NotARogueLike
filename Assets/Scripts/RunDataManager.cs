using UnityEngine;

public class RunDataManager : MonoBehaviour
{
    public void TryLoad()
    {
        if (!RunData.Instance.Initialized) {
            Debug.Log("Rundata not initialized yet, loading");
            RunData.Instance.NewRun();
            GameSaver.subscribe(RunData.Instance.currencies);
            GameSaver.subscribe(RunData.Instance.upgrades);
            GameSaver.load();
        } 
    }

    public void TrySave()
    {
        GameSaver.save();
    }
}
