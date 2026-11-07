using UnityEngine;

namespace TeleopStudentModules
{
    // Add to arbitrary warehouse props. This is *manual annotation* only, never MOS inference.
    public sealed class SemanticSceneLabel : MonoBehaviour
    {
        public string objectLabel = "Object";
        public bool manuallyMarkedMoving;
        [Tooltip("For labels anchored above an object rather than at its origin.")]
        public Vector3 localLabelOffset = Vector3.zero;
        public Vector3 WorldLabelPosition { get { return transform.TransformPoint(localLabelOffset); } }
    }
}
