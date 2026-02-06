using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

namespace AutonomousPerception
{
    /// <summary>
    /// Publishes depth images from the main camera to ROS2.
    ///
    /// Uses a custom shader (ExtractDepth) to extract the camera's depth buffer
    /// and encode it as a 3-channel BGR image. The depth is normalized against
    /// the camera's far plane and packed into the R channel (0-255).
    ///
    /// <b>Default Topic:</b> /camera/depth (sensor_msgs/Image)
    ///
    /// <b>Inspector Settings:</b>
    /// - extractDepthShader: Assign the "ExtractDepth" shader
    /// - topicName: ROS2 topic name
    /// - publishFrequency: Publish rate in Hz
    /// </summary>
    public class DepthCameraPublisher : MonoBehaviour
    {
        [Header("Shader")]
        [Tooltip("Assign the ExtractDepth shader from Assets/Shaders")]
        public Shader extractDepthShader;

        [Header("ROS2 Settings")]
        [Tooltip("ROS2 topic name for the published depth image")]
        public string topicName = "camera/depth";

        [Tooltip("Publish rate in Hz")]
        public float publishFrequency = 10f;

        [Header("Frame")]
        [Tooltip("TF frame ID attached to depth messages")]
        public string frameId = "camera_link";

        private Material depthMat;
        private ROSConnection ros;
        private float timeElapsed;
        private RenderTexture depthRT;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<ImageMsg>(topicName);

            // Enable Unity's depth texture generation on this camera
            GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;

            if (extractDepthShader != null)
                depthMat = new Material(extractDepthShader);
            else
                Debug.LogError("[DepthCameraPublisher] Assign ExtractDepth Shader in Inspector!");
        }

        /// <summary>
        /// Called by Unity after the camera finishes rendering.
        /// Passes the frame through, then periodically extracts depth.
        /// </summary>
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Pass-through: keep the game view visible
            Graphics.Blit(source, destination);

            timeElapsed += Time.deltaTime;
            if (timeElapsed >= 1.0f / publishFrequency)
            {
                CaptureAndPublishDepth(source);
                timeElapsed = 0;
            }
        }

        /// <summary>
        /// Extracts depth via shader blit, reads pixels, and publishes as ROS ImageMsg.
        /// </summary>
        private void CaptureAndPublishDepth(RenderTexture source)
        {
            // Ensure render target matches screen size
            if (depthRT == null || depthRT.width != source.width || depthRT.height != source.height)
            {
                if (depthRT != null) depthRT.Release();
                depthRT = new RenderTexture(source.width, source.height, 0);
            }

            // Blit through depth extraction shader
            Graphics.Blit(source, depthRT, depthMat);

            // Read pixels to CPU
            var image = new Texture2D(depthRT.width, depthRT.height, TextureFormat.RGB24, false);
            RenderTexture.active = depthRT;
            image.ReadPixels(new Rect(0, 0, depthRT.width, depthRT.height), 0, 0);
            image.Apply();
            RenderTexture.active = null;

            // Build ROS message
            var msg = new ImageMsg();

            // Timestamp
            double timeInSeconds = Time.timeAsDouble;
            uint sec = (uint)timeInSeconds;
            uint nanosec = (uint)((timeInSeconds - sec) * 1e9);
            msg.header.stamp.sec = (int)sec;
            msg.header.stamp.nanosec = nanosec;
            msg.header.frame_id = frameId;

            // Image metadata
            msg.height = (uint)depthRT.height;
            msg.width = (uint)depthRT.width;
            msg.encoding = "bgr8";
            msg.step = (uint)(depthRT.width * 3);  // Width * BytesPerPixel
            msg.data = image.GetRawTextureData();

            ros.Publish(topicName, msg);
            Destroy(image);
        }
    }
}