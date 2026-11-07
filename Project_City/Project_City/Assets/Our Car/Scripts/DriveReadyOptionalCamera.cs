using UnityEngine;

namespace DriveReadyCoupe
{
    // Optional: add to an EXISTING camera, then assign Target to the vehicle.
    // No new camera or audio listener is created by the package.
    public class DriveReadyOptionalCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 localOffset = new Vector3(0, 3.0f, -7.8f);
        public float followSmoothness = 6f;
        public float lookHeight = 0.95f;

        private void LateUpdate()
        {
            if (!target) return;
            Vector3 desiredPosition = target.TransformPoint(localOffset);
            float t = 1f - Mathf.Exp(-followSmoothness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(target.position + Vector3.up * lookHeight - transform.position,
                    Vector3.up), t);
        }
    }
}
