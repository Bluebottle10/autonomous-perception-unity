using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

namespace AutonomousPerception
{
    /// <summary>
    /// Subscribes to ROS2 velocity commands and drives a Unity GameObject.
    ///
    /// Converts ROS2 Twist messages (linear.x for speed, angular.z for steering)
    /// into Unity Transform movements. Handles the coordinate conversion between
    /// ROS (radians, CCW positive) and Unity (degrees, CW positive).
    ///
    /// <b>Default Topic:</b> /cmd_vel (geometry_msgs/Twist)
    ///
    /// <b>Inspector Settings:</b>
    /// - topicName: ROS2 topic to subscribe to
    /// </summary>
    public class RosVehicleController : MonoBehaviour
    {
        [Header("ROS2 Settings")]
        [Tooltip("ROS2 topic for velocity commands (geometry_msgs/Twist)")]
        public string topicName = "cmd_vel";

        private float targetSpeed;
        private float targetRotation;

        void Start()
        {
            ROSConnection.GetOrCreateInstance().Subscribe<TwistMsg>(topicName, OnVelocityReceived);
        }

        /// <summary>
        /// Called when a new Twist message arrives from ROS2.
        /// Converts from ROS conventions (m/s, rad/s) to Unity conventions.
        /// </summary>
        private void OnVelocityReceived(TwistMsg msg)
        {
            // Linear speed (m/s) — used directly
            targetSpeed = (float)msg.linear.x;

            // Angular velocity: ROS +Z = CCW (left), Unity +Y = CW (right)
            // Flip sign and convert radians → degrees
            targetRotation = -(float)msg.angular.z * Mathf.Rad2Deg;
        }

        void Update()
        {
            // Apply movement (frame-rate independent via deltaTime)
            transform.Translate(Vector3.forward * targetSpeed * Time.deltaTime);
            transform.Rotate(Vector3.up * targetRotation * Time.deltaTime);
        }
    }
}
