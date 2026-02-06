using UnityEngine;
using UnityEngine.UI;

public class ListNavigation : MonoBehaviour
{
    public RectTransform[] uiElements;
    private int selected = 0;

    public void UpdateSelection(Vector2 delta)
    {
        if (uiElements == null || uiElements.Length == 0) return;

        if (delta.y > 0.1f)
        {
            selected++;
        }
        else if (delta.y < -0.1f)
        {
            selected--;
        }

        selected = (selected + uiElements.Length) % uiElements.Length;

        if (uiElements[selected].TryGetComponent<Button>(out Button b))
        {
            Debug.Log("selection " + uiElements[selected].name);
            b.Select();
        }
    }

    public void Submit()
    {
        if (uiElements == null || uiElements.Length == 0) return;

        if (uiElements[selected].TryGetComponent<Button>(out Button b))
        {
            Debug.Log("invoking " + uiElements[selected].name);
            b.onClick.Invoke();
        }
    }
}

