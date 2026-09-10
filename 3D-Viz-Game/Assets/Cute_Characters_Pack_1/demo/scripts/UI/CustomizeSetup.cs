using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace My.DemoScene
{

    public class CharacterPreview : MonoBehaviour
    {
        [Header("Settings")]
        public Camera previewCamera;
        public RawImage targetImage;
        public Transform target;
        public Image maskImage;
        public LayerMask renderLayer;
        public int textureSize = 512;

        [Header("Rotate")]
        public float rotateSpeed = 0.3f;

        [Header("Zoom")]
        public float zoomSpeed = 0.5f;
        public float zoomSmoothing = 8f;
        public float minDistance = 1f;
        public float maxDistance = 5f;

        [Header("Height")]
        public float heightSpeed = 0.01f;
        public float heightSmoothing = 8f;
        public float minHeight = 0f;
        public float maxHeight = 3f;

        [Header("Start Height")]
        public float startHeight = 1f;

        private RenderTexture renderTexture;
        private float angle;
        private float distance;
        private float targetDistance;
        private float height;
        private float targetHeight;

        void Start()
        {
            if (previewCamera == null || targetImage == null || target == null) return;

            height = startHeight;
            targetHeight = startHeight;

            Vector3 offset = previewCamera.transform.position - target.position;
            distance = new Vector2(offset.x, offset.z).magnitude;
            targetDistance = distance;
            angle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;

            previewCamera.cullingMask = renderLayer;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;

            renderTexture = new RenderTexture(textureSize, textureSize, 16);
            renderTexture.Create();
            previewCamera.targetTexture = renderTexture;
            targetImage.texture = renderTexture;

            if (maskImage != null)
            {
                Mask mask = maskImage.GetComponent<Mask>();
                if (mask == null)
                    mask = maskImage.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                targetImage.transform.SetParent(maskImage.transform, false);
                RectTransform rt = targetImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            EventTrigger trigger = targetImage.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = targetImage.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry dragEntry = new EventTrigger.Entry();
            dragEntry.eventID = EventTriggerType.Drag;
            dragEntry.callback.AddListener((data) => OnDrag((PointerEventData)data));
            trigger.triggers.Add(dragEntry);

            EventTrigger.Entry scrollEntry = new EventTrigger.Entry();
            scrollEntry.eventID = EventTriggerType.Scroll;
            scrollEntry.callback.AddListener((data) => OnScroll((PointerEventData)data));
            trigger.triggers.Add(scrollEntry);

            UpdateCamera();
        }

        void Update()
        {
            bool needsUpdate = false;

            if (Mathf.Abs(distance - targetDistance) > 0.01f)
            {
                distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * zoomSmoothing);
                needsUpdate = true;
            }

            if (Mathf.Abs(height - targetHeight) > 0.01f)
            {
                height = Mathf.Lerp(height, targetHeight, Time.deltaTime * heightSmoothing);
                needsUpdate = true;
            }

            if (needsUpdate) UpdateCamera();
        }

        void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                angle += eventData.delta.x * rotateSpeed;
                UpdateCamera();
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                targetHeight -= eventData.delta.y * heightSpeed;
                targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
            }
        }

        void OnScroll(PointerEventData eventData)
        {
            targetDistance -= eventData.scrollDelta.y * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        private void UpdateCamera()
        {
            float rad = angle * Mathf.Deg2Rad;
            previewCamera.transform.position = target.position + new Vector3(
                Mathf.Sin(rad) * distance,
                height,
                Mathf.Cos(rad) * distance
            );
            previewCamera.transform.LookAt(target.position + Vector3.up * height * 0.5f);
        }

        void OnDestroy()
        {
            if (renderTexture != null)
            {
                if (previewCamera != null)
                    previewCamera.targetTexture = null;

                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }
}