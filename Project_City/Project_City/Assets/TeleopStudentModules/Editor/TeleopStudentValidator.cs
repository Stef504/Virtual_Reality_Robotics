#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TeleopStudentModules.Editor
{
    public static class TeleopStudentValidator
    {
        [MenuItem("Tools/Teleop Students/Validate Current Scene Connections")]
        public static void Validate()
        {
            StringBuilder report = new StringBuilder();
            var context = Object.FindObjectOfType<TeleopStudentContext>();
            var scene = Object.FindObjectOfType<SceneTeleopDataSource>();
            var cameras = Object.FindObjectOfType<CameraModule>();
            var map = Object.FindObjectOfType<SpatialMapModule>();
            var proximity = Object.FindObjectOfType<ObstacleAwarenessModule>();
            var semantic = Object.FindObjectOfType<SemanticPerceptionModule>();
            report.AppendLine("XR Teleop Student Modules — Scene validation\n");
            report.AppendLine("Context: " + (context ? "OK" : "MISSING"));
            report.AppendLine("Data source selected: " + (context && context.dataSource ? context.dataSource.GetType().Name : "NONE"));
            report.AppendLine("Four modules: " + (cameras && map && proximity && semantic ? "FOUND" : "SOME MISSING"));
            if (scene != null)
            {
                report.AppendLine("Husky root: " + (scene.huskyRoot ? scene.huskyRoot.name : "MISSING"));
                report.AppendLine("Pose frame: " + (scene.robotPoseFrame ? scene.robotPoseFrame.name : "MISSING"));
                report.AppendLine("Front texture: " + (scene.frontTextureOverride || (scene.frontCamera && scene.frontCamera.targetTexture) ? "AVAILABLE" : "MISSING"));
                report.AppendLine("Rear texture: " + (scene.rearTextureOverride || (scene.rearCamera && scene.rearCamera.targetTexture) ? "AVAILABLE" : "MISSING"));
                report.AppendLine("Map texture (optional): " + (scene.topDownTextureOverride || (scene.topDownCamera && scene.topDownCamera.targetTexture) ? "AVAILABLE" : "MISSING"));
            }
            report.AppendLine("\nThis validation does NOT test runtime articulation physics or ROS bridge connectivity.");
            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Teleop connection check", report.ToString(), "OK");
        }
    }
}
#endif
