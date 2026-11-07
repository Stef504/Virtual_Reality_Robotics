using UnityEngine;

namespace RealisticStreetDropIns
{
    /// <summary>Control animation playback speed. Root translation is handled by StreetRouteMover.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetMotionAnimator : MonoBehaviour
    {
        [Min(.2f)] public float playbackSpeed = 1f;
        private Animator animator;
        void Awake() { animator = GetComponentInChildren<Animator>(); }
        void Start()
        {
            if (!animator) return;
            animator.applyRootMotion = false;
            animator.speed = playbackSpeed;
        }
    }
}
