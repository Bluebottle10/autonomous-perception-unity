using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class SimpleRosCar : MonoBehaviour
{
// Store the latest commands from ROS
    private float targetSpeed = 0f;
    private float targetRotation = 0f;

    void Start()
    {
        // Subscribe to standard ROS command topic
        ROSConnection.GetOrCreateInstance().Subscribe<TwistMsg>("cmd_vel_smoothed", OnMessageReceived);
    }

    void OnMessageReceived(TwistMsg msg)
    {
        // 1. Get Linear Speed (Gas)
        // ROS sends meters/second. We can use it directly.
        targetSpeed = (float)msg.linear.x;

        // 2. Get Angular Speed (Steering)
        // ROS sends Radians per Second. Unity expects Degrees.
        // Also, ROS +Z is Left (CCW), Unity +Y is Right (CW) usually.
        // We flip the sign (-) and convert to Degrees.
        targetRotation = -(float)msg.angular.z * Mathf.Rad2Deg;
    }

    void Update()
    {
        // 3. Move the Object
        // "Time.deltaTime" makes it smooth regardless of frame rate
        
        // Move Forward
        transform.Translate(Vector3.forward * targetSpeed * Time.deltaTime);

        // Turn
        transform.Rotate(Vector3.up * targetRotation * Time.deltaTime);
    }
}
