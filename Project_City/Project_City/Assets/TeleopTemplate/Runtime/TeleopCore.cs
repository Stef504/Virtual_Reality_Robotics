using UnityEngine;

namespace TeleopLab
{
    public enum TeleopSourceMode { Demo, Ros2 }

    // Implemented by an optional ROS adapter. The core never imports ROS libraries.
    public interface ITeleopRosPort
    {
        bool Ready { get; }
        string Health { get; }
        void SetVelocityCommand(float linearMetersPerSecond, float angularRadiansPerSecond);
    }

    public class TeleopCore : MonoBehaviour
    {
        public TeleopSourceMode mode = TeleopSourceMode.Demo;
        public DemoRobotDriver demoDriver;
        public DemoProximity demoProximity;
        public TeleopDashboard dashboard;
        public TrialRecorder trials;
        [Tooltip("Assign the optional ROS adapter here, after installing ROS-TCP-Connector.")]
        public MonoBehaviour rosAdapter;

        [Header("Command limits (not robot safety limits)")]
        public float maxLinearSpeed = 0.45f;
        public float maxAngularSpeed = 0.80f;

        public float LinearCommand { get; private set; }
        public float AngularCommand { get; private set; }
        public ITeleopRosPort RosPort { get { return rosAdapter as ITeleopRosPort; } }

        void Start() { ApplyMode(); }

        public void ApplyMode()
        {
            if (demoDriver) demoDriver.enabled = (mode == TeleopSourceMode.Demo);
            if (demoProximity) demoProximity.enabled = (mode == TeleopSourceMode.Demo);
            if (rosAdapter) rosAdapter.enabled = (mode == TeleopSourceMode.Ros2);
            if (dashboard) dashboard.EnterMode(mode);
            if (mode == TeleopSourceMode.Demo && trials) trials.ResetTrial();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) { mode = TeleopSourceMode.Demo; ApplyMode(); }
            if (Input.GetKeyDown(KeyCode.F2)) { mode = TeleopSourceMode.Ros2; ApplyMode(); }
            if (mode == TeleopSourceMode.Demo && Input.GetKeyDown(KeyCode.R) && trials)
                trials.ResetTrial();

            float throttle = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                           - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            float steer = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f)
                        - (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f);

            // Live motion requires a keyboard deadman. Simulator also accepts it, but does not require it.
            bool deadman = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (mode == TeleopSourceMode.Ros2 && !deadman) { throttle = 0; steer = 0; }
            if (Input.GetKey(KeyCode.Space)) { throttle = 0; steer = 0; }

            LinearCommand = throttle * maxLinearSpeed;
            AngularCommand = steer * maxAngularSpeed;

            if (mode == TeleopSourceMode.Demo)
            {
                if (demoDriver) demoDriver.SetVelocityCommand(LinearCommand, AngularCommand);
                if (trials && (throttle != 0 || steer != 0)) trials.StartIfNeeded();
            }
            else
            {
                ITeleopRosPort adapter = RosPort;
                if (adapter != null)
                {
                    // No nonzero commands until fresh odometry has been received.
                    if (adapter.Ready) adapter.SetVelocityCommand(LinearCommand, AngularCommand);
                    else adapter.SetVelocityCommand(0f, 0f);
                }
            }

            if (dashboard) dashboard.Refresh(this);
        }

        void OnDisable()
        {
            if (RosPort != null) RosPort.SetVelocityCommand(0f, 0f);
            if (demoDriver) demoDriver.SetVelocityCommand(0f, 0f);
        }
    }
}
