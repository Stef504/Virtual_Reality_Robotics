using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CityPeople
{
    public class CityPeople : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Autoplay random animation clips automatically.")] 
        private bool AutoPlayAnimations = false;

        [SerializeField]
        [Tooltip("Drag a specific Animation Clip here to play it on start.")]
        private AnimationClip specificClipToPlay;

        [Header("Material Settings")]
        [SerializeField]
        [Tooltip("Overrides palette materials, skips other objects")]
        private Material PaletteOverride;
        
        public string CurrentPaletteName { get; private set; }

        private AnimationClip[] myClips;
        private Animator animator;
        public const string people_pal_prefix = "people_pal";
        private List<Renderer> _paletteMeshes;

        private void Awake()
        {
            var AllRenderers = gameObject.GetComponentsInChildren<Renderer>();
            _paletteMeshes = new List<Renderer>();
            foreach (Renderer r in AllRenderers)
            {
                var matName = r.sharedMaterial.name;
                var len = Math.Min(people_pal_prefix.Length, matName.Length);
                if (matName[0..len] == CityPeople.people_pal_prefix)
                {
                    _paletteMeshes.Add(r);
                }
            }
            if (_paletteMeshes.Count > 0)
            {
                CurrentPaletteName = _paletteMeshes[0].sharedMaterial.name;
            }

            if (PaletteOverride != null)
            {
                SetPalette(PaletteOverride);
            }
        }

        void Start()
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                myClips = animator.runtimeAnimatorController.animationClips;

                if (AutoPlayAnimations)
                {
                    PlayAnyClip();
                    StartCoroutine(ShuffleClips());
                }
                else if (specificClipToPlay != null)
                {
                    PlaySpecificClip(specificClipToPlay);
                }
            }

            if (AutoPlayAnimations)
            {
                CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 0.8f, 0f);
                collider.radius = 0.3f;
                collider.height = 1.77f;
                collider.direction = 1;
            }
        }

        /// <summary>
        /// Plays a specific clip by name immediately.
        /// </summary>
        public void PlaySpecificClip(AnimationClip clip)
        {
            if (animator == null || clip == null) return;

            // 1. Create a runtime override controller from the active animator controller
            var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            
            // 2. Find a matching clip in the controller (case-insensitive)
            AnimationClip targetOriginalClip = null;
            if (myClips != null)
            {
                foreach (var c in myClips)
                {
                    if (string.Equals(c.name, clip.name, StringComparison.OrdinalIgnoreCase))
                    {
                        targetOriginalClip = c;
                        break;
                    }
                }
            }

            // 3. Fallback: If not found by name, swap the primary base clip in the controller
            if (targetOriginalClip == null && myClips != null && myClips.Length > 0)
            {
                targetOriginalClip = myClips[0];
            }

            if (targetOriginalClip != null)
            {
                overrideController[targetOriginalClip] = clip;
                animator.runtimeAnimatorController = overrideController;
                animator.CrossFadeInFixedTime(targetOriginalClip.name, 0.25f);
            }
            else
            {
                Debug.LogWarning($"[CityPeople] Could not find a valid base state to assign '{clip.name}'");
            }
        }

        public void PlayAnyClip()
        {
            if (myClips != null && myClips.Length > 0)
            {
                var cl = myClips[Random.Range(0, myClips.Length)];
                animator.CrossFadeInFixedTime(cl.name, 1.0f, -1, Random.value * cl.length);
            }
            else Debug.LogWarning("Missing animation clips.");
        }

        IEnumerator ShuffleClips()
        {
            while (true)
            {
                yield return new WaitForSeconds(15.0f + Random.value * 5.0f);
                PlayAnyClip();
            }
        }

        public void SetPalette(Material mat)
        {
            if (mat != null)
            {
                if (mat.name[0..people_pal_prefix.Length] == CityPeople.people_pal_prefix)
                {
                    CurrentPaletteName = mat.name;
                    foreach (Renderer r in _paletteMeshes)
                    {
                        r.material = mat;
                    }
                } 
                else
                {
                    Debug.Log("Material name should start with 'people_pal...' by convention.");
                } 
            }
        }
    }
}