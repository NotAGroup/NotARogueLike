using UnityEngine;

public class Pausing : MonoBehaviour
{
    private float originalTimeScale;

    public bool isPaused { get; private set; } = false;

    public void Pause()
    {
        isPaused = true;
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        Time.timeScale = originalTimeScale;
        isPaused = false;
    }
}
