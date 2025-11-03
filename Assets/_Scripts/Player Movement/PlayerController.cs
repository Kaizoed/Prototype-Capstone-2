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

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;

    private Vector3 currentMovement;
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
    }

    void Update()
    {
        HandleCrouch();
        HandleMovement();
        HandleRotation();
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;

        characterController.Move(currentMovement * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        if (playerInputHandler.crouchTriggered && !isCrouched)
        {
            StartCrouch();
        }
        else if (!playerInputHandler.crouchTriggered && isCrouched)
        {
            StopCrouch();
        }

        float targetCameraHeight = isCrouched ? (defaultCameraHeight - cameraCrouchOffset) : defaultCameraHeight;
        Vector3 camPos = mainCamera.transform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCameraHeight, Time.deltaTime * crouchTransitionSpeed);
        mainCamera.transform.localPosition = camPos;
    }

    private void StartCrouch()
    {
        isCrouched = true;
        characterController.height = crouchHeight;
    }

    private void StopCrouch()
    {
        isCrouched = false;
        characterController.height = standHeight;
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmount, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleRotation()
    {
        float mouseXRotation = playerInputHandler.rotationInput.x * mouseSensitivity;
        float mouseYRotation = playerInputHandler.rotationInput.y * mouseSensitivity;

        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);
    }
}
