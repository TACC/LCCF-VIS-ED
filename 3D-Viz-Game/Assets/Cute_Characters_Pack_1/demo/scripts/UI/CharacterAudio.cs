using UnityEngine;

namespace My.DemoScene
{

    public class FootstepSounds : MonoBehaviour
    {
        public AudioSource audioSource;
        public AudioClip footstepClip;

        [Header("Pitch Settings")]
        public float minPitch = 0.9f;
        public float maxPitch = 1.1f;

        [Header("Settings")]
        public float speedThreshold = 0.1f;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void PlayFootstep()
        {
            if (audioSource == null || footstepClip == null) return;
            if (animator.GetFloat("Speed") < speedThreshold) return;

            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(footstepClip);
        }

        private void PlayJump()
        {
            if (audioSource == null || footstepClip == null) return;

            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(footstepClip);
        }

        public void rightfootstep() => PlayFootstep();
        public void leftfootstep() => PlayFootstep();
        public void runrightfootstep() => PlayFootstep();
        public void runleftfootstep() => PlayFootstep();
        public void jumpstart() => PlayJump();
        public void jumpend() => PlayJump();
    }
}