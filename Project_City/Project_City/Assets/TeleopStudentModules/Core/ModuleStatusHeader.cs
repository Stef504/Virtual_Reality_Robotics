using UnityEngine;
using UnityEngine.UI;

namespace TeleopStudentModules
{
    public sealed class ModuleStatusHeader : MonoBehaviour
    {
        public TeleopStudentContext context;
        public Text status;
        void LateUpdate()
        {
            if (!context) context = FindObjectOfType<TeleopStudentContext>();
            if (status && context)
                status.text = context.Current.sourceLabel;
        }
    }
}
