using System.Collections;
using UnityEngine;
using RealisticStreetDropIns;

public class IntersectionZone : MonoBehaviour
{
    [Header("Intersection Rules")]
    [Tooltip("How long (in seconds) the car stops at the intersection before turning/continuing.")]
    public float stopDuration = 2.5f;

    [Tooltip("Turn decision when reaching this zone.")]
    public IntersectionAction turnAction = IntersectionAction.TurnRight;

    [Tooltip("Radius (in meters) of the turning arc. Match this to your road lane width.")]
    public float turnRadius = 4.5f;

    [Tooltip("Distance (in meters) for the car to drive down the new road after turning.")]
    public float nextRoadDistance = 35.0f;

    private void OnTriggerEnter(Collider other)
    {
        // LOG 1: Tests if Unity Physics triggers on contact
        Debug.Log($"[PHYSICS TRIGGER] {other.gameObject.name} touched intersection {gameObject.name}");
        StreetRouteMover_Mod mover = other.GetComponentInParent<StreetRouteMover_Mod>();

        if (mover != null)
        {
            // Reads the specific car's preferred turn setting
            IntersectionAction action = mover.preferredTurn;
            // LOG 2: Confirms StreetRouteMover was found and coroutine was called
            Debug.Log($"[CALLING STOP WAIT TURN] Calling mover on {mover.gameObject.name}");
            mover.StartCoroutine(mover.StopWaitAndTurn(stopDuration, action, nextRoadDistance, turnRadius));
        }
    }
}