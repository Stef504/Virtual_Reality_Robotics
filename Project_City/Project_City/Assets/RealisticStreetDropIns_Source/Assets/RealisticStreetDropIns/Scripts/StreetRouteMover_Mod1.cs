using System.Collections;
using UnityEngine;

namespace RealisticStreetDropIns
{
    public enum IntersectionAction { ContinueStraight, TurnLeft, TurnRight }

    /// <summary>Background-only route mover with intersection turning and stopping capabilities.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetRouteMover_Mod : MonoBehaviour
    {
        [Header("Travel relative to how you place this prefab")]
        [Min(0.1f)] public float distance = 15f;
        [Min(0.01f)] public float speed = 1.35f;
        public Vector3 localDirection = Vector3.forward;
        [Tooltip("Loop: warp back to the start at the end. Off: turn around and walk back.")]
        public bool loopToStart = false;
        [Min(0f)] public float initialDelay = 0f;
        [Tooltip("Turn the whole object to face movement direction.")]
        public bool turnToFaceDirection = true;
        [Range(0f, 1080f)] public float turnSpeedDegrees = 540f;

        private Vector3 origin, direction;
        private Quaternion initialRotation;
        private float progress, delay;
        private int sign = 1;
        private bool initialized;
        private bool isStopped = false;

        /// <summary>Guards against executing multiple turning coroutines simultaneously.</summary>
        public bool IsTurning { get; private set; } = false;
        public float CurrentSpeed { get; private set; }

        // --- INTERSECTION & SENSOR CONTROLS ---
        public void PauseVehicle() => isStopped = true;
        public void ResumeVehicle() => isStopped = false;

        public IntersectionAction preferredTurn = IntersectionAction.TurnLeft;

        void OnEnable() { initialized = false; }

        void Start()
        {
            InitializeSegment(transform.position, transform.rotation, localDirection, distance);
            delay = initialDelay;
            initialized = true;
        }

        /// <summary>Resets origin and heading for a new road segment after turning at an intersection.</summary>
        public void InitializeSegment(Vector3 startPos, Quaternion startRot, Vector3 localDir, float newDistance)
        {
            origin = startPos;
            initialRotation = startRot;
            distance = newDistance;
            Vector3 d = localDir.sqrMagnitude > 0.0001f ? localDir.normalized : Vector3.forward;
            direction = initialRotation * d;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            direction.Normalize();
            progress = 0f;
            sign = 1;
        }

        void Update()
        {
    
            if (!initialized || !isActiveAndEnabled || isStopped) return;
            if (delay > 0f) { delay -= Time.deltaTime; CurrentSpeed = 0f; return; }

            float step = speed * Time.deltaTime;
            progress += step * sign;

            if (progress >= distance)
            {
                progress = loopToStart ? 0f : distance;
                if (!loopToStart) sign = -1;
            }
            else if (progress <= 0f && sign < 0)
            {
                progress = 0f;
                sign = 1;
            }

            Vector3 next = origin + direction * progress;
            transform.position = next;
            CurrentSpeed = sign * speed;

            if (turnToFaceDirection && !loopToStart)
            {
                Quaternion target = Quaternion.LookRotation(direction * sign, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeedDegrees * Time.deltaTime);
            }
        }


        /// <summary>Pauses the car at an intersection, turns it, and resumes movement down the next street.</summary>
        /// <summary>Executes a smooth radial arc turn into the perpendicular lane.</summary>
        public IEnumerator StopWaitAndTurn(float stopDuration, IntersectionAction action, float nextSegmentDistance, float turnRadius)
        {
            // Guard: Ignore if already turning
            if (IsTurning) yield break;

            // DIAGNOSTIC LOG 1: Check if coroutine is triggered at all
            Debug.Log($"[INTERSECTION ENTERED] {gameObject.name} received Action: {action}");
    
            IsTurning = true;
            isStopped = true;
            CurrentSpeed = 0f;

            // 1. Wait at intersection (e.g. Red Light / Stop Sign)
            if (stopDuration > 0f)
            {
                yield return new WaitForSeconds(stopDuration);
            }

            // 2. Perform Kinematic Arc Turn if turning Left or Right
            if (action == IntersectionAction.TurnLeft || action == IntersectionAction.TurnRight)
            {
                float turnAngle = (action == IntersectionAction.TurnRight) ? 90f : -90f;
                float dirSign = (action == IntersectionAction.TurnRight) ? 1f : -1f;

                float safeRadius = Mathf.Max(0.5f, turnRadius);
                float angularSpeedDeg = (speed / safeRadius) * Mathf.Rad2Deg;
                float totalTime = Mathf.Abs(turnAngle) / angularSpeedDeg;

                Vector3 lastPos = transform.position;
                Quaternion startRotation = transform.rotation;

                float elapsed = 0f;
                while (elapsed < totalTime)
                {
                    float dt = Time.deltaTime;
                    elapsed += dt;

                    // Move forward along current heading while rotating
                    transform.position += transform.forward * speed * dt;
                    transform.Rotate(0f, dirSign * angularSpeedDeg * dt, 0f);

                    // Paints a magenta trail in Scene View showing the exact turning curve
                    Debug.DrawLine(lastPos, transform.position, Color.magenta, 10.0f);
                    lastPos = transform.position;

                    CurrentSpeed = speed;
                    yield return null;
                }
            

                // Measure total angular difference between start of turn and end of turn
                float completedAngle = Quaternion.Angle(startRotation, transform.rotation);
                Debug.Log($"[ARC TURN COMPLETE] {gameObject.name} completed turn. Total Rotated Angle: {completedAngle:F2}°");
            }
                // 3. Reset origin and direction heading for the new street lane
                InitializeSegment(transform.position, transform.rotation, localDirection, nextSegmentDistance);

                // 4. Resume straight-line Update movement
                isStopped = false;
                IsTurning = false;
            }
        void OnDrawGizmosSelected()
        {
            Vector3 start;
            Vector3 d;
            if (Application.isPlaying && initialized) { start = origin; d = direction; }
            else
            {
                start = transform.position;
                d = transform.rotation * (localDirection.sqrMagnitude < 0.0001f ? Vector3.forward : localDirection.normalized);
                d.y = 0f;
                d = d.sqrMagnitude < 0.0001f ? Vector3.forward : d.normalized;
            }
            Gizmos.color = new Color(0f, 0.85f, 1f, .9f);
            Gizmos.DrawLine(start, start + d * distance);
            Gizmos.DrawWireSphere(start, .18f);
            Gizmos.DrawWireSphere(start + d * distance, .18f);
        }
    }
}