using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

namespace TeleopStudentModules
{
    /// <summary>
    /// Reads raw 3D pose telemetry from TeleopStudentContext and publishes 
    /// a ROS 2 PoseStamped message. Topic name and frame ID can be set via UI InputFields.
    /// </summary>
    public sealed class RobotPosePublisher : MonoBehaviour
    {
        // FIX FOR CS0103: Class field declaration restored
        [Header("Context Reference")]
        public TeleopStudentContext context;

        [Header("ROS Topic Configuration")]
        public string poseTopicName = "/robot_pose";
        private string frameId = "map";

        private ROSConnection ros;

        void Start()
        {
            if (!context) context = FindObjectOfType<TeleopStudentContext>();

            ros = ROSConnection.GetOrCreateInstance();
            if (ros != null && !string.IsNullOrEmpty(poseTopicName))
            {
                ros.RegisterPublisher<PoseStampedMsg>(poseTopicName);
            }
        }

        /// <summary>
        /// Converts Unity Time.time into a ROS 2 TimeMsg without package dependencies.
        /// </summary>
        private RosMessageTypes.BuiltinInterfaces.TimeMsg GetRosTime()
        {
            float currentSeconds = Time.time;
            int seconds = (int)Mathf.Floor(currentSeconds);
            uint nanoseconds = (uint)((currentSeconds - seconds) * 1e9f);

            return new RosMessageTypes.BuiltinInterfaces.TimeMsg
            {
                sec = seconds,       // ROS 2 TimeMsg sec field (int32)
                nanosec = nanoseconds // ROS 2 TimeMsg nanosec field (uint32)
            };
        }

        void Update()
        {
            if (!context || context.Current == null || !context.Current.poseAvailable) return;

            TeleopFrame frame = context.Current;

            Vector3 unityPos = frame.robotWorldPosition;
            float yawDegrees = frame.robotYawDegrees;

            // Construct ROS 2 PoseStamped Message
           PoseStampedMsg poseMsg = new PoseStampedMsg();
            poseMsg.header = new HeaderMsg
            {
                frame_id = frameId,
                stamp = GetRosTime()
            };

            // Convert Unity coordinates (X Right, Y Up, Z Forward) -> ROS (X Forward, Y Left, Z Up)
            poseMsg.pose.position.x = unityPos.z;
            poseMsg.pose.position.y = -unityPos.x;
            poseMsg.pose.position.z = unityPos.y;

            Quaternion rosRotation = Quaternion.Euler(0, 0, -yawDegrees);
            poseMsg.pose.orientation.x = rosRotation.x;
            poseMsg.pose.orientation.y = rosRotation.y;
            poseMsg.pose.orientation.z = rosRotation.z;
            poseMsg.pose.orientation.w = rosRotation.w;

            ros.Publish(poseTopicName, poseMsg);
        }
    }
}