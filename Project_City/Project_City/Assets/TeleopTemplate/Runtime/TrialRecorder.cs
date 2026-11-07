using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TeleopLab
{
    public class TrialRecorder : MonoBehaviour
    {
        public DemoRobotDriver robot;
        public string condition = "reference_demo";
        public int trialNumber = 1;
        public bool IsRunning { get; private set; }
        public bool Completed { get; private set; }
        public int Collisions { get; private set; }
        public float Elapsed { get { return IsRunning ? Time.time - started : elapsedAtEnd; } }
        float started, elapsedAtEnd;

        public void ResetTrial()
        {
            IsRunning = Completed = false;
            Collisions = 0;
            elapsedAtEnd = 0f;
            if (robot) robot.ResetRobot();
        }
        public void StartIfNeeded()
        {
            if (!IsRunning && !Completed) { started = Time.time; IsRunning = true; }
        }
        public void AddCollision() { if (IsRunning && !Completed) ++Collisions; }
        public void CompleteTrial()
        {
            if (!IsRunning || Completed) return;
            elapsedAtEnd = Time.time - started;
            IsRunning = false;
            Completed = true;
            SaveCsv();
        }
        void SaveCsv()
        {
            string path = Path.Combine(Application.persistentDataPath, "teleop_trials.csv");
            bool exists = File.Exists(path);
            var sb = new StringBuilder();
            if (!exists) sb.AppendLine("utc_time,condition,trial,completion_time_s,contact_events,completed,source");
            sb.Append(DateTime.UtcNow.ToString("o")).Append(',')
              .Append(condition.Replace(",", "_")).Append(',').Append(trialNumber).Append(',')
              .Append(Elapsed.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(Collisions).Append(",true,demo_surrogate").AppendLine();
            File.AppendAllText(path, sb.ToString());
            Debug.Log("Teleop trial CSV: " + path);
            ++trialNumber;
        }
    }
}
