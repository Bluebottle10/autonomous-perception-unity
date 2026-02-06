using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace ROS2Related
{
    public class RosImagePublishExample : MonoBehaviour
    {
        public Camera cam;
        public int imageWidth = 1280;
        public int imageHeight = 960;
        public int imageDepth = 8;
        private Texture2D QuickAccessTexture = null;
    
        ROSConnection ros;
        public string topicName = "cam_view";
    
        // Publish the cube's position and rotation every N seconds
        public float publishMessageFrequency = 0.5f;

        // Used to determine how much time has elapsed since the last message was published
        private float timeElapsed;
    
    
        bool _tookImage = false;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // initialize camera capture
            InitializeCamera();
        
            // setup ros
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<ImageMsg>(topicName);
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
            timeElapsed += Time.deltaTime;

            if (timeElapsed > publishMessageFrequency)
            {
                // publish
                var msg = GetImage();
                ros.Publish(topicName, msg);

                timeElapsed = 0;
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
    }
}