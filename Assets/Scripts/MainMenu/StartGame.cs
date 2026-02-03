using System.Data.SqlTypes;
using UnityEngine;

public class StartGame : MonoBehaviour
{
    public void StartNewGame()
    {
        Debug.Log("Starting New Game");
        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelScene");
    }
}
