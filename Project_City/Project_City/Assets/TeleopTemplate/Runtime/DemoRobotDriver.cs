
using UnityEngine;

namespace TeleopLab
{
    [RequireComponent(typeof(CharacterController))]
    public class DemoRobotDriver : MonoBehaviour
    {
        public enum MovementAxis
        {
            ForwardZ,
            RightX,
            BackwardZ,
            LeftX
        }

        [Header("Robot References")]
        public Transform visualRoot;
        public Transform goal;
        public TrialRecorder trials;

        [Header("Starting Position")]
        public Vector3 startPosition = new Vector3(0f, 0f, -7f);
        public float goalRadius = 0.85f;

        [Header("Movement Direction")]
        public MovementAxis movementAxis = MovementAxis.RightX;

        [Header("Movement Speed")]
        public float maxLinearSpeed = 0.5f;
        public float maxAngularSpeed = 35f;

        [Header("Acceleration")]
        public float linearAcceleration = 1.4f;
        public float angularAcceleration = 1.0f;

        [Header("Rotation")]
        public bool invertAngularDirection = true;

        private CharacterController controller;

        private float targetLinear;
        private float targetAngular;
        private float linear;
        private float angular;
        private float lastCollisionTime = -100f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void SetVelocityCommand(float v, float w)
        {
            targetLinear = Mathf.Clamp(
                v, -maxLinearSpeed, maxLinearSpeed
            );

            float maxAngularRadians =
                maxAngularSpeed * Mathf.Deg2Rad;

            targetAngular = Mathf.Clamp(
                w,
                -maxAngularRadians,
                maxAngularRadians
            );
        }

        public void ResetRobot()
        {
            if (!controller)
                controller = GetComponent<CharacterController>();

            controller.enabled = false;

            transform.position = startPosition;
            transform.rotation = Quaternion.identity;

            controller.enabled = true;

            targetLinear = 0f;
            targetAngular = 0f;
            linear = 0f;
            angular = 0f;

            lastCollisionTime = -100f;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            linear = Mathf.MoveTowards(
                linear,
                targetLinear,
                linearAcceleration * dt
            );

            angular = Mathf.MoveTowards(
                angular,
                targetAngular,
                angularAcceleration * dt
            );

            // Rotate in place
            float rotationSign =
                invertAngularDirection ? -1f : 1f;

            float rotationDegrees =
                angular * Mathf.Rad2Deg * dt * rotationSign;

            transform.Rotate(
                0f,
                rotationDegrees,
                0f,
                Space.World
            );

            // Select movement direction
            Vector3 direction;

            switch (movementAxis)
            {
                case MovementAxis.RightX:
                    direction = transform.right;
                    break;

                case MovementAxis.BackwardZ:
                    direction = -transform.forward;
                    break;

                case MovementAxis.LeftX:
                    direction = -transform.right;
                    break;

                default:
                    direction = transform.forward;
                    break;
            }

            // Move robot
            if (controller != null &&
                controller.enabled &&
                Mathf.Abs(linear) > 0.001f)
            {
                controller.Move(
                    direction * linear * dt
                );
            }

            // Check navigation goal
            if (trials && goal &&
                !trials.Completed &&
                trials.IsRunning)
            {
                Vector3 a = transform.position;
                Vector3 b = goal.position;

                a.y = 0f;
                b.y = 0f;

                if (Vector3.Distance(a, b) < goalRadius)
                {
                    trials.CompleteTrial();
                }
            }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!trials ||
                !trials.IsRunning ||
                hit.collider.gameObject.name == "Ground")
                return;

            if (Time.time - lastCollisionTime >= 0.80f)
            {
                trials.AddCollision();
                lastCollisionTime = Time.time;
            }
        }
    }
}
