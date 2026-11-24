using UnityEngine;
using RealisticStreetDropIns;

namespace RealisticStreetDropIns
{
    /// <summary>
    /// Shoots a forward raycast from the vehicle bumper. 
    /// Pauses StreetRouteMover when a car is detected ahead.
    /// </summary>
    [RequireComponent(typeof(StreetRouteMover_Mod))]
    public class CarFollowingSensor : MonoBehaviour
    {
        [Header("Sensor Configuration")]
        [Tooltip("Distance (in meters) to detect cars ahead and stop.")]
        public float followDistance = 4.0f;

        [Tooltip("Height offset above the ground to shoot the forward raycast.")]
        public float sensorHeight = 0.5f;

        [Tooltip("Distance forward from the car center to the front bumper.")]
        public float frontBumperOffset = 6.0f;

        [Tooltip("Layer mask assigned to vehicle colliders.")]
        public LayerMask vehicleLayers;

        private StreetRouteMover_Mod mover;

        private bool isBlockedByCarAhead = false;

        void Start()
        {
            mover = GetComponent<StreetRouteMover_Mod>();
            if (mover == null)
            {
                Debug.LogError($"[CarFollowSensor] Missing StreetRouteMover on {gameObject.name}!");
            }
        }

        void Update()
        {
            if (!mover) return;

            Vector3 origin = transform.position 
                            + (Vector3.up * sensorHeight) 
                            + (transform.forward * frontBumperOffset);
            Vector3 forward = transform.forward;

            // 2. Perform Raycast 
            bool hitSomething = Physics.Raycast(
                origin, 
                forward, 
                out RaycastHit hit, 
                followDistance, 
                vehicleLayers, 
                QueryTriggerInteraction.Collide
            );

            // 3. Verify hit object is NOT part of this car's own object hierarchy
            if (hitSomething && !hit.collider.transform.IsChildOf(transform))
            {
                // Draw Red line in Scene View up to the hit point
                Debug.DrawRay(origin, forward * hit.distance, Color.red, 0.1f);

                if (!isBlockedByCarAhead)
                {
                    isBlockedByCarAhead = true;
                    mover.PauseVehicle();
                }
            }
            else
            {
                // Clear path: Draw Yellow line in Scene View
                Debug.DrawRay(origin, forward * followDistance, Color.yellow, 0.1f);

                if (isBlockedByCarAhead)
                {
                    isBlockedByCarAhead = false;
                    mover.ResumeVehicle();
                }
            }
        }
        void OnDrawGizmosSelected()
        {
            // Visual green/red sensor debug line in Scene View
            Vector3 origin = transform.position + (Vector3.up * sensorHeight);
            Gizmos.color = isBlockedByCarAhead ? Color.red : Color.green;
            Gizmos.DrawLine(origin, origin + (transform.forward * followDistance));
            Gizmos.DrawWireSphere(origin + (transform.forward * followDistance), 0.15f);
        }
    }
}