using UnityEngine;

public class UIGrid : MonoBehaviour
{
    [Header("Corner transforms")]
    public RectTransform cellTransformBotLeft;
    public RectTransform cellTransformBotRight;
    public RectTransform cellTransformTopLeft;

    [Header("Parent of grid cells")]
    public RectTransform cellParent;

    [Header("Prefab for grid cells")]
    public GameObject cellPrefab;

    public GameObject InstantiateGridEntry(int x, int y, int columns, int rows)
    {
        GameObject entry = Instantiate(cellPrefab, cellParent);

        float right = (columns > 1) ? ((float)x) / (columns - 1) : 0f;
        float up = (rows > 1) ? ((float)y) / (rows - 1) : 0f;

        // lerp position
        Vector2 pos1 = Vector2.Lerp(cellTransformBotLeft.localPosition, cellTransformBotRight.localPosition, right);
        Vector2 pos2 = Vector2.Lerp(cellTransformBotLeft.localPosition, cellTransformTopLeft.localPosition, up);

        // cellTransformBotLeft has been added twice, so subtract it once here
        RectTransform target = entry.GetComponent<RectTransform>();
        target.localPosition = pos1 + pos2 - (Vector2)cellTransformBotLeft.localPosition;

        return entry;
    }
    
    public void Commit()
    {
        cellTransformBotLeft.gameObject.SetActive(false);
        cellTransformBotRight.gameObject.SetActive(false);
        cellTransformTopLeft.gameObject.SetActive(false);
    }
}
