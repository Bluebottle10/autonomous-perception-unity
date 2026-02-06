using RosMessageTypes.UnityRoboticsDemo;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.AiInterfaces;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

public class RosPedestrianDetectionSrvCall : MonoBehaviour
{
    public Camera cam;
    public int imageWidth = 1280;
    public int imageHeight = 960;
    public int imageDepth = 8;
    private Texture2D QuickAccessTexture = null;
    
    public string serviceName = "yolo_detector";
    public float publishMessageFrequency = 0.5f;

    ROSConnection ros;
    private float timeElapsed;
    float awaitingResponseUntilTimestamp = -1;
    private bool shouldStop = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // initialize camera capture
        InitializeCamera();
        
        // setup ros
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<PedestrianDetectionRequest, PedestrianDetectionResponse>(serviceName);
    }
    
    void InitializeCamera()
    {
        RenderTexture texture = new RenderTexture(imageWidth,imageHeight,imageDepth,RenderTextureFormat.ARGB32);
        texture.filterMode = FilterMode.Point;
        texture.antiAliasing = 1;
        cam.targetTexture = texture;
        imageWidth = texture.width;
        imageHeight = texture.height;
        QuickAccessTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGB24, false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > awaitingResponseUntilTimestamp)
        {
            // setup request
            PedestrianDetectionRequest request = new PedestrianDetectionRequest();
            request.image = GetImage();
        
            // send message
            ros.SendServiceMessage<PedestrianDetectionResponse>(serviceName, request, Callback_PedestrianDetectionResponse);
            awaitingResponseUntilTimestamp = Time.time + 2.0f; 
        }
    }
    
    public ImageMsg GetImage()
    {
        cam.Render();

        RenderTexture.active = cam.targetTexture;
        QuickAccessTexture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        QuickAccessTexture.Apply();

        // return QuickAccessTexture.EncodeToPNG();
        return QuickAccessTexture.ToImageMsg(new HeaderMsg());
    }

    void Callback_PedestrianDetectionResponse(PedestrianDetectionResponse response)
    {
        shouldStop = response.stop;
    }

    public bool ShouldStop()
    {
        return shouldStop;
    }
}
