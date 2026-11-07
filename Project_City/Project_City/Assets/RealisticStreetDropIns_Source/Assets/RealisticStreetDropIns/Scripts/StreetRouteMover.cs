using UnityEngine;

namespace RealisticStreetDropIns
{
    /// <summary>Background-only, animation-friendly straight route. No NavMesh or vehicle physics required.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetRouteMover : MonoBehaviour
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
        public float CurrentSpeed { get; private set; }

        void OnEnable() { initialized = false; }

        void Start()
        {
            origin = transform.position;
            initialRotation = transform.rotation;
            Vector3 d = localDirection.sqrMagnitude > 0.0001f ? localDirection.normalized : Vector3.forward;
            direction = initialRotation * d;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            direction.Normalize();
            progress = 0f;
            delay = initialDelay;
            sign = 1;
            CurrentSpeed = 0;
            initialized = true;
        }

        void Update()
        {
            if (!initialized || !isActiveAndEnabled) return;
            if (delay > 0f) { delay -= Time.deltaTime; CurrentSpeed = 0f; return; }
            float step = speed * Time.deltaTime;
            progress += step * sign;
            if (progress >= distance) { progress = loopToStart ? 0f : distance; if (!loopToStart) sign = -1; }
            else if (progress <= 0f && sign < 0) { progress = 0f; sign = 1; }
            Vector3 next = origin + direction * progress;
            transform.position = next;
            CurrentSpeed = sign * speed;
            if (turnToFaceDirection && !loopToStart)
            {
                Quaternion target = Quaternion.LookRotation(direction * sign, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeedDegrees * Time.deltaTime);
            }
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
