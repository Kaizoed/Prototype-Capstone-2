using UnityEngine;
using UnityEngine.InputSystem;

namespace ShakySurvival.Player
{
    public class PlayerLook : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField, Tooltip("Reference to the InputActionAsset")]
        private InputActionAsset inputActions;

        [Header("Camera Reference")]
        [SerializeField, Tooltip("CameraRoot transform - for applying pitch rotation")]
        private Transform cameraRoot;

        [Header("Sensitivity")]
        [SerializeField, Tooltip("Mouse sensitivity for horizontal look")]
        private float sensitivityX = 0.1f;

        [SerializeField, Tooltip("Mouse sensitivity for vertical look")]
        private float sensitivityY = 0.1f;

        [Header("Vertical Limits")]
        [SerializeField, Tooltip("Maximum look up angle (degrees)")]
        private float maxLookUp = 80f;

        [SerializeField, Tooltip("Maximum look down angle (degrees)")]
        private float maxLookDown = 80f;

        [Header("Cursor Settings")]
        [SerializeField, Tooltip("Lock and hide cursor on start")]
        private bool lockCursorOnStart = true;

        private InputActionMap _playerActionMap;
        private InputAction _lookAction;
        private Vector2 _lookInput;
        private float _xRotation;
        private float _yRotation;

        // Clamping state
        private bool _isHorizontalClamped;
        private float _clampCenterYaw;
        private float _maxYawOffset = 180f;

        public bool IsLookLocked { get; private set; }
        
        public float CurrentYaw => _yRotation;

        private void Awake()
        {
            if (cameraRoot == null)
            {
                Debug.LogWarning("[PlayerLook] CameraRoot not assigned!");
            }

            SetupInputActions();
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                LockCursor();
            }
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        private void Update()
        {
            HandleLook();

            // Toggle cursor lock with Escape
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleCursorLock();
            }
        }

        private void SetupInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset not assigned!");
                return;
            }

            _playerActionMap = inputActions.FindActionMap("Player");
            if (_playerActionMap == null)
            {
                Debug.LogError("Player action map not found!");
                return;
            }

            _lookAction = _playerActionMap.FindAction("Look");
            if (_lookAction == null)
            {
                Debug.LogWarning("Look action not found!");
            }
        }

        private void EnableInput()
        {
            if (_playerActionMap == null) return;

            _playerActionMap.Enable();

            if (_lookAction != null)
            {
                _lookAction.performed += OnLook;
                _lookAction.canceled += OnLook;
            }
        }

        private void DisableInput()
        {
            if (_lookAction != null)
            {
                _lookAction.performed -= OnLook;
                _lookAction.canceled -= OnLook;
            }
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        // Lock look input (can be used for UI, cutscenes, etc.)
        public void LockLook()
        {
            IsLookLocked = true;
        }

        /// Unlock look input.
        public void UnlockLook()
        {
            IsLookLocked = false;
        }

        /// Lock and hide the cursor.
        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// Unlock and show the cursor.
        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// Toggle cursor lock state.
        public void ToggleCursorLock()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }

        private void HandleLook()
        {
            if (IsLookLocked || cameraRoot == null) return;

            // Only process input when cursor is locked
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float mouseX = _lookInput.x * sensitivityX;
            float mouseY = _lookInput.y * sensitivityY;

            // Horizontal rotation - rotate player body
            _yRotation += mouseX;
            
            // Apply horizontal clamping if enabled
            if (_isHorizontalClamped)
            {
                float minYaw = _clampCenterYaw - _maxYawOffset;
                float maxYaw = _clampCenterYaw + _maxYawOffset;
                _yRotation = Mathf.Clamp(_yRotation, minYaw, maxYaw);
            }
            
            transform.rotation = Quaternion.Euler(0f, _yRotation, 0f);

            // Vertical rotation - rotate the CameraRoot (Cinemachine follows it)
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -maxLookUp, maxLookDown);

            cameraRoot.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }

        // Enables horizontal look clamping around the current facing direction.
        public void EnableHorizontalClamp(float maxOffset)
        {
            _clampCenterYaw = _yRotation;
            _maxYawOffset = maxOffset;
            _isHorizontalClamped = true;
        }

        // Enables horizontal look clamping around a specific yaw angle.
        public void EnableHorizontalClamp(float centerYaw, float maxOffset)
        {
            _clampCenterYaw = centerYaw;
            _maxYawOffset = maxOffset;
            _isHorizontalClamped = true;
        }

        /// Disables horizontal look clamping.
        public void DisableHorizontalClamp()
        {
            _isHorizontalClamped = false;
            _maxYawOffset = 180f;
        }

        // Sets the current yaw rotation directly (used during cover transitions).
        public void SetYaw(float yaw)
        {
            _yRotation = yaw;
            transform.rotation = Quaternion.Euler(0f, _yRotation, 0f);
        }

        /// Temporarily overrides vertical look limits.
        public void SetVerticalLimits(float lookUp, float lookDown)
        {
            maxLookUp = lookUp;
            maxLookDown = lookDown;
        }
    }
}
