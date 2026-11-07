using System.Collections.Generic;
using UnityEngine;

namespace TeleopStudentModules
{
    // Uses EXISTING robot, cameras and optional top-down camera. Does not drive anything.
    public sealed class SceneTeleopDataSource : TeleopDataSource
    {
        [Header("Drag objects from YOUR scene")]
        public Transform huskyRoot;
        public Transform robotPoseFrame;
        public Camera frontCamera;
        public Camera rearCamera;
        public Camera topDownCamera;

        [Header("Use existing camera RenderTextures, if supplied")]
        public Texture frontTextureOverride;
        public Texture rearTextureOverride;
        public Texture topDownTextureOverride;

        [Header("Local obstacle raycasts (simple prototype, NOT LiDAR)")]
        public bool enableRaycastProximity = true;
        public LayerMask obstacleLayers = ~0;
        [Min(0.05f)] public float sensorHeight = 0.45f;
        [Min(0.2f)] public float maximumRangeMeters = 3f;
        [Tooltip("Horizontal angles relative to robot-forward. Positive angles face to robot's right.")]
        public float[] leftRayAnglesDegrees = { -30f, -55f, -85f };
        public float[] rightRayAnglesDegrees = { 30f, 55f, 85f };

        [Header("Annotated scene objects (not detected automatically)")]
        public bool includeManualSemanticLabels = true;
        [Min(0.1f)] public float refreshLabelsSeconds = 1f;

        readonly List<SemanticSceneLabel> labels = new List<SemanticSceneLabel>();
        float nextLabelScan;

        public override void Fill(TeleopFrame dst)
        {
            dst.sourceLabel = "UNITY SIMULATION";
            dst.sourceDetail = "Scene references; map is a camera view; proximity is raycast-based";

            Transform pose = robotPoseFrame != null ? robotPoseFrame : huskyRoot;
            if (pose != null)
            {
                dst.poseAvailable = true;
                dst.robotWorldPosition = pose.position;
                dst.robotForward = pose.forward;
                dst.robotYawDegrees = pose.eulerAngles.y;
            }

            dst.frontImage = frontTextureOverride != null ? frontTextureOverride :
                (frontCamera != null ? frontCamera.targetTexture : null);
            dst.rearImage = rearTextureOverride != null ? rearTextureOverride :
                (rearCamera != null ? rearCamera.targetTexture : null);
            dst.mapImage = topDownTextureOverride != null ? topDownTextureOverride :
                (topDownCamera != null ? topDownCamera.targetTexture : null);

            // Only show the robot marker when map texture is from the specified map camera.
            if (pose != null && topDownCamera != null && dst.mapImage != null &&
                dst.mapImage == topDownCamera.targetTexture)
            {
                Vector3 p = topDownCamera.WorldToViewportPoint(pose.position);
                Vector3 ahead = topDownCamera.WorldToViewportPoint(pose.position + pose.forward);
                Vector2 delta = new Vector2(ahead.x - p.x, ahead.y - p.y);
                if (p.z > 0 && delta.sqrMagnitude > 0.000001f)
                {
                    dst.mapMarkerAvailable = true;
                    dst.mapMarkerUV = new Vector2(p.x, p.y);
                    dst.mapMarkerDegrees = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
                }
            }

            if (enableRaycastProximity && pose != null)
            {
                Vector3 origin = pose.position + Vector3.up * sensorHeight;
                dst.leftDistanceMeters = FindMinimumDistance(pose, origin, leftRayAnglesDegrees);
                dst.rightDistanceMeters = FindMinimumDistance(pose, origin, rightRayAnglesDegrees);
                dst.proximityAvailable = true;
            }

            if (includeManualSemanticLabels)
            {
                if (Time.unscaledTime >= nextLabelScan)
                {
                    labels.Clear();
                    labels.AddRange(FindObjectsOfType<SemanticSceneLabel>());
                    nextLabelScan = Time.unscaledTime + Mathf.Max(0.1f, refreshLabelsSeconds);
                }
                foreach (SemanticSceneLabel label in labels)
                {
                    if (label == null || !label.isActiveAndEnabled) continue;
                    dst.objects.Add(new ObservedObject(label.objectLabel,
                        label.WorldLabelPosition, label.manuallyMarkedMoving, "manual scene label"));
                }
            }
        }

        float FindMinimumDistance(Transform pose, Vector3 origin, float[] angles)
        {
            float nearest = maximumRangeMeters;
            if (angles == null) return nearest;
            foreach (float angle in angles)
            {
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * pose.forward;
                RaycastHit[] hits = Physics.RaycastAll(origin, dir, maximumRangeMeters,
                    obstacleLayers, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits)
                {
                    if (huskyRoot != null && hit.collider.transform.IsChildOf(huskyRoot)) continue;
                    if (hit.distance < nearest) nearest = hit.distance;
                }
            }
            return nearest;
        }
    }
}
