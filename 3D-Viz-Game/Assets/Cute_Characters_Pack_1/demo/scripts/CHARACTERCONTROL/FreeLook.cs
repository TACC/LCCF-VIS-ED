using UnityEngine;

namespace My.DemoScene
{

    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;

        [Header("Sensitivity")]
        public float mouseSensitivity = 200f;

        [Header("Zoom")]
        public float distance = 5f;
        public float minDistance = 2f;
        public float maxDistance = 8f;
        public float zoomSpeed = 5f;

        [Header("Height")]
        public float height = 2f;

        [Header("Vertical Clamp")]
        public float minY = -35f;
        public float maxY = 60f;

        [Header("Collision")]
        public float collisionRadius = 0.3f;
        public LayerMask collisionLayers = ~0;

        [Header("Startup Lock")]
        public bool enableStartupLock = true;
        public float startupLockDuration = 3f;

        [HideInInspector] public bool isControlEnabled = true;

        private float xRotation = 20f;
        private float yRotation = 0f;
        private float currentDistance;
        private Vector3 currentVelocity;
        private float startupTimer = 0f;

        void Start()
        {
            currentDistance = distance;
            startupTimer = 0f;

            Vector3 angles = transform.eulerAngles;
            yRotation = angles.y;
            xRotation = angles.x > 180f ? angles.x - 360f : angles.x;
        }

        void LateUpdate()
        {
            if (!isControlEnabled) return;
            HandleMouseLook();
            HandleZoom();
        }

        void HandleMouseLook()
        {
            float inputWeight = 1f;
            bool isLocked = false;

            if (enableStartupLock)
            {
                startupTimer += Time.deltaTime;

                if (startupTimer < startupLockDuration)
                {
                    isLocked = true;
                    float fadeStartAt = startupLockDuration * 0.6f;
                    float t = Mathf.InverseLerp(fadeStartAt, startupLockDuration, startupTimer);
                    inputWeight = Mathf.SmoothStep(0f, 1f, t);
                }
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime * inputWeight;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * inputWeight;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, minY, maxY);

            Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0f);

            Vector3 targetPosition = target.position + Vector3.up * height;

            float targetDistance = distance;
            Vector3 direction = -(rotation * Vector3.forward);

            if (Physics.SphereCast(targetPosition, collisionRadius, direction, out RaycastHit hit, distance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                targetDistance = Mathf.Clamp(hit.distance - 0.05f, minDistance, distance);
            }

            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * (targetDistance < currentDistance ? 15f : 5f));

            Vector3 desiredPosition = targetPosition + direction * currentDistance;

            if (isLocked)
            {
                transform.position = desiredPosition;
                currentVelocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref currentVelocity,
                    0.05f
                );
            }

            transform.LookAt(targetPosition);
        }

        void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance -= scroll * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }
    }
}