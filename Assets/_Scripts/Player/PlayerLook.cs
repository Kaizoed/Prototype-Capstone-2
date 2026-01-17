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
        [SerializeField, Tooltip("The camera container to rotate vertically (pitch)")]
        private Transform cameraContainer;

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
        private float _xRotation; // Current vertical rotation

        public bool IsLookLocked { get; private set; }

        private void Awake()
        {
            // Find camera container if not assigned
            if (cameraContainer == null)
            {
                if (transform.childCount > 0)
                {
                    cameraContainer = transform.GetChild(0);
                }
                else
                {
                    Debug.LogWarning("[PlayerLook] No camera container assigned!");
                }
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
                Debug.LogError("[PlayerLook] InputActionAsset not assigned!");
                return;
            }

            _playerActionMap = inputActions.FindActionMap("Player");
            if (_playerActionMap == null)
            {
                Debug.LogError("[PlayerLook] 'Player' action map not found!");
                return;
            }

            _lookAction = _playerActionMap.FindAction("Look");
            if (_lookAction == null)
            {
                Debug.LogWarning("[PlayerLook] 'Look' action not found - mouse look disabled.");
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

        /// Lock look input (for UI, cutscenes, etc.).
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
            if (IsLookLocked || cameraContainer == null) return;

            // Only process input when cursor is locked
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float mouseX = _lookInput.x * sensitivityX;
            float mouseY = _lookInput.y * sensitivityY;

            // Horizontal rotation - rotate the player body
            transform.Rotate(Vector3.up * mouseX);

            // Vertical rotation - rotate the camera container
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -maxLookUp, maxLookDown);

            cameraContainer.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }
    }
}
