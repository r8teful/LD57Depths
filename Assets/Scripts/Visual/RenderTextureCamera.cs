using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class RenderTextureCamera : MonoBehaviour {
    
    private Camera renderCamera;
    private RenderTexture renderTexture;
    
    private int screenWidth, screenHeight;
    public RenderTexture renderInput;
    //public RenderTexture blurredOutput;
    public FilterMode FilterMode;
    public bool SetTextureSize = false;
    [ShowIf("SetTextureSize")]
    public Vector2Int targetScreenSize = new Vector2Int(256, 144);
    [HideIf("SetTextureSize")]
    public uint screenScaleFactor = 1;

    public Texture GetInput => renderTexture; 

    private void Start() {
        Init();
    }
    public void SetDisplay(RawImage display) {
        display.color = Color.white;
        // Attaching texture to the display UI RawImage
        if(renderTexture == null) {
            Debug.LogWarning("Render texture not created! running init...");
            Init();
        }
        display.texture = renderTexture;
    }
    private void Update() {
        // Re initialize system if the screen has been resized
        if (CheckScreenResize()) Init();
    }

    public void Init() {

        // Initialize the camera and get screen size values
        if (!renderCamera) renderCamera = GetComponent<Camera>();
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        // Prevent any error
        if (screenScaleFactor < 1) screenScaleFactor = 1;
        if (targetScreenSize.x < 1) targetScreenSize.x = 1;
        if (targetScreenSize.y < 1) targetScreenSize.y = 1;

        // Calculate the render texture size
        int width = SetTextureSize ? (int)targetScreenSize.x : screenWidth / (int)screenScaleFactor;
        int height = SetTextureSize ? (int)targetScreenSize.y : screenHeight / (int)screenScaleFactor;

        // Initialize the render texture
        //renderTexture = new RenderTexture(width, height, 24) {
        //    filterMode = FilterMode,
        //    antiAliasing = 1
        //};

        //renderTexture = new RenderTexture(width, height, 
        //    renderTo.graphicsFormat,renderTo.depthStencilFormat);

        renderTexture = renderInput;
        //new RenderTexture(width,height,24,RenderTextureFormat.RG32,)
        // Set the render texture as the camera's output
        renderCamera.targetTexture = renderTexture;

    }

    public bool CheckScreenResize() {
        // Check whether the screen has been resized
        return Screen.width != screenWidth || Screen.height != screenHeight;
    }
}