using UnityEngine;
using UnityEngine.UI;

namespace My.DemoScene
{

    public class CurtainUI : MonoBehaviour
    {
        [Header("Scene References")]
        public Camera renderCamera;
        public Animator curtainAnimator;

        [Header("UI References")]
        public RawImage displayRawImage;
        public Canvas canvasA;
        public Canvas canvasBOriginal;

        [Header("Render Texture Settings")]
        public int textureWidth = 1920;
        public int textureHeight = 1080;

        [Header("Lighting")]
        public Light characterLight;

        private RenderTexture renderTexture;
        private bool isOpen = false;
        private bool isAnimating = false;

        private CanvasGroup canvasAGroup;
        private CanvasGroup canvasBGroup;

        void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            renderTexture = new RenderTexture(textureWidth, textureHeight, 16);
            renderTexture.Create();
            renderCamera.targetTexture = renderTexture;
            displayRawImage.texture = renderTexture;

            canvasAGroup = GetOrAddCanvasGroup(canvasA);
            canvasBGroup = GetOrAddCanvasGroup(canvasBOriginal);

            SetCanvasVisible(canvasAGroup, false);
            SetCanvasVisible(canvasBGroup, false);

            if (characterLight != null)
                characterLight.enabled = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E) && !isAnimating)
            {
                TriggerCurtain();
            }
        }

        void TriggerCurtain()
        {
            isAnimating = true;
            SetCanvasVisible(canvasBGroup, true);
            curtainAnimator.Play("Curtain", 0, 0f);
        }

        public void curtainclosed()
        {
            if (!isOpen)
            {
                SetCanvasVisible(canvasAGroup, true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                if (characterLight != null)
                    characterLight.enabled = true;
            }
            else
            {
                SetCanvasVisible(canvasAGroup, false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                if (characterLight != null)
                    characterLight.enabled = false;
            }
        }

        public void curtainanimfinished()
        {
            SetCanvasVisible(canvasBGroup, false);
            isOpen = !isOpen;
            isAnimating = false;
        }

        private void SetCanvasVisible(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;

            GraphicRaycaster[] raycasters = group.GetComponentsInChildren<GraphicRaycaster>(true);
            foreach (GraphicRaycaster raycaster in raycasters)
                raycaster.enabled = visible;
        }

        private CanvasGroup GetOrAddCanvasGroup(Canvas canvas)
        {
            CanvasGroup group = canvas.GetComponent<CanvasGroup>();
            if (group == null)
                group = canvas.gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderCamera.targetTexture = null;
                renderTexture.Release();
            }
        }
    }
}