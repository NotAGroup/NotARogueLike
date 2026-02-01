using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorClearing : MonoBehaviour
{
    // reduce Find calls
    private GameObject cachedOpponent;

    // gameobject to activate when the level is cleared
    public GameObject refObject;
    public bool isBossDoor = false;


    public bool CheckClearingCondition() {
        if (cachedOpponent != null && cachedOpponent.activeInHierarchy) return false;

        cachedOpponent = GameObject.FindWithTag("Opponent");
        return cachedOpponent == null;
    }

    void Update()
    {
        if (CheckClearingCondition()) {
            if (!isBossDoor) {
                refObject.GetComponent<TrapDoor>().Enable();
            }
            else
            { 
                refObject.GetComponent<BossDoor>().Enable(); 
            }
        }
    }
}
