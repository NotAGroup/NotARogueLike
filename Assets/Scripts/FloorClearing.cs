using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorClearing : MonoBehaviour
{
    // reduce Find calls
    private GameObject cachedOpponent;

    // gameobject to activate when the level is cleared
    public GameObject trapDoor;


    public bool CheckClearingCondition() {
        if (cachedOpponent != null && cachedOpponent.activeInHierarchy) return false;

        cachedOpponent = GameObject.FindWithTag("Opponent");
        return cachedOpponent == null;
    }

    void Update()
    {
        if (CheckClearingCondition()) {
            trapDoor.GetComponent<TrapDoor>().Enable();
        }
    }
}
