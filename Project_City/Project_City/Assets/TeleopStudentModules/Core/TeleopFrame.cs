using System.Collections.Generic;
using UnityEngine;

namespace TeleopStudentModules
{
    // A single read-only-in-practice snapshot used by all four student modules.
    // Populate only real observations or explicitly labeled sample values.
    public sealed class TeleopFrame
    {
        public bool poseAvailable;
        public Vector3 robotWorldPosition;
        public Vector3 robotForward;
        public float robotYawDegrees;

        public Texture frontImage;
        public Texture rearImage;
        public Texture mapImage;

        public bool mapMarkerAvailable;
        public Vector2 mapMarkerUV;
        public float mapMarkerDegrees; // 0 = marker triangle faces upward

        public bool proximityAvailable;
        public float leftDistanceMeters;
        public float rightDistanceMeters;

        public bool isSampleData;
        public string sourceLabel = "UNCONFIGURED";
        public string sourceDetail = "";

        public readonly List<ObservedObject> objects = new List<ObservedObject>();

        public void Clear()
        {
            poseAvailable = false;
            robotWorldPosition = Vector3.zero;
            robotForward = Vector3.forward;
            robotYawDegrees = 0f;
            frontImage = rearImage = mapImage = null;
            mapMarkerAvailable = false;
            mapMarkerUV = Vector2.zero;
            mapMarkerDegrees = 0f;
            proximityAvailable = false;
            leftDistanceMeters = rightDistanceMeters = 0f;
            isSampleData = false;
            sourceLabel = "UNCONFIGURED";
            sourceDetail = "";
            objects.Clear();
        }
    }

    public struct ObservedObject
    {
        public string label;
        public Vector3 worldPosition;
        public bool isMoving; // Must be provided by a labeled data source, NOT inferred by the UI.
        public string provenance; // e.g. "manually tagged", "mock", "ROS adapter"

        public ObservedObject(string label, Vector3 worldPosition, bool isMoving, string provenance)
        {
            this.label = label;
            this.worldPosition = worldPosition;
            this.isMoving = isMoving;
            this.provenance = provenance;
        }
    }
}
