using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Std;
using RosMessageTypes.Tf2;

namespace AutonomousPerception
{
    /// <summary>
    /// Publishes odometry data and TF transforms from a Unity GameObject to ROS2.
    ///
    /// Converts the attached GameObject's position and rotation from Unity's
    /// coordinate system (Z-forward, Y-up, left-handed) to ROS conventions
    /// (X-forward, Z-up, right-handed). Publishes both:
    ///   - nav_msgs/Odometry on /odom
    ///   - tf2_msgs/TFMessage on /tf (odom → base_link transform)
    ///
    /// <b>Default Topics:</b>
    /// - /odom (nav_msgs/Odometry)
    /// - /tf (tf2_msgs/TFMessage)
    ///
    /// <b>Inspector Settings:</b>
    /// - odomTopic: Odometry topic name
    /// - tfTopic: TF topic name
    /// - parentFrameId / childFrameId: TF frame names
    /// </summary>
    public class OdometryPublisher : MonoBehaviour
    {
        [Header("ROS2 Topics")]
        [Tooltip("Topic for nav_msgs/Odometry")]
        public string odomTopic = "odom";

        [Tooltip("Topic for tf2_msgs/TFMessage")]
        public string tfTopic = "/tf";

        [Header("TF Frame IDs")]
        public string parentFrameId = "odom";
        public string childFrameId = "base_link";

        private ROSConnection ros;
        private float previousRealTime;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<OdometryMsg>(odomTopic);
            ros.RegisterPublisher<TFMessageMsg>(tfTopic);
            previousRealTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Publishes odometry and TF every physics step.
        /// Converts Unity coordinates (Z-fwd, Y-up, left-handed) to
        /// ROS coordinates (X-fwd, Z-up, right-handed).
        /// </summary>
        void FixedUpdate()
        {
            // Unity → ROS coordinate conversion
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            // Position: Unity(X,Y,Z) → ROS(Z, -X, Y)
            Vector3 rosPos = new Vector3(pos.z, -pos.x, pos.y);

            // Rotation: quaternion coordinate swap
            Quaternion rosRot = new Quaternion(-rot.z, rot.x, -rot.y, rot.w);

            // Timestamp (wall-clock for ROS2 synchronization)
            double rostime = (double)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            int sec = (int)rostime;
            uint nanosec = (uint)((rostime - System.Math.Floor(rostime)) * 1e9);

            // --- Publish /tf (odom → base_link) ---
            var tf = new TransformStampedMsg();
            tf.header.frame_id = parentFrameId;
            tf.header.stamp.sec = sec;
            tf.header.stamp.nanosec = nanosec;
            tf.child_frame_id = childFrameId;

            tf.transform.translation.x = rosPos.x;
            tf.transform.translation.y = rosPos.y;
            tf.transform.translation.z = rosPos.z;
            tf.transform.rotation.x = rosRot.x;
            tf.transform.rotation.y = rosRot.y;
            tf.transform.rotation.z = rosRot.z;
            tf.transform.rotation.w = rosRot.w;

            var tfMessage = new TFMessageMsg();
            tfMessage.transforms = new TransformStampedMsg[] { tf };
            ros.Publish(tfTopic, tfMessage);

            // --- Publish /odom ---
            var odomMessage = new OdometryMsg();
            odomMessage.header = tf.header;
            odomMessage.child_frame_id = childFrameId;
            odomMessage.pose.pose.position.x = rosPos.x;
            odomMessage.pose.pose.position.y = rosPos.y;
            odomMessage.pose.pose.position.z = rosPos.z;
            odomMessage.pose.pose.orientation.x = rosRot.x;
            odomMessage.pose.pose.orientation.y = rosRot.y;
            odomMessage.pose.pose.orientation.z = rosRot.z;
            odomMessage.pose.pose.orientation.w = rosRot.w;

            ros.Publish(odomTopic, odomMessage);
        }
    }
}