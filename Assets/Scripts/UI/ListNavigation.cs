using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListNavigation : MonoBehaviour
{
    public RectTransform[] uiElements;
    private int selected = 0;

    void OnEnable()
    {
        selected = 0;
        if (uiElements == null || uiElements.Length == 0) return;

        EventSystem.current.SetSelectedGameObject(uiElements[selected].gameObject);
    }

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
            b.Select();
        }
    }

    public void Submit()
    {
        if (uiElements == null || uiElements.Length == 0) return;

        if (uiElements[selected].TryGetComponent<Button>(out Button b))
        {
            b.onClick.Invoke();
        }
    }
}

