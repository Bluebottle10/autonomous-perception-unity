using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace AutonomousPerception
{
    /// <summary>
    /// Publishes camera RGB images to ROS2 as sensor_msgs/Image.
    ///
    /// This script captures frames from the assigned Unity Camera at a configurable
    /// resolution and frequency, then publishes them over the ROS TCP connection.
    ///
    /// <b>Default Topic:</b> /camera/image_raw (sensor_msgs/Image)
    ///
    /// <b>Inspector Settings:</b>
    /// - cam: The Unity Camera to capture from
    /// - topicName: ROS2 topic name (default: "camera/image_raw")
    /// - imageWidth/imageHeight: Capture resolution
    /// - publishMessageFrequency: Seconds between publishes
    /// </summary>
    public class RosCameraPublisher : MonoBehaviour
    {
        [Header("Camera Settings")]
        [Tooltip("The Unity Camera to capture and publish")]
        public Camera cam;

        [Tooltip("Capture width in pixels")]
        public int imageWidth = 1280;

        [Tooltip("Capture height in pixels")]
        public int imageHeight = 960;

        [Tooltip("Render texture bit depth")]
        public int imageDepth = 8;

        [Header("ROS2 Settings")]
        [Tooltip("ROS2 topic name for the published image")]
        public string topicName = "camera/image_raw";

        [Tooltip("Publish interval in seconds (0.1 = 10Hz)")]
        public float publishMessageFrequency = 0.1f;

        private ROSConnection ros;
        private Texture2D captureTexture;
        private float timeElapsed;

        void Start()
        {
            InitializeCamera();

            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<ImageMsg>(topicName);
        }

        /// <summary>
        /// Creates a dedicated RenderTexture for the camera to render into,
        /// and a matching Texture2D for CPU-side pixel readback.
        /// </summary>
        private void InitializeCamera()
        {
            var renderTex = new RenderTexture(imageWidth, imageHeight, imageDepth, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1
            };
            cam.targetTexture = renderTex;
            imageWidth = renderTex.width;
            imageHeight = renderTex.height;
            captureTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
        }

        void Update()
        {
            timeElapsed += Time.deltaTime;
            if (timeElapsed >= publishMessageFrequency)
            {
                var msg = CaptureImage();
                ros.Publish(topicName, msg);
                timeElapsed = 0;
            }
        }

        /// <summary>
        /// Renders the camera, reads pixels from the GPU, and converts to a ROS ImageMsg.
        /// </summary>
        public ImageMsg CaptureImage()
        {
            cam.Render();
            RenderTexture.active = cam.targetTexture;
            captureTexture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
            captureTexture.Apply();
            return captureTexture.ToImageMsg(new HeaderMsg());
        }
    }
}