using RosMessageTypes.UnityRoboticsDemo;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

namespace AutonomousPerception
{
    /// <summary>
    /// Example: Implements a ROS2 service server in Unity.
    ///
    /// Receives an ObjectPoseServiceRequest containing a GameObject name,
    /// looks up that object in the Unity scene, and returns its pose converted
    /// from Unity coordinates (Z-fwd, Y-up) to ROS coordinates (X-fwd, Z-up).
    ///
    /// <b>Service:</b> obj_pose_srv (unity_robotics_demo_msgs/ObjectPoseService)
    /// </summary>
    public class RosUnityServiceExample : MonoBehaviour
    {
        [Header("ROS2 Service")]
        [Tooltip("Name of the Unity-side service")]
        [SerializeField]
        private string serviceName = "obj_pose_srv";

        void Start()
        {
            ROSConnection.GetOrCreateInstance()
                .ImplementService<ObjectPoseServiceRequest, ObjectPoseServiceResponse>(
                    serviceName, GetObjectPose);
        }

        /// <summary>
        /// Service callback: finds the requested GameObject and returns its pose.
        /// Uses the FLU (Forward-Left-Up) coordinate conversion for ROS compatibility.
        /// </summary>
        private ObjectPoseServiceResponse GetObjectPose(ObjectPoseServiceRequest request)
        {
            Debug.Log($"[RosUnityService] Received request for object: {request.object_name}");

            var response = new ObjectPoseServiceResponse();
            var gameObject = GameObject.Find(request.object_name);

            if (gameObject != null)
            {
                // Convert Unity coordinates → ROS FLU coordinates
                response.object_pose.position = gameObject.transform.position.To<FLU>();
                response.object_pose.orientation = gameObject.transform.rotation.To<FLU>();
            }

            return response;
        }
    }
}
