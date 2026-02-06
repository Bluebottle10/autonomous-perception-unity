using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class MainCameraDepthPublisher : MonoBehaviour
{
    public Shader extractDepthShader; // Assign "ExtractDepth" here
    public string topicName = "cam_view/depth";
    public float publishFrequency = 10f;

    private Material depthMat;
    private ROSConnection ros;
    private float timeElapsed;
    
    // We need a temporary texture to hold the processed depth
    private RenderTexture depthRT;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImageMsg>(topicName);

        // 1. CRITICAL: Tell Main Camera to actually generate a depth texture
        // Without this, _CameraDepthTexture is empty!
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;

        // Create a material to run our shader
        if (extractDepthShader != null)
            depthMat = new Material(extractDepthShader);
        else
            Debug.LogError("Assign ExtractDepth Shader!");
    }

    // 2. This Unity function runs AFTER the camera finishes rendering the screen
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // A. Copy the screen to the screen (so you still see the game!)
        Graphics.Blit(source, destination);

        // B. Run our Depth Extraction logic
        // We only do this periodically to save performance
        timeElapsed += Time.deltaTime;
        if (timeElapsed > 1.0f / publishFrequency)
        {
            CaptureDepth(source);
            timeElapsed = 0;
        }
    }

    void CaptureDepth(RenderTexture source)
    {
        // Ensure we have a texture to draw into (match screen size)
        if (depthRT == null || depthRT.width != source.width || depthRT.height != source.height)
        {
            if (depthRT != null) depthRT.Release();
            depthRT = new RenderTexture(source.width, source.height, 0);
        }

        // Run the Shader: Source(Screen) -> Material(ExtractDepth) -> DepthRT
        Graphics.Blit(source, depthRT, depthMat);

        // Read pixels and Publish
        Texture2D image = new Texture2D(depthRT.width, depthRT.height, TextureFormat.RGB24, false);
        
        RenderTexture.active = depthRT;
        image.ReadPixels(new Rect(0, 0, depthRT.width, depthRT.height), 0, 0);
        image.Apply();
        RenderTexture.active = null;

        ImageMsg msg = new ImageMsg();
        msg.header.frame_id = "camera_link";
        
        // ... after msg.width = ...
        msg.width = (uint)depthRT.width;
        
        // ADD THIS LINE:
        msg.step = (uint)(depthRT.width * 3); // Width * BytesPerPixel (BGR = 3)
        
        msg.encoding = "bgr8";
        // ...
        
        // --- THE FIX: Manual Timestamp Generation ---
        // 1. Get time since start in seconds
        double timeInSeconds = Time.timeAsDouble; 
        
        // 2. Split into Seconds (int) and Nanoseconds (uint)
        uint sec = (uint)timeInSeconds;
        uint nanosec = (uint)((timeInSeconds - sec) * 1e9);

        msg.header.stamp.sec = (int)sec;
        msg.header.stamp.nanosec = nanosec;
        // ---------------------------------------------
        
        msg.height = (uint)depthRT.height;
        msg.width = (uint)depthRT.width;
        msg.encoding = "bgr8"; // Sending grayscale as RGB for easy viewing
        msg.data = image.GetRawTextureData();

        ros.Publish(topicName, msg);
        
        Destroy(image);
    }
}