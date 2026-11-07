using UnityEngine;
using UnityEngine.UI;

namespace TeleopLab
{
    // Baseline UI students replace/refine while preserving the inspector-facing data API.
    public class TeleopDashboard : MonoBehaviour
    {
        public RawImage frontImage, rearImage, mapImage;
        public Button swapButton, mapButton, resetButton;
        bool camerasSwapped;
        Texture frontSource, rearSource;
        bool frontFlip, rearFlip;
        public Text statusText, modeText, mapLabel, leftText, rightText, metricText, helpText;
        public Image leftBar, rightBar;
        public RectTransform mapRobotMarker;
        public RenderTexture demoFront, demoRear, demoMap;
        public DemoProximity demoProximity;
        public TrialRecorder trials;

        void Start()
        {
            if (swapButton) swapButton.onClick.AddListener(SwapCameras);
            if (mapButton) mapButton.onClick.AddListener(ToggleMap);
            if (resetButton) resetButton.onClick.AddListener(() => { if (trials && FindObjectOfType<TeleopCore>().mode == TeleopSourceMode.Demo) trials.ResetTrial(); });
        }

        void SwapCameras() { camerasSwapped = !camerasSwapped; DisplayCameras(); }
        void ToggleMap() { if (mapImage) mapImage.transform.parent.gameObject.SetActive(!mapImage.transform.parent.gameObject.activeSelf); }
        void DisplayCameras()
        {
            Texture front = camerasSwapped ? rearSource : frontSource;
            Texture rear = camerasSwapped ? frontSource : rearSource;
            if (frontImage)
            {
                frontImage.texture = front;
                frontImage.uvRect = (camerasSwapped ? rearFlip : frontFlip) ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
                var aspect = frontImage.GetComponent<AspectRatioFitter>();
                if (aspect && front) aspect.aspectRatio = (float)front.width / front.height;
            }
            if (rearImage)
            {
                rearImage.texture = rear;
                rearImage.uvRect = (camerasSwapped ? frontFlip : rearFlip) ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
                var aspect = rearImage.GetComponent<AspectRatioFitter>();
                if (aspect && rear) aspect.aspectRatio = (float)rear.width / rear.height;
            }
        }

        public void EnterMode(TeleopSourceMode mode)
        {
            if (mapRobotMarker) mapRobotMarker.gameObject.SetActive(false);
            if (mode == TeleopSourceMode.Demo)
            {
                frontSource = demoFront; rearSource = demoRear; frontFlip = rearFlip = false;
                DisplayCameras();
                SetMapTexture(demoMap);
            }
            else
            {
                frontSource = null; rearSource = null; frontFlip = rearFlip = false;
                DisplayCameras();
                if (mapImage) mapImage.texture = null;
                if (mapLabel) mapLabel.text = "WAITING FOR ROS OCCUPANCY GRID";
                SetRosProximityUnavailable();
            }
        }

        public void SetCameraTexture(bool front, Texture texture, bool flipV)
        {
            if (front) { frontSource = texture; frontFlip = flipV; }
            else { rearSource = texture; rearFlip = flipV; }
            DisplayCameras();
        }
        public void SetMapTexture(Texture texture)
        {
            if (mapImage)
            {
                mapImage.texture = texture; mapImage.uvRect = new Rect(0, 0, 1, 1);
                var aspect = mapImage.GetComponent<AspectRatioFitter>();
                if (aspect && texture) aspect.aspectRatio = (float)texture.width / texture.height;
            }
        }
        public void SetMapMarker(bool visible, float u, float v, float yawRelativeToMapDegrees)
        {
            if (!mapRobotMarker) return;
            mapRobotMarker.gameObject.SetActive(visible);
            if (!visible) return;
            mapRobotMarker.anchorMin = mapRobotMarker.anchorMax = new Vector2(u, v);
            mapRobotMarker.anchoredPosition = Vector2.zero;
            // The icon triangle points up; ROS heading 0 points right on an XY map.
            mapRobotMarker.localRotation = Quaternion.Euler(0, 0, yawRelativeToMapDegrees - 90f);
        }

        public void SetRosProximity(float leftDistance, float rightDistance, float leftUrgency, float rightUrgency)
        {
            SetProximity(leftDistance, rightDistance, leftUrgency, rightUrgency);
        }
        public void SetRosProximityUnavailable()
        {
            if (leftText) leftText.text = "LEFT  N/A";
            if (rightText) rightText.text = "RIGHT N/A";
            if (leftBar) leftBar.fillAmount = 0f;
            if (rightBar) rightBar.fillAmount = 0f;
        }
        void SetProximity(float left, float right, float leftUrgency, float rightUrgency)
        {
            if (leftText) leftText.text = "LEFT   " + left.ToString("F2") + " m";
            if (rightText) rightText.text = "RIGHT  " + right.ToString("F2") + " m";
            if (leftBar) { leftBar.fillAmount = Mathf.Clamp01(leftUrgency); leftBar.color = leftUrgency > 0.7f ? new Color(.96f, .35f, .32f) : new Color(.95f, .69f, .33f); }
            if (rightBar) { rightBar.fillAmount = Mathf.Clamp01(rightUrgency); rightBar.color = rightUrgency > 0.7f ? new Color(.96f, .35f, .32f) : new Color(.95f, .69f, .33f); }
        }

        public void Refresh(TeleopCore core)
        {
            if (core.mode == TeleopSourceMode.Demo)
            {
                if (modeText) modeText.text = "STANDALONE  |  DEMO DATA";
                if (mapLabel) mapLabel.text = "TOP-DOWN CAMERA  |  NOT SLAM";
                if (statusText) statusText.text = "SIMULATED / LOCAL";
                if (demoProximity) SetProximity(demoProximity.LeftDistance, demoProximity.RightDistance, demoProximity.LeftUrgency, demoProximity.RightUrgency);
                if (metricText && trials) metricText.text = "TIME  " + trials.Elapsed.ToString("F1") + " s    CONTACTS  " + trials.Collisions + (trials.Completed ? "    FINISHED" : "");
            }
            else
            {
                if (modeText) modeText.text = "ROS 2  |  GAZEBO DATA";
                if (statusText) statusText.text = core.RosPort == null ? "NO ROS ADAPTER INSTALLED" : core.RosPort.Health;
                if (metricText) metricText.text = "ROS TRIAL LOGGING NOT ENABLED";
            }
            if (helpText) helpText.text = core.mode == TeleopSourceMode.Demo
                ? "W/S drive  A/D turn  |  SPACE stop  |  R reset  |  F2 ROS"
                : "HOLD SHIFT + W/A/S/D  |  SPACE stop  |  F1 demo";
        }
    }
}
