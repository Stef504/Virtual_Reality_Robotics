using UnityEngine;
using UnityEngine.UI;

namespace TeleopStudentModules
{
    // STUDENT 3 owns this file and its generated panel/prefab.
    // Local scene raycasts are NOT a physical LiDAR. Vibration is NOT implemented.
    public sealed class ObstacleAwarenessModule : MonoBehaviour
    {
        public TeleopStudentContext context;
        public Text leftText, rightText, statusText, thresholdText;
        public Image leftBar, rightBar;
        public Button increaseWarningButton, decreaseWarningButton;

        [Header("Reference: AIM paper proximity thresholds (meters)")]
        [Min(0.1f)] public float warningDistance = 0.85f;
        [Min(0.05f)] public float stopDistance = 0.50f;

        void Start()
        {
            if (increaseWarningButton) increaseWarningButton.onClick.AddListener(() => warningDistance = Mathf.Min(3f, warningDistance+0.1f));
            if (decreaseWarningButton) decreaseWarningButton.onClick.AddListener(() => warningDistance = Mathf.Max(stopDistance+0.05f, warningDistance-0.1f));
        }
        public float Urgency(float distance)
        {
            return Mathf.Clamp01((warningDistance-distance) / Mathf.Max(0.01f, warningDistance-stopDistance));
        }
        void SetBar(Image bar, float urgency)
        {
            if (!bar) return;
            // No sprite dependencies: width is controlled by the RectTransform, not Image.fillAmount.
            RectTransform rect = bar.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(urgency), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            bar.color = urgency >= 0.75f ? new Color(.96f,.26f,.25f) : new Color(1f,.70f,.28f);
        }
        void LateUpdate()
        {
            if (!context) context = FindObjectOfType<TeleopStudentContext>();
            if (!context) return;
            TeleopFrame frame = context.Current;
            if (thresholdText) thresholdText.text = "WARN " + warningDistance.ToString("F2") + " m";
            if (!frame.proximityAvailable)
            {
                if (leftText) leftText.text = "LEFT  N/A";
                if (rightText) rightText.text = "RIGHT N/A";
                if (statusText) statusText.text = "NO PROXIMITY INPUT";
                SetBar(leftBar, 0f);
                SetBar(rightBar, 0f);
                return;
            }
            float l = Urgency(frame.leftDistanceMeters), r = Urgency(frame.rightDistanceMeters);
            if (leftText) leftText.text = "LEFT   " + frame.leftDistanceMeters.ToString("F2") + " m";
            if (rightText) rightText.text = "RIGHT  " + frame.rightDistanceMeters.ToString("F2") + " m";
            SetBar(leftBar, l);
            SetBar(rightBar, r);
            if (statusText) statusText.text = (l >= 1f || r >= 1f) ? "CRITICAL PROXIMITY" :
                ((l > 0 || r > 0) ? "OBSTACLE NEAR — VISUAL PREVIEW" : "CLEAR WITHIN WARNING RANGE");
        }
    }
}
