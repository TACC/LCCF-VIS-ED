using UnityEngine;
using UnityEngine.InputSystem;

namespace My.DemoScene
{

    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runFastSpeed = 10f;
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("RunFast Settings")]
        [SerializeField] private float runFastDelay = 2f;

        private CharacterController characterController;
        private float velocityY;
        private float targetRotation;
        private bool isGrounded;
        private float shiftHeldTime = 0f;

        private const int LAYER_BASE  = 0;
        private const int LAYER_WAVE  = 1;
        private const int LAYER_YES   = 2;
        private const int LAYER_TALK  = 3;
        private const int LAYER_NO    = 4;
        private const int LAYER_LAUGH = 5;

        void Start()
        {
            characterController = GetComponent<CharacterController>();

            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;

            targetRotation = transform.eulerAngles.y;
        }

        void Update()
        {
            CheckGround();
            HandleMovement();
            HandleJump();
            ApplyGravity();
            UpdateAnimation();
            HandleEmotes();
        }

        void CheckGround()
        {
            isGrounded = characterController.isGrounded;

            if (!isGrounded)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
                isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
            }
        }

        void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(horizontal) < 0.1f && Mathf.Abs(vertical) < 0.1f)
                return;

            if (vertical > 0.1f)
                targetRotation = cameraTransform.eulerAngles.y + 0f;
            else if (vertical < -0.1f)
                targetRotation = cameraTransform.eulerAngles.y + 180f;

            if (horizontal > 0.1f)
                targetRotation = cameraTransform.eulerAngles.y + 90f;
            else if (horizontal < -0.1f)
                targetRotation = cameraTransform.eulerAngles.y - 90f;

            if (Mathf.Abs(horizontal) > 0.1f && Mathf.Abs(vertical) > 0.1f)
            {
                if (vertical > 0.1f && horizontal > 0.1f)
                    targetRotation = cameraTransform.eulerAngles.y + 45f;
                else if (vertical > 0.1f && horizontal < -0.1f)
                    targetRotation = cameraTransform.eulerAngles.y - 45f;
                else if (vertical < -0.1f && horizontal > 0.1f)
                    targetRotation = cameraTransform.eulerAngles.y + 135f;
                else if (vertical < -0.1f && horizontal < -0.1f)
                    targetRotation = cameraTransform.eulerAngles.y - 135f;
            }

            float smoothRotation = Mathf.LerpAngle(transform.eulerAngles.y, targetRotation, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, smoothRotation, 0f);

            float t = Mathf.Clamp01(shiftHeldTime / runFastDelay);
            float speed;

            if (Input.GetKey(KeyCode.LeftShift) || shiftHeldTime > 0f)
                speed = Mathf.Lerp(walkSpeed, runFastSpeed, t);
            else
                speed = walkSpeed;

            Vector3 moveDirection = transform.forward * speed;
            characterController.Move(moveDirection * Time.deltaTime);
        }

        void HandleJump()
        {
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                velocityY = jumpForce;
                animator.SetTrigger("Jump");
            }
        }

        void ApplyGravity()
        {
            if (isGrounded && velocityY < 0f)
                velocityY = -2f;

            velocityY += Physics.gravity.y * Time.deltaTime;
            characterController.Move(Vector3.up * velocityY * Time.deltaTime);
        }

        void UpdateAnimation()
        {
            if (animator == null) return;

            animator.SetBool("IsGrounded", isGrounded);

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            float inputMagnitude = new Vector2(horizontal, vertical).magnitude;

            if (inputMagnitude > 0.1f)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    shiftHeldTime += Time.deltaTime;
                    shiftHeldTime = Mathf.Min(shiftHeldTime, runFastDelay);
                }
                else
                {
                    shiftHeldTime -= Time.deltaTime;
                    shiftHeldTime = Mathf.Max(0f, shiftHeldTime);
                }

                float t = Mathf.Clamp01(shiftHeldTime / runFastDelay);
                float animSpeed = Mathf.Lerp(0.333f, 1f, t);
                animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
            }
            else
            {
                shiftHeldTime -= Time.deltaTime;
                shiftHeldTime = Mathf.Max(0f, shiftHeldTime);
                animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            }
        }

        void HandleEmotes()
        {
            if (animator == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                animator.Play("Wave", LAYER_WAVE);

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                animator.Play("Yes", LAYER_YES);

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                animator.Play("No", LAYER_NO);

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                animator.Play("Talk", LAYER_TALK);

            if (Keyboard.current.digit5Key.wasPressedThisFrame)
                animator.Play("HappyDance", LAYER_BASE);

            if (Keyboard.current.digit6Key.wasPressedThisFrame)
                animator.Play("Disappointed", LAYER_BASE);

            if (Keyboard.current.digit7Key.wasPressedThisFrame)
                animator.Play("Laugh", LAYER_LAUGH);

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(LAYER_BASE);
                if (state.IsName("HappyDance"))
                    animator.Play("Locomotion", LAYER_BASE);
            }
        }
    }
}