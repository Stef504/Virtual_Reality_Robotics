using UnityEngine;

namespace TeleopLab
{
    // Synthetic raycasts for interface testing; do not call them LiDAR measurements.
    public class DemoProximity : MonoBehaviour
    {
        public LayerMask obstacleMask = ~0;
        public float scanDistance = 2.0f;
        public float rayHeight = 0.38f;
        public float threshold = 0.85f; // Published paper threshold
        public float stopDistance = 0.50f; // Published paper maximum vibration distance
        public float LeftDistance { get; private set; }
        public float RightDistance { get; private set; }
        public float LeftUrgency { get { return ComputeIntensity(LeftDistance); } }
        public float RightUrgency { get { return ComputeIntensity(RightDistance); } }

        void Update()
        {
            LeftDistance = Mathf.Min(Cast(-25), Cast(-55), Cast(-90));
            RightDistance = Mathf.Min(Cast(25), Cast(55), Cast(90));
        }

        float Cast(float angle)
        {
            Vector3 origin = transform.position + Vector3.up * rayHeight;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            RaycastHit hit;
            return Physics.Raycast(origin, dir, out hit, scanDistance, obstacleMask, QueryTriggerInteraction.Ignore)
                ? hit.distance : scanDistance;
        }

        float ComputeIntensity(float distance)
        {
            if (distance >= threshold) return 0f;
            if (distance <= stopDistance) return 1f;
            return (threshold - distance) / (threshold - stopDistance);
        }
    }
}
