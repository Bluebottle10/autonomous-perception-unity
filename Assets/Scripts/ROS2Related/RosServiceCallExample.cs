using RosMessageTypes.UnityRoboticsDemo;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

namespace AutonomousPerception
{
    /// <summary>
    /// Example: Calls a ROS2 service to get a new position for a cube.
    ///
    /// Sends the cube's current position/rotation as a PositionServiceRequest,
    /// and moves the cube to the position returned in the response.
    /// Useful for demonstrating ROS2 service call patterns from Unity.
    ///
    /// <b>Service:</b> pos_srv (unity_robotics_demo_msgs/PositionService)
    /// </summary>
    public class RosServiceCallExample : MonoBehaviour
    {
        [Header("ROS2 Service")]
        [Tooltip("Name of the ROS2 position service")]
        public string serviceName = "pos_srv";

        [Header("Target Object")]
        [Tooltip("The GameObject to move based on service responses")]
        public GameObject cube;

        [Header("Movement")]
        [Tooltip("Distance threshold to trigger next service call")]
        public float delta = 1.0f;

        [Tooltip("Movement speed in m/s")]
        public float speed = 2.0f;

        private ROSConnection ros;
        private Vector3 destination;
        private float awaitingResponseUntilTimestamp = -1;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<PositionServiceRequest, PositionServiceResponse>(serviceName);
            destination = cube.transform.position;
        }

        void Update()
        {
            // Move toward current destination
            float step = speed * Time.deltaTime;
            cube.transform.position = Vector3.MoveTowards(cube.transform.position, destination, step);

            // When close enough, request a new destination
            if (Vector3.Distance(cube.transform.position, destination) < delta
                && Time.time > awaitingResponseUntilTimestamp)
            {
                var cubePos = new PosRotMsg(
                    cube.transform.position.x, cube.transform.position.y, cube.transform.position.z,
                    cube.transform.rotation.x, cube.transform.rotation.y,
                    cube.transform.rotation.z, cube.transform.rotation.w
                );

                var request = new PositionServiceRequest(cubePos);
                ros.SendServiceMessage<PositionServiceResponse>(serviceName, request, OnDestinationReceived);
                awaitingResponseUntilTimestamp = Time.time + 1.0f;
            }
        }

        private void OnDestinationReceived(PositionServiceResponse response)
        {
            awaitingResponseUntilTimestamp = -1;
            destination = new Vector3(response.output.pos_x, response.output.pos_y, response.output.pos_z);
            Debug.Log($"[RosServiceCall] New destination: {destination}");
        }
    }
}
