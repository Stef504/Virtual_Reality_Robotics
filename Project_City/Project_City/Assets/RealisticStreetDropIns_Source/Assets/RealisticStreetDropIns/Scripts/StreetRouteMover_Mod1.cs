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

        [Header("End of Route Action")]
        [Tooltip("What happens when the object reaches the end of its path distance.")]
        public RouteEndBehavior endBehavior = RouteEndBehavior.LoopToStart;

        [Header("Timing & Turning")]
        [Min(0f)] public float initialDelay = 0f;
        [Tooltip("Turn the whole object to face movement direction.")]
        public bool turnToFaceDirection = true;
        [Range(0f, 1080f)] public float turnSpeedDegrees = 540f;

        [Tooltip("Distance (meters) to drive straight INTO junction before banking into the curve.")]
        public float entryForwardDistance = 3f;

        private Vector3 origin, direction;
        private Quaternion initialRotation;
        private float progress, delay;
        private int sign = 1;
        private bool initialized;
        private bool isStopped = false;

        // Saved Scene Spawn References (For Loop To Start)
        private Vector3 initialSpawnPosition;
        private Quaternion initialSpawnRotation;
        private float initialSpawnDistance;
        private RouteEndBehavior initialEndBehavior;


        /// <summary>Guards against executing multiple turning coroutines simultaneously.</summary>
        public bool IsTurning { get; private set; } = false;
        public float CurrentSpeed { get; private set; }

        // --- INTERSECTION & SENSOR CONTROLS ---
        public void PauseVehicle() => isStopped = true;
        public void ResumeVehicle() => isStopped = false;

        public IntersectionAction preferredTurn = IntersectionAction.TurnLeft;


        void Start()
        {
            // Cache original scene placement
            initialSpawnPosition = transform.position;
            initialSpawnRotation = transform.rotation;
            initialSpawnDistance = distance;

            InitializeSegment(transform.position, transform.rotation, localDirection, distance);
            delay = initialDelay;
            initialized = true;
        }

        void Awake()
        {
            // Cache original scene placement before any script toggling occurs
            initialSpawnPosition = transform.position;
            initialSpawnRotation = transform.rotation;
            initialSpawnDistance = distance;
        }

        void OnEnable()
        {
            // Resets vehicle position and state whenever the scenario parent is set to Active (true)
            transform.position = initialSpawnPosition;
            transform.rotation = initialSpawnRotation;
            IsTurning = false;
            isStopped = false;
            
            InitializeSegment(initialSpawnPosition, initialSpawnRotation, localDirection, initialSpawnDistance);
            delay = initialDelay;
            initialized = true;
        }

        void OnDisable()
        {
            // Cleanly stops all turning coroutines when the scenario parent is set to Active (false)
            StopAllCoroutines();
            IsTurning = false;
            isStopped = true;
            CurrentSpeed = 0f;
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
                switch (endBehavior)
                {
                    case RouteEndBehavior.LoopToStart:
                        transform.position = initialSpawnPosition;
                        transform.rotation = initialSpawnRotation;
                        InitializeSegment(initialSpawnPosition, initialSpawnRotation, localDirection, initialSpawnDistance);
                        return;

                    case RouteEndBehavior.Reverse:
                        progress = distance;
                        sign = -1;
                        break;

                    case RouteEndBehavior.Stop:
                        progress = distance;
                        isStopped = true;
                        CurrentSpeed = 0f;
                        return;

                    case RouteEndBehavior.Despawn:
                        isStopped = true;
                        CurrentSpeed = 0f;
                        gameObject.SetActive(false); // Disables character from scene
                        return;
                }
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
        public IEnumerator StopWaitAndTurn(
            float stopDuration,
            IntersectionAction action,
            float turnRadius,
            float newSegmentDistance = 0f,
            RouteEndBehavior? newEndBehavior = null
            )
        {
            if (IsTurning) yield break;

            IsTurning = true;
            isStopped = true;
            CurrentSpeed = 0f;

            // 1. Wait at the intersection (Stop sign / Red light)
            if (stopDuration > 0f)
            {
                yield return new WaitForSeconds(stopDuration);
            }

            // 2. Drive straight forward into the intersection center before turning
            if (entryForwardDistance > 0f)
            {
                float driven = 0f;
                while (driven < entryForwardDistance)
                {
                    float step = speed * Time.deltaTime;
                    driven += step;
                    transform.position += transform.forward * step;
                    CurrentSpeed = speed;
                    yield return null;
                }
            }

            // 3. Perform Closed-Loop Kinematic Arc Turn
            if (action == IntersectionAction.TurnLeft || action == IntersectionAction.TurnRight)
            {
                float turnAngle = (action == IntersectionAction.TurnRight) ? 90f : -90f;
                float safeRadius = Mathf.Max(0.5f, turnRadius);
                float angularSpeedDeg = (speed / safeRadius) * Mathf.Rad2Deg;

                Quaternion startRotation = transform.rotation;
                Quaternion targetRotation = startRotation * Quaternion.Euler(0f, turnAngle, 0f);

                Vector3 lastPos = transform.position;

                while (Quaternion.Angle(transform.rotation, targetRotation) > 0.05f)
                {
                    float dt = Time.deltaTime;

                    transform.position += transform.forward * speed * dt;
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        angularSpeedDeg * dt
                    );

                    Debug.DrawLine(lastPos, transform.position, Color.magenta, 10.0f);
                    lastPos = transform.position;

                    CurrentSpeed = speed;
                    yield return null;
                }

                transform.rotation = targetRotation;
            }

            // 4. Re-initialize baseline segment for the new road lane
            InitializeSegment(transform.position, transform.rotation, localDirection, distance);

            // Update end-of-route behavior (e.g. Despawn or Stop)
            if (newEndBehavior.HasValue)
            {
                endBehavior = newEndBehavior.Value;
            }

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