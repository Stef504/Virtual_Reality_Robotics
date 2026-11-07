using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DriveReadyCoupe
{
    // Stable arcade car. Intentionally DOES NOT use WheelCollider.
    // Wheels are visual-only and stay centered on their own pivots.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public class DriveReadyCarController : MonoBehaviour
    {
        [Header("W/S drive, A/D steer, Space brake, R reset")]
        [Min(0)] public float forwardAcceleration = 12f;
        [Min(0)] public float reverseAcceleration = 7f;
        [Min(1)] public float topSpeedKmh = 85f;
        [Range(0, 180)] public float turnRate = 95f;
        [Range(0, 40)] public float sidewaysGrip = 14f;
        [Range(0, 8)] public float coastingDrag = 1.2f;
        [Range(0, 30)] public float brakeStrength = 12f;
        [Range(0, 40)] public float frontWheelSteerAngle = 28f;
        [Min(0.01f)] public float wheelRadius = 0.368f;

        [Header("Automatically wired by the prefab builder")]
        public Transform[] steerPivots = new Transform[2];
        public Transform[] spinPivots = new Transform[4];

        private Rigidbody body;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float throttleInput;
        private float steerInput;
        private bool brakeInput;
        private float wheelSpinDegrees;
        private float displayedSteer;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void Update()
        {
            throttleInput = (Held(KeyCode.W) || Held(KeyCode.UpArrow) ? 1f : 0f)
                          - (Held(KeyCode.S) || Held(KeyCode.DownArrow) ? 1f : 0f);
            steerInput = (Held(KeyCode.D) || Held(KeyCode.RightArrow) ? 1f : 0f)
                       - (Held(KeyCode.A) || Held(KeyCode.LeftArrow) ? 1f : 0f);
            brakeInput = Held(KeyCode.Space);
            if (Pressed(KeyCode.R)) ResetCar();

            displayedSteer = Mathf.MoveTowards(displayedSteer,
                steerInput * frontWheelSteerAngle, Time.deltaTime * 160f);
            if (steerPivots != null)
                foreach (Transform pivot in steerPivots)
                    if (pivot) pivot.localRotation = Quaternion.Euler(0f, displayedSteer, 0f);

            if (body != null && spinPivots != null)
            {
                float signedSpeed = Vector3.Dot(body.velocity, transform.forward);
                wheelSpinDegrees += signedSpeed / wheelRadius * Mathf.Rad2Deg * Time.deltaTime;
                foreach (Transform pivot in spinPivots)
                    if (pivot) pivot.localRotation = Quaternion.Euler(wheelSpinDegrees, 0f, 0f);
            }
        }

        private void FixedUpdate()
        {
            if (!body || body.isKinematic) return;
            Vector3 velocity = body.velocity;
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            float lateralSpeed = Vector3.Dot(velocity, transform.right);
            float maxSpeed = topSpeedKmh / 3.6f;
            bool onGround = Physics.Raycast(transform.position + transform.up * .75f,
                -transform.up, 1.6f, ~0, QueryTriggerInteraction.Ignore);

            // Tire-like sideways grip, without any WheelCollider scale/pivot issues.
            if (onGround)
            {
                body.AddForce(-transform.right * lateralSpeed * sidewaysGrip,
                    ForceMode.Acceleration);
                if (Mathf.Abs(throttleInput) > .01f && !brakeInput &&
                    (Mathf.Abs(forwardSpeed) < maxSpeed || Mathf.Sign(throttleInput) != Mathf.Sign(forwardSpeed)))
                {
                    float acceleration = throttleInput > 0f ? forwardAcceleration : reverseAcceleration;
                    body.AddForce(transform.forward * acceleration * throttleInput,
                        ForceMode.Acceleration);
                }
                if (brakeInput)
                    body.AddForce(-transform.forward * forwardSpeed * brakeStrength,
                        ForceMode.Acceleration);
                else if (Mathf.Abs(throttleInput) < .01f)
                    body.AddForce(-transform.forward * forwardSpeed * coastingDrag,
                        ForceMode.Acceleration);

                // Steering only while rolling, reverse steering reverses naturally.
                float direction = Mathf.Sign(forwardSpeed);
                float rolling = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 2.8f);
                float rateScale = Mathf.Lerp(1f, .40f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 22f));
                float yaw = steerInput * direction * turnRate * rateScale * rolling * Time.fixedDeltaTime;
                body.MoveRotation(body.rotation * Quaternion.Euler(0f, yaw, 0f));
            }
        }

        private void ResetCar()
        {
            if (!body) return;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = spawnPosition + Vector3.up * 0.65f;
            body.rotation = Quaternion.Euler(0f, spawnRotation.eulerAngles.y, 0f);
        }

        private static bool Held(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            switch (key)
            {
                case KeyCode.W: return kb.wKey.isPressed;
                case KeyCode.S: return kb.sKey.isPressed;
                case KeyCode.A: return kb.aKey.isPressed;
                case KeyCode.D: return kb.dKey.isPressed;
                case KeyCode.Space: return kb.spaceKey.isPressed;
                case KeyCode.R: return kb.rKey.isPressed;
                case KeyCode.UpArrow: return kb.upArrowKey.isPressed;
                case KeyCode.DownArrow: return kb.downArrowKey.isPressed;
                case KeyCode.LeftArrow: return kb.leftArrowKey.isPressed;
                case KeyCode.RightArrow: return kb.rightArrowKey.isPressed;
                default: return false;
            }
#else
            return false;
#endif
        }

        private static bool Pressed(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#elif ENABLE_INPUT_SYSTEM
            return key == KeyCode.R && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return false;
#endif
        }
    }
}
