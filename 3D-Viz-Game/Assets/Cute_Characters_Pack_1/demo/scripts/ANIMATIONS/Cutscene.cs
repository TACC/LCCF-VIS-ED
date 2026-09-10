using UnityEngine;

namespace My.DemoScene
{

    public class AnimationEventBridge : MonoBehaviour
    {
        [Header("References")]
        public ThirdPersonCamera thirdPersonCamera;
        public MonoBehaviour thirdPersonController;
        public MonoBehaviour characterController;
        public Animator cinematicAnimator;

        [Header("Settings")]
        public bool disableOnStart = true;

        void Start()
        {
            if (disableOnStart)
                DisableControl();
        }

        public void DisableControl()
        {
            if (thirdPersonCamera != null)     thirdPersonCamera.isControlEnabled = false;
            if (thirdPersonController != null) thirdPersonController.enabled = false;
            if (characterController != null)   characterController.enabled = false;
        }

        public void OnAnimationEnd()
        {
            if (thirdPersonCamera != null)     thirdPersonCamera.isControlEnabled = true;
            if (thirdPersonController != null) thirdPersonController.enabled = true;
            if (characterController != null)   characterController.enabled = true;
            if (cinematicAnimator != null)     cinematicAnimator.enabled = false;
        }
    }
}