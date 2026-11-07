using System.Collections.Generic;
using UnityEngine;

namespace TeleopStudentModules
{
    // A MAIN-THREAD interface for a future ROS 2 bridge (no ROS package dependency).
    // The project does not create ROS subscriptions automatically.
    public sealed class ExternalTeleopDataSource : TeleopDataSource
    {
        [Min(0.1f)] public float poseTimeoutSeconds = 1f;
        [Min(0.1f)] public float imageTimeoutSeconds = 2f;
        [Min(0.1f)] public float mapTimeoutSeconds = 5f;
        [Min(0.1f)] public float proximityTimeoutSeconds = 1f;
        [Min(0.1f)] public float semanticTimeoutSeconds = 2f;

        Vector3 pos, heading;
        float yaw;
        Texture front, rear, map;
        bool hasMapMarker;
        Vector2 markerUV;
        float markerDegrees, left, right;
        float poseStamp=-99999f, frontStamp=-99999f, rearStamp=-99999f,
              mapStamp=-99999f, proximityStamp=-99999f, objectsStamp=-99999f;
        readonly List<ObservedObject> labels = new List<ObservedObject>();

        public void PushPose(Vector3 worldPosition, Vector3 worldForward, float yawDegrees)
        {
            pos = worldPosition; heading = worldForward; yaw = yawDegrees;
            poseStamp = Time.unscaledTime;
        }
        public void PushCameras(Texture frontTexture, Texture rearTexture)
        {
            if (frontTexture != null) { front = frontTexture; frontStamp = Time.unscaledTime; }
            if (rearTexture != null) { rear = rearTexture; rearStamp = Time.unscaledTime; }
        }
        public void PushMap(Texture mapTexture, bool markerValid, Vector2 markerNormalizedUV, float headingDegrees)
        {
            map = mapTexture; mapStamp = Time.unscaledTime;
            hasMapMarker = markerValid;
            markerUV = markerNormalizedUV;
            markerDegrees = headingDegrees;
        }
        public void PushProximity(float leftMeters, float rightMeters)
        {
            left = Mathf.Max(0f, leftMeters); right = Mathf.Max(0f, rightMeters);
            proximityStamp = Time.unscaledTime;
        }
        public void PushSemanticObjects(IEnumerable<ObservedObject> observations)
        {
            labels.Clear();
            if (observations != null) labels.AddRange(observations);
            objectsStamp = Time.unscaledTime;
        }
        public override void Fill(TeleopFrame dst)
        {
            float t = Time.unscaledTime;
            dst.sourceLabel = "EXTERNAL DATA";
            dst.sourceDetail = "Push methods require an explicit ROS/other adapter";
            if (t - poseStamp <= poseTimeoutSeconds)
            {
                dst.poseAvailable = true;
                dst.robotWorldPosition = pos;
                dst.robotForward = heading;
                dst.robotYawDegrees = yaw;
            }
            if (t-frontStamp <= imageTimeoutSeconds) dst.frontImage = front;
            if (t-rearStamp <= imageTimeoutSeconds) dst.rearImage = rear;
            if (t-mapStamp <= mapTimeoutSeconds)
            {
                dst.mapImage = map;
                dst.mapMarkerAvailable = hasMapMarker && map != null;
                dst.mapMarkerUV = markerUV;
                dst.mapMarkerDegrees = markerDegrees;
            }
            if (t-proximityStamp <= proximityTimeoutSeconds)
            {
                dst.proximityAvailable = true;
                dst.leftDistanceMeters = left;
                dst.rightDistanceMeters = right;
            }
            if (t-objectsStamp <= semanticTimeoutSeconds) dst.objects.AddRange(labels);
        }
    }
}
