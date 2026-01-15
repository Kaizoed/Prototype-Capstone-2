using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintMultiplier = 2.0f;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float standHeight = 2.0f;
    [SerializeField] private float cameraCrouchOffset = 0.5f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownLookRange = 80f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundStickForce = -3f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;

    private Vector3 currentMovement;
    private Vector3 velocity;
    private float verticalRotation;
    private float defaultCameraHeight;
    private bool isCrouched;

    private float CurrentSpeed
    {
        get
        {
            float baseSpeed = walkSpeed;

            if (playerInputHandler.sprintTriggered && !isCrouched)
                baseSpeed *= sprintMultiplier;

            if (isCrouched)
                baseSpeed *= crouchSpeedMultiplier;

            return baseSpeed;
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        defaultCameraHeight = mainCamera.transform.localPosition.y;
        mainCamera.GetComponent<CameraShaking>().enabled = true;
    }

    void Update()
    {
        HandleCrouch();
        HandleMovement();
        ApplyGravity();
        HandleRotation();
    }

    // ---------------- MOVEMENT ----------------

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(
            playerInputHandler.MovementInput.x,
            0f,
            playerInputHandler.MovementInput.y
        );

        return transform.TransformDirection(inputDirection).normalized;
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();

        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;

        characterController.Move(currentMovement * Time.deltaTime);
    }

    // ---------------- GRAVITY & GROUNDING ----------------

    private void ApplyGravity()
    {
        if (characterController.isGrounded)
        {
            // Keeps player snapped to ground when stepping down
            if (velocity.y < 0)
                velocity.y = groundStickForce;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        characterController.Move(velocity * Time.deltaTime);
    }

    // ---------------- CROUCH ----------------

    private void HandleCrouch()
    {
        if (playerInputHandler.crouchTriggered && !isCrouched)
            StartCrouch();
        else if (!playerInputHandler.crouchTriggered && isCrouched)
            StopCrouch();

        float targetCameraHeight = isCrouched
            ? (defaultCameraHeight - cameraCrouchOffset)
            : defaultCameraHeight;

        Vector3 camPos = mainCamera.transform.localPosition;

        CameraShaking shake = mainCamera.GetComponent<CameraShaking>();
        Vector3 shakeOffset = shake != null ? shake.ShakePositionOffset : Vector3.zero;

        camPos.y = Mathf.Lerp(camPos.y, targetCameraHeight, Time.deltaTime * crouchTransitionSpeed);
        mainCamera.transform.localPosition = camPos + shakeOffset;
    }

    private void StartCrouch()
    {
        isCrouched = true;
        characterController.height = crouchHeight;
        characterController.center = new Vector3(0, crouchHeight / 2f, 0);
    }

    private void StopCrouch()
    {
        isCrouched = false;
        characterController.height = standHeight;
        characterController.center = new Vector3(0, standHeight / 2f, 0);
    }

    // ---------------- CAMERA ROTATION ----------------

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        verticalRotation = Mathf.Clamp(
            verticalRotation - rotationAmount,
            -upDownLookRange,
            upDownLookRange
        );

        Quaternion lookRotation = Quaternion.Euler(verticalRotation, 0, 0);

        CameraShaking shake = mainCamera.GetComponent<CameraShaking>();
        if (shake != null)
            mainCamera.transform.localRotation = lookRotation * shake.ShakeRotationOffset;
        else
            mainCamera.transform.localRotation = lookRotation;
    }

    private void HandleRotation()
    {
        float mouseXRotation = playerInputHandler.rotationInput.x * mouseSensitivity;
        float mouseYRotation = playerInputHandler.rotationInput.y * mouseSensitivity;

        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);
    }
}
