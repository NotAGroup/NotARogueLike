using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FloorClearing : MonoBehaviour
{
    // reduce Find calls
    private GameObject cachedOpponent;

    public UnityEvent onCleared;
    public string[] opponentTags;

    bool cleared = false;

    public bool CheckClearingCondition() {
        if (cachedOpponent != null && cachedOpponent.activeInHierarchy) return false;

        // check all tags
        if (opponentTags != null)
        {
            foreach(string tag in opponentTags)
            {
                if (cachedOpponent != null) break;
                cachedOpponent = GameObject.FindWithTag(tag);
            }
        }

        return cachedOpponent == null;
    }

    void FixedUpdate()
    {
        if (!cleared && CheckClearingCondition()) {
            cleared = true;

            onCleared.Invoke();
        }
    }
}
