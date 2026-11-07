using UnityEngine;

namespace TeleopStudentModules
{
    // Useful for students running modules WITHOUT a robot or live data.
    // NEVER use this to claim navigation, safety or perception performance.
    public sealed class SampleTeleopDataSource : TeleopDataSource
    {
        public float cycleSeconds = 8f;
        public override void Fill(TeleopFrame dst)
        {
            float t = Time.unscaledTime;
            float p = Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(1f, cycleSeconds));
            dst.isSampleData = true;
            dst.sourceLabel = "SAMPLE DATA — NOT A SENSOR";
            dst.sourceDetail = "Synthetic positions, proximity and object labels";
            dst.poseAvailable = true;
            dst.robotWorldPosition = new Vector3(0f, 0f, p * 2f);
            dst.robotForward = Vector3.forward;
            dst.robotYawDegrees = 0f;
            dst.proximityAvailable = true;
            dst.leftDistanceMeters = Mathf.Lerp(0.35f, 2.5f, (p + 1f) / 2f);
            dst.rightDistanceMeters = Mathf.Lerp(2.5f, 0.4f, (p + 1f) / 2f);
            dst.objects.Add(new ObservedObject("Wall section", new Vector3(-2,0,3), false, "synthetic sample"));
            dst.objects.Add(new ObservedObject("Person A", new Vector3(1,0,2+p), true, "synthetic sample"));
            dst.objects.Add(new ObservedObject("Pallet", new Vector3(2,0,-2), false, "synthetic sample"));
            // Camera and map intentionally unavailable: do not show invented imagery.
        }
    }
}
