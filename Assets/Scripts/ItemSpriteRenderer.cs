using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Runtime.InteropServices;

[ExecuteInEditMode]
public class ItemSpriteRenderer : MonoBehaviour
{
    [Tooltip("if flag below is set, renders the item whenever you update this index")]
    public int selection = 0;

    [Tooltip("This will render a frame to a file. Use at your on risk")]
    public bool enableRendering = false;

    private GameObject[] items;
    private Camera renderCamera;
    private Texture2D result;

    void OnValidate() 
    {
        // find objects
        renderCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        items = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            items[i] = transform.GetChild(i).gameObject;
        }

        // update selection
        if (items.Length > selection)
        {
            Select(selection);
        }


        // prepare rendering
        if (!enableRendering) return;

        result = new Texture2D(256, 256, TextureFormat.RGBA32, false);

        // Add the onPostRender callback (remove first if present)
        Debug.Log("Adding on post render callback");
        RenderPipelineManager.endCameraRendering -= OnPostRenderCallback;
        RenderPipelineManager.endCameraRendering += OnPostRenderCallback;
    }

    void OnDestroy() 
    {
        RenderPipelineManager.endCameraRendering -= OnPostRenderCallback;
    }

    void Select(int index) 
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            items[i].SetActive(i == index);
        }
    }

    void OnPostRenderCallback(ScriptableRenderContext ctx, Camera cam)
    {
        // remove callback
        RenderPipelineManager.endCameraRendering -= OnPostRenderCallback;

        Debug.Log("Received Post render callback from " + cam.name);

        if (Application.isEditor && cam == renderCamera) {
            Render();
            Save();
        }
    }

    public void Render() 
    {
        RenderTexture cameraTexture = renderCamera.activeTexture;

        // compute rectantle
        Rect regionSource = new Rect(0, 0, Screen.width, Screen.height);
        int xPosTarget = 0;
        int yPosTarget = 0;
        bool updateMipMaps = false;

        Debug.Log("read pixels from render camera" + cameraTexture);
        result.ReadPixels(regionSource, xPosTarget, yPosTarget, updateMipMaps);
    }

    private string GetPath() 
    {
        return Path.Combine(Application.dataPath, "Textures", "Items", items[selection].name + ".png");
    }

    public void Save() 
    {
        byte[] bytes = ImageConversion.EncodeToPNG(result);
        Debug.Log("Got " + bytes.Length + " bytes");
        string path  = GetPath();

        Debug.Log("saving render texture to: " + path);

        File.WriteAllBytes(path, bytes);
    }

}
