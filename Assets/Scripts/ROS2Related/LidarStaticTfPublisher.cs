using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Tf2;
using RosMessageTypes.Std;

namespace AutonomousPerception
{
    /// <summary>
    /// Publishes a static TF transform between two frames (e.g., base_link → lidar_link).
    ///
    /// In real ROS2, static transforms use "Transient Local" QoS so they persist
    /// for late-joining subscribers. Since the Unity TCP bridge doesn't support
    /// QoS profiles, this script re-publishes the static TF at a configurable rate
    /// (default 1 Hz) as a workaround.
    ///
    /// <b>Default Topic:</b> /tf (tf2_msgs/TFMessage)
    ///
    /// <b>Inspector Settings:</b>
    /// - parentFrameId: Parent TF frame (e.g., "base_link")
    /// - childFrameId: Child TF frame (e.g., "lidar_link")
    /// - publishRateHz: Re-publish rate for late joiners
    /// </summary>
    public class LidarStaticTfPublisher : MonoBehaviour
    {
        [Header("TF Frame Configuration")]
        [Tooltip("Parent frame (e.g., the drone/robot body)")]
        public string parentFrameId = "base_link";

        [Tooltip("Child frame (e.g., the sensor mount point)")]
        public string childFrameId = "lidar_link";

        [Header("ROS2 Settings")]
        [Tooltip("TF topic name")]
        public string topicName = "/tf";

        [Tooltip("Re-publish rate in Hz (for late-joining subscribers)")]
        public float publishRateHz = 1.0f;

        private ROSConnection ros;
        private float timeElapsed;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<TFMessageMsg>(topicName);

            // Publish immediately so early subscribers get data right away
            PublishStaticTransform();
        }

        void Update()
        {
            timeElapsed += Time.deltaTime;
            if (timeElapsed >= 1.0f / publishRateHz)
            {
                PublishStaticTransform();
                timeElapsed = 0;
            }
        }

        /// <summary>
        /// Builds and publishes an identity transform (no offset, no rotation)
        /// between parentFrameId and childFrameId.
        ///
        /// If the LiDAR sensor script already converts points to the ROS frame,
        /// the TF should be identity. Adjust translation/rotation here if the
        /// sensor has a physical offset from the robot center.
        /// </summary>
        private void PublishStaticTransform()
        {
            var tf = new TransformStampedMsg();

            // Timestamp (wall-clock for simulation sync)
            double rostime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            tf.header.stamp.sec = (int)rostime;
            tf.header.stamp.nanosec = (uint)((rostime - System.Math.Floor(rostime)) * 1e9);
            tf.header.frame_id = parentFrameId;
            tf.child_frame_id = childFrameId;

            // Identity transform (sensor is co-located with parent frame)
            tf.transform.translation.x = 0;
            tf.transform.translation.y = 0;
            tf.transform.translation.z = 0;
            tf.transform.rotation.x = 0;
            tf.transform.rotation.y = 0;
            tf.transform.rotation.z = 0;
            tf.transform.rotation.w = 1;

            var tfMessage = new TFMessageMsg();
            tfMessage.transforms = new TransformStampedMsg[] { tf };
            ros.Publish(topicName, tfMessage);
        }
    }
}
