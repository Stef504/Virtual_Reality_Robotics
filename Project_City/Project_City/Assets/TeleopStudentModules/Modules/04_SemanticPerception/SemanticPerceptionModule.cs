using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TeleopStudentModules
{
    // STUDENT 4 owns this file and its generated panel/prefab.
    // This is a visualization for explicitly supplied labels, NOT a semantic detector/MOS.
    public sealed class SemanticPerceptionModule : MonoBehaviour
    {
        public TeleopStudentContext context;
        public Text statusText, filterText;
        public Text[] objectRows;
        public Button staticFilterButton, movingFilterButton;
        public bool showStatic = true;
        public bool showMoving = true;
        public int maxVisibleObjects = 6;

        readonly List<ObservedObject> visible = new List<ObservedObject>();
        Vector3 currentRobotPosition;

        void Start()
        {
            if (staticFilterButton) staticFilterButton.onClick.AddListener(() => showStatic = !showStatic);
            if (movingFilterButton) movingFilterButton.onClick.AddListener(() => showMoving = !showMoving);
        }
        void LateUpdate()
        {
            if (!context) context = FindObjectOfType<TeleopStudentContext>();
            if (!context) return;
            TeleopFrame frame = context.Current;
            currentRobotPosition = frame.robotWorldPosition;
            visible.Clear();
            foreach (ObservedObject obj in frame.objects)
                if (obj.isMoving ? showMoving : showStatic) visible.Add(obj);
            visible.Sort((a, b) =>
                (a.worldPosition-currentRobotPosition).sqrMagnitude.CompareTo((b.worldPosition-currentRobotPosition).sqrMagnitude));
            if (filterText) filterText.text = "STATIC " + (showStatic ? "ON" : "OFF") +
                "     MOVING " + (showMoving ? "ON" : "OFF");
            if (statusText) statusText.text = frame.objects.Count == 0 ? "NO OBJECT LABELS — ADD SemanticSceneLabel" :
                frame.objects.Count + " ANNOTATIONS  |  " + (frame.isSampleData ? "SAMPLE" : "MANUAL / ADAPTER");
            if (objectRows == null) return;
            for (int i = 0; i < objectRows.Length; i++)
            {
                if (!objectRows[i]) continue;
                if (i >= visible.Count || i >= maxVisibleObjects)
                {
                    objectRows[i].text = "";
                    continue;
                }
                ObservedObject o = visible[i];
                string distance = frame.poseAvailable ?
                    Vector3.Distance(currentRobotPosition, o.worldPosition).ToString("F1") + " m" : "distance N/A";
                objectRows[i].text = (o.isMoving ? "[MOVING]  " : "[STATIC]    ") +
                    o.label + "   |   " + distance;
                objectRows[i].color = o.isMoving ? new Color(1f,.74f,.40f) : new Color(.67f,.89f,1f);
            }
        }
    }
}
