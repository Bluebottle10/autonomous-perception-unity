using UnityEngine;
using UnityEngine.Splines;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace AutonomousPerception
{
    /// <summary>
    /// Controls a SplineAnimate component based on ROS2 stop/go signals.
    ///
    /// Subscribes to a Bool topic. When true, the spline animation pauses
    /// (e.g., pedestrian detected). When false, the animation resumes.
    ///
    /// <b>Default Topic:</b> /yolo/stop_signal (std_msgs/Bool)
    ///
    /// <b>Inspector Settings:</b>
    /// - splineAnim: The SplineAnimate component to control
    /// - topicName: ROS2 Bool topic to subscribe to
    /// </summary>
    public class RosSplineAnimatorController : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("The SplineAnimate component to pause/resume")]
        public SplineAnimate splineAnim;

        [Header("ROS2 Settings")]
        [Tooltip("ROS2 topic for stop/go signal (std_msgs/Bool)")]
        public string topicName = "yolo/stop_signal";

        void Start()
        {
            ROSConnection.GetOrCreateInstance().Subscribe<BoolMsg>(topicName, OnStopSignalReceived);
        }

        /// <summary>
        /// Callback: pauses spline animation when stop=true, resumes when false.
        /// </summary>
        private void OnStopSignalReceived(BoolMsg msg)
        {
            if (msg.data)
                splineAnim.Pause();
            else
                splineAnim.Play();
        }
    }
}
