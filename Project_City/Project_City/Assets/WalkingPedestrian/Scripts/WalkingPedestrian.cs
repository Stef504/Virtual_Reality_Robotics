using UnityEngine;

namespace TeleopPedestrian
{
    /// <summary>
    /// Self-contained walking pedestrian for Unity 2022.3.
    /// Moves between start/end positions and animates primitive-model limbs.
    /// Does not modify the Husky, its physics, or keyboard controls.
    /// </summary>
    public class WalkingPedestrian : MonoBehaviour
    {
        [Header("Walking route (local to the starting pose)")]
        [Min(0.1f)] public float pathLength = 5f;
        [Min(0.01f)] public float speed = 0.8f;
        public bool startWalking = true;
        public bool pingPong = true;
        public bool showPathGizmo = true;

        [Header("Walking animation")]
        public Transform leftLegPivot;
        public Transform rightLegPivot;
        public Transform leftArmPivot;
        public Transform rightArmPivot;
        [Range(0f, 50f)] public float legSwingDegrees = 28f;
        [Range(0f, 50f)] public float armSwingDegrees = 18f;
        [Min(0.1f)] public float stepsPerSecond = 1.6f;

        private Vector3 origin;
        private Quaternion startingRotation;
        private float pathDistance;
        private int walkingDirection = 1;
        private float phase;
        private bool isWalking;

        void Start()
        {
            origin = transform.position;
            startingRotation = transform.rotation;
            isWalking = startWalking;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (isWalking)
            {
                pathDistance += walkingDirection * speed * dt;
                if (pathDistance >= pathLength)
                {
                    pathDistance = pathLength;
                    if (pingPong) walkingDirection = -1;
                    else isWalking = false;
                }
                else if (pathDistance <= 0f && walkingDirection < 0)
                {
                    pathDistance = 0f;
                    walkingDirection = 1;
                }

                phase += dt * stepsPerSecond * Mathf.PI * 2f;
            }

            // Route follows the original local forward (+Z) direction.
            transform.position = origin + (startingRotation * Vector3.forward) * pathDistance;
            transform.rotation = startingRotation * Quaternion.Euler(0f, walkingDirection < 0 ? 180f : 0f, 0f);

            float stride = isWalking ? Mathf.Sin(phase) : 0f;
            if (leftLegPivot) leftLegPivot.localRotation = Quaternion.Euler(stride * legSwingDegrees, 0f, 0f);
            if (rightLegPivot) rightLegPivot.localRotation = Quaternion.Euler(-stride * legSwingDegrees, 0f, 0f);
            if (leftArmPivot) leftArmPivot.localRotation = Quaternion.Euler(-stride * armSwingDegrees, 0f, 0f);
            if (rightArmPivot) rightArmPivot.localRotation = Quaternion.Euler(stride * armSwingDegrees, 0f, 0f);
        }

        public void SetWalking(bool active) { isWalking = active; }

        void OnDrawGizmosSelected()
        {
            if (!showPathGizmo) return;
            Vector3 start = Application.isPlaying ? origin : transform.position;
            Quaternion rot = Application.isPlaying ? startingRotation : transform.rotation;
            Gizmos.color = Color.cyan;
            Vector3 end = start + rot * Vector3.forward * pathLength;
            Gizmos.DrawLine(start + Vector3.up * 0.06f, end + Vector3.up * 0.06f);
            Gizmos.DrawWireSphere(start, 0.12f);
            Gizmos.DrawWireSphere(end, 0.12f);
        }
    }
}
