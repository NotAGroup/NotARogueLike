using UnityEngine;

public class MainMenuCamera : MonoBehaviour
{
    private Transform cameraTransform;
    private Vector2 tiling = new Vector2(5.0f, 5.0f);

    public Renderer floor, ceiling;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraTransform = GetComponent<Transform>();

        if (floor != null && ceiling != null)
        {
            floor.material.mainTextureScale = tiling;
            ceiling.material.mainTextureScale = tiling;
        }
    }

    // Update is called once per frame
    void Update()
    {
        cameraTransform.Rotate(new Vector3(0, 5, 0) * Time.deltaTime);
    }
}
