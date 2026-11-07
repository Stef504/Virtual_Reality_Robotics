using UnityEngine;

namespace TeleopStudentModules
{
    public sealed class TeleopStudentContext : MonoBehaviour
    {
        [Tooltip("Assign SceneTeleopDataSource or SampleTeleopDataSource. Future: external ROS adapter.")]
        public TeleopDataSource dataSource;

        private readonly TeleopFrame frame = new TeleopFrame();
        private int frameNumber = -1;

        public TeleopFrame Current
        {
            get
            {
                if (frameNumber != Time.frameCount)
                {
                    frame.Clear();
                    if (dataSource != null && dataSource.isActiveAndEnabled)
                        dataSource.Fill(frame);
                    frameNumber = Time.frameCount;
                }
                return frame;
            }
        }

        public void SelectDataSource(TeleopDataSource newSource)
        {
            dataSource = newSource;
            frameNumber = -1;
        }
    }
}
