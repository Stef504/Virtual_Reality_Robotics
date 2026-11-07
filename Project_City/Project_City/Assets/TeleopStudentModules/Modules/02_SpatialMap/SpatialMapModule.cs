using UnityEngine;
using UnityEngine.UI;

namespace TeleopStudentModules
{
    // STUDENT 2 owns this file and its generated panel/prefab.
    // Map is a top-down CAMERA VIEW initially. It is NOT SLAM or occupancy mapping.
    public sealed class SpatialMapModule : MonoBehaviour
    {
        public TeleopStudentContext context;
        public RawImage mapImage;
        public RectTransform marker;
        public Text coordinateText, statusText;
        public Button zoomInButton, zoomOutButton, resetZoomButton;
        [Range(1f, 3f)] public float zoom = 1f;

        void Start()
        {
            if (zoomInButton) zoomInButton.onClick.AddListener(() => zoom = Mathf.Clamp(zoom + 0.25f, 1f, 3f));
            if (zoomOutButton) zoomOutButton.onClick.AddListener(() => zoom = Mathf.Clamp(zoom - 0.25f, 1f, 3f));
            if (resetZoomButton) resetZoomButton.onClick.AddListener(() => zoom = 1f);
        }
        void LateUpdate()
        {
            if (!context) context = FindObjectOfType<TeleopStudentContext>();
            if (!context) return;
            TeleopFrame frame = context.Current;
            if (mapImage)
            {
                mapImage.texture = frame.mapImage;
                mapImage.color = frame.mapImage ? Color.white : Color.black;
                float span = 1f / Mathf.Max(1f, zoom);
                mapImage.uvRect = new Rect((1f - span) * 0.5f, (1f - span) * 0.5f, span, span);
            }
            if (marker)
            {
                bool visible = frame.mapImage && frame.mapMarkerAvailable;
                marker.gameObject.SetActive(visible);
                if (visible)
                {
                    float span = 1f / Mathf.Max(1f, zoom);
                    Vector2 uv = (frame.mapMarkerUV - new Vector2((1f-span)*0.5f, (1f-span)*0.5f)) / span;
                    marker.anchorMin = marker.anchorMax = uv;
                    marker.anchoredPosition = Vector2.zero;
                    marker.localRotation = Quaternion.Euler(0, 0, frame.mapMarkerDegrees);
                    marker.gameObject.SetActive(uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f);
                }
            }
            if (coordinateText) coordinateText.text = frame.poseAvailable
                ? string.Format("ROBOT  X {0:F1} m   Z {1:F1} m    YAW {2:F0}°", frame.robotWorldPosition.x, frame.robotWorldPosition.z, frame.robotYawDegrees)
                : "ROBOT POSE UNAVAILABLE";
            if (statusText) statusText.text = frame.mapImage == null ? "NO MAP — assign top-down RenderTexture" :
                (frame.mapMarkerAvailable ? "TOP-DOWN REFERENCE — NOT SLAM" : "MAP IMAGE — MARKER NOT CALIBRATED");
        }
    }
}
