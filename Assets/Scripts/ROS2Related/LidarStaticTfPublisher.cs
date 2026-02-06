using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Tf2;
using RosMessageTypes.Std;

public class LidarStaticTfPublisher : MonoBehaviour
{
    // Configuration
    public string parentFrameId = "base_link"; // The Drone
    public string childFrameId = "lidar_link"; // The Sensor
    public string topicName = "/tf";

    // Publish once per second to ensure late-joiners (like RViz) catch it
    // (Real static TFs use "Transient Local" QoS, but 1Hz is a safe, simple alternative)
    public float publishRateHz = 1.0f; 
    
    private ROSConnection ros;
    private float timeElapsed;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TFMessageMsg>(topicName);
        
        // Publish immediately on start
        PublishStaticTransform();
    }

    void Update()
    {
        // Keep publishing periodically just in case RViz/Nav2 restarts
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= 1.0f / publishRateHz)
        {
            PublishStaticTransform();
            timeElapsed = 0;
        }
    }

    void PublishStaticTransform()
    {
        // 1. Create the TransformStamped (The actual link data)
        TransformStampedMsg tf = new TransformStampedMsg();

        // Header (Time + Parent)
        tf.header.frame_id = parentFrameId;
        // We can use 0 time for static transforms, or current time. 
        // Current time is safer for simulation synchronization.
        // var rostime = Unity.Robotics.Core.Clock.time;

        // NEW (Standard Unity Code):
        var now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        double rostime = now / 1000.0 + 0.2;
        //tf.header.stamp.sec = (int)rostime;
        //tf.header.stamp.nanosec = (uint)((rostime - System.Math.Floor(rostime)) * 1e9);
        tf.header.stamp.sec = (int)rostime;
        tf.header.stamp.nanosec = (uint)((rostime - System.Math.Floor(rostime)) * 1e9);

        // Child Frame
        tf.child_frame_id = childFrameId;

        // Position (0, 0, 0)
        // Since your Lidar is at the exact center of the drone:
        tf.transform.translation.x = 0;
        tf.transform.translation.y = 0;
        tf.transform.translation.z = 0;

        // Rotation (Identity / No Rotation)
        // Unity (Z-fwd, Y-up) vs ROS (X-fwd, Z-up).
        // Since your C# Lidar script ALREADY converts the points to ROS frame manually,
        // we want the TF to be "Identity" (0,0,0 rotation).
        tf.transform.rotation.x = 0;
        tf.transform.rotation.y = 0;
        tf.transform.rotation.z = 0;
        tf.transform.rotation.w = 1;

        // 2. Wrap it in a TFMessage (A list of transforms)
        TFMessageMsg tfMessage = new TFMessageMsg();
        tfMessage.transforms = new TransformStampedMsg[] { tf };

        // 3. Publish
        ros.Publish(topicName, tfMessage);
        // Debug.Log($"Sent TF Update to {topicName} at time {Time.time}");
    }
}