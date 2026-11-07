using UnityEngine;

namespace TeleopStudentModules
{
    // Replaceable data adapter. None of the four module scripts knows about ROS or Husky physics.
    public abstract class TeleopDataSource : MonoBehaviour
    {
        public abstract void Fill(TeleopFrame destination);
    }
}
