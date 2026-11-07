using UnityEngine;
using UnityEngine.UI;

namespace TeleopStudentModules
{
    // STUDENT 1 owns this file and its generated panel/prefab.
    // TODO: UX layouts, camera priorities, better status/latency feedback.
    public sealed class CameraModule : MonoBehaviour
    {
        public TeleopStudentContext context;
        public RawImage primaryImage;
        public RawImage secondaryImage;
        public Text primaryLabel, secondaryLabel, statusText;
        public Button swapButton, secondaryButton;
        public GameObject secondaryView;

        bool swapped, showSecondary = true;

        void Start()
        {
            if (swapButton) swapButton.onClick.AddListener(ToggleSwap);
            if (secondaryButton) secondaryButton.onClick.AddListener(ToggleSecondary);
        }
        void ToggleSwap() { swapped = !swapped; }
        void ToggleSecondary()
        {
            showSecondary = !showSecondary;
            if (secondaryView) secondaryView.SetActive(showSecondary);
        }
        void LateUpdate()
        {
            if (!context) context = FindObjectOfType<TeleopStudentContext>();
            if (!context) return;
            TeleopFrame frame = context.Current;
            Texture first = swapped ? frame.rearImage : frame.frontImage;
            Texture second = swapped ? frame.frontImage : frame.rearImage;
            if (primaryImage) { primaryImage.texture = first; primaryImage.color = first ? Color.white : Color.black; }
            if (secondaryImage) { secondaryImage.texture = second; secondaryImage.color = second ? Color.white : Color.black; }
            if (primaryLabel) primaryLabel.text = swapped ? "REAR CAMERA" : "FRONT CAMERA";
            if (secondaryLabel) secondaryLabel.text = swapped ? "FRONT CAMERA" : "REAR CAMERA";
            if (statusText) statusText.text = first == null ? "NO CAMERA TEXTURE — assign existing RenderTexture" :
                (second == null ? "PRIMARY LIVE / SECONDARY N/A" : "CAMERAS CONNECTED");
        }
    }
}
