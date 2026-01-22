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

    [Tooltip("The color that should be replaced with transparent pixels")]
    public Color backgroundColor = Color.black;
    [Tooltip("How much the color may deviate to be counted as background")]
    public float tolerance = 0.1f;

    public Vector2Int resolution = new Vector2Int(256,256);

    private GameObject[] items;
    private Camera renderCamera;
    private Texture2D result;

    void OnValidate() 
    {
        // find objects
        renderCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        renderCamera.backgroundColor = backgroundColor;

        // 
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

        if (Application.isEditor && cam == renderCamera) 
        {
            if (renderCamera.pixelWidth != resolution.x || renderCamera.pixelHeight != resolution.y) 
            {
                Debug.LogError("Camera resolution not matching the expected one. Please set it in the \"Game\" tab.");
                return;
            }

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

        // make black pixels transparent
        Debug.Log("making background color " + renderCamera.backgroundColor + " transparent");
        MakeColorTransparent(result, renderCamera.backgroundColor, tolerance);
    }

    private string GetPath() 
    {
        return Path.Combine(Application.dataPath, "Textures", "Items", items[selection].name + ".png");
    }

    // makes all pixels that are close (where euclidean norm is lower than tolerance) 
    // to the given color transparent
    private void MakeColorTransparent(Texture2D texture, Color colorToReplace, float tolerance) 
    {
        // get pixels, to overwrite black with transparent
        Color[] pixels = result.GetPixels();
        for (int i = 0; i < pixels.Length; i++) 
        {
            // make black pixels transparent
            Color diffColor = pixels[i] - colorToReplace;
            float diffR = diffColor.r;
            float diffG = diffColor.g;
            float diffB = diffColor.b;
            float diff = diffR * diffR + diffG * diffG + diffB * diffB;
            if (diff <= tolerance * tolerance)
            {
                pixels[i] = Color.black;
                pixels[i].a = 0;
            }
        }
        result.SetPixels(pixels);
    }

    public void Save() 
    {
        // get bytes 
        byte[] bytes = ImageConversion.EncodeToPNG(result);

        string path  = GetPath();
        Debug.Log("saving render texture to: " + path);
        File.WriteAllBytes(path, bytes);
    }

}
