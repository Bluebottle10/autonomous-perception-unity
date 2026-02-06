using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Std;
using RosMessageTypes.Tf2;

public class OdometryPublisher : MonoBehaviour
{
    public string odomTopic = "odom";
    public string tfTopic = "/tf";
    public string parentFrameId = "odom";
    public string childFrameId = "base_link";

    private ROSConnection ros;
    // private Rigidbody robotBody;
    private float previousRealTime;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<OdometryMsg>(odomTopic);
        ros.RegisterPublisher<TFMessageMsg>(tfTopic);

        // robotBody = GetComponent<Rigidbody>();
        previousRealTime = Time.realtimeSinceStartup;
    }

    void FixedUpdate()
    {
        // 1. Get Unity Position & Rotation
        // Convert from Unity (Z-fwd, Y-up) to ROS (X-fwd, Z-up)
        // Unity Position
        Vector3 pos = transform.position;
        // Unity Rotation
        Quaternion rot = transform.rotation;

        // ROS Position: Z -> X, -X -> Y, Y -> Z
        Vector3 rosPos = new Vector3(pos.z, -pos.x, pos.y);
        
        // ROS Rotation: Z -> X, -X -> Y, Y -> Z (Complex quaternion mapping)
        // Standard Unity->ROS conversion:
        Quaternion rosRot = new Quaternion(-rot.z, rot.x, -rot.y, rot.w);

        // --- TIME SETUP ---
        double rostime = (double)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;;
        int sec = (int)rostime;
        uint nanosec = (uint)((rostime - System.Math.Floor(rostime)) * 1e9);

        // ==========================================
        // PART A: Publish The /tf Transform (CRITICAL FOR NAV2)
        // ==========================================
        TransformStampedMsg tf = new TransformStampedMsg();
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

        TFMessageMsg tfMessage = new TFMessageMsg();
        tfMessage.transforms = new TransformStampedMsg[] { tf };
        ros.Publish(tfTopic, tfMessage);

        // ==========================================
        // PART B: Publish The /odom Message (For Velocity)
        // ==========================================
        OdometryMsg odomMessage = new OdometryMsg();
        odomMessage.header = tf.header;
        odomMessage.child_frame_id = childFrameId;

        odomMessage.pose.pose.position.x = rosPos.x;
        odomMessage.pose.pose.position.y = rosPos.y;
        odomMessage.pose.pose.position.z = rosPos.z;

        odomMessage.pose.pose.orientation.x = rosRot.x;
        odomMessage.pose.pose.orientation.y = rosRot.y;
        odomMessage.pose.pose.orientation.z = rosRot.z;
        odomMessage.pose.pose.orientation.w = rosRot.w;

        // Calculate Velocity (Optional but good for Nav2)
        // For simplicity in this fix, we leave twist zero or implement logic later.
        // Nav2 mostly cares about the Pose above.

        ros.Publish(odomTopic, odomMessage);
    }
}