using UnityEngine;

public class MainMenuCamera : MonoBehaviour
{
    Transform cameraTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraTransform = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        cameraTransform.Rotate(new Vector3(0, 5, 0) * Time.deltaTime);

    }
}
