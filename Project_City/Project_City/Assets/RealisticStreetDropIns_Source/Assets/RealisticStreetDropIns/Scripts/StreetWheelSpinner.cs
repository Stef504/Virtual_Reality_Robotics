using UnityEngine;

namespace RealisticStreetDropIns
{
    /// <summary>Optional visual rotation for the existing DriveReadyCoupe rolling pivots.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StreetRouteMover))]
    public sealed class StreetWheelSpinner : MonoBehaviour
    {
        public Transform[] rollingPivots = new Transform[0];
        [Min(.05f)] public float radius = .368f;
        private StreetRouteMover mover;
        private float angle;
        private void Awake() { mover = GetComponent<StreetRouteMover>(); }
        private void Update()
        {
            if (!mover || rollingPivots == null) return;
            angle += mover.CurrentSpeed / radius * Mathf.Rad2Deg * Time.deltaTime;
            for (int i = 0; i < rollingPivots.Length; i++)
                if (rollingPivots[i]) rollingPivots[i].localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
