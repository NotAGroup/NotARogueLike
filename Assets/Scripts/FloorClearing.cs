using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FloorClearing : MonoBehaviour
{
    // reduce Find calls
    private GameObject cachedOpponent;

    public UnityEvent onCleared;
    public string opponentTag = "Opponent";

    bool cleared = false;

    public bool CheckClearingCondition() {
        if (cachedOpponent != null && cachedOpponent.activeInHierarchy) return false;

        cachedOpponent = GameObject.FindWithTag(opponentTag);
        return cachedOpponent == null;
    }

    void Update()
    {
        if (!cleared && CheckClearingCondition()) {
            cleared = true;

            onCleared.Invoke();
        }
    }
}
