using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ShakySurvival.Player;

namespace ShakySurvival.Interactions.Examine
{
    public class ExamineController : MonoBehaviour
    {
        public static ExamineController Instance { get; private set; }

        // ── Inspector ───────────────────────────────────────────
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string takeActionName = "TakeItem";

        [Header("Offset")]
        [Tooltip("Transform the examined object lerps towards (child of camera).")]
        [SerializeField] private Transform examineOffset;

        [Header("Lerp")]
        [SerializeField] private float positionLerpSpeed = 8f;
        [SerializeField] private float rotationLerpSpeed = 8f;
        [SerializeField] private float returnThreshold = 0.005f;

        [Header("Rotation")]
        [Tooltip("Degrees per pixel of mouse delta.")]
        [SerializeField] private float rotationSpeed = 0.4f;

        // ── Events (for UI / other listeners) ───────────────────
        public event Action<ExamineInteractable> OnExamineStarted;
        public event Action OnExamineStopped;

        public event Action<ExamineData> OnItemTaken;

        // ── State ───────────────────────────────────────────────
        private enum State { Idle, Examining, Returning }
        private State _state = State.Idle;

        public bool IsExamining => _state != State.Idle;

        private ExamineInteractable _activeInteractable;
        private Transform _activeTransform;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;

        private PlayerMovement _playerMovement;
        private PlayerLook _playerLook;
        private PlayerInventory _playerInventory;

        private InputAction _takeAction;
        private bool _takeRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ExamineController] Duplicate instance destroyed.", this);
                Destroy(this);
                return;
            }
            Instance = this;

            _playerMovement = GetComponent<PlayerMovement>();
            _playerLook = GetComponent<PlayerLook>();
            _playerInventory = GetComponent<PlayerInventory>();
            SetupInput();
        }

        private void OnEnable()
        {
            if (_takeAction != null)
            {
                _takeAction.Enable();
                _takeAction.performed += OnTakePerformed;
            }
        }

        private void OnDisable()
        {
            if (_takeAction != null)
            {
                _takeAction.performed -= OnTakePerformed;
                _takeAction.Disable();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void SetupInput()
        {
            if (inputActions == null) return;

            var map = inputActions.FindActionMap(actionMapName);
            if (map == null) return;

            _takeAction = map.FindAction(takeActionName);
            if (_takeAction == null)
                Debug.LogWarning($"[ExamineController] Could not find action '{takeActionName}' in map '{actionMapName}'.", this);
        }

        private void OnTakePerformed(InputAction.CallbackContext context)
        {
            _takeRequested = true;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Examining:
                    UpdateExamining();
                    break;
                case State.Returning:
                    UpdateReturning();
                    break;
            }
        }

        // ── Public API ──────────────────────────────────────────
        public void StartExamine(ExamineInteractable interactable)
        {
            if (_state != State.Idle) return;
            if (interactable == null) return;

            _activeInteractable = interactable;
            _activeTransform = interactable.transform;

            // Store original world pose
            _originalPosition = _activeTransform.position;
            _originalRotation = _activeTransform.rotation;

            // Lock player
            _playerMovement?.LockInput();
            if (_playerLook != null)
            {
                _playerLook.LockLook();
            }

            _state = State.Examining;
            OnExamineStarted?.Invoke(_activeInteractable);
        }

        public void StopExamine()
        {
            if (_state != State.Examining) return;

            _state = State.Returning;
        }

        // ── Update helpers ──────────────────────────────────────
        private void UpdateExamining()
        {
            if (_activeTransform == null) { ForceCleanup(); return; }

            if (_takeRequested)
            {
                _takeRequested = false;
                TakeItem();
                return;
            }

            _activeTransform.position = Vector3.Lerp(
                _activeTransform.position,
                examineOffset.position,
                positionLerpSpeed * Time.deltaTime);

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _activeTransform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
                _activeTransform.Rotate(Vector3.right, delta.y * rotationSpeed, Space.World);
            }
        }

        private void UpdateReturning()
        {
            if (_activeTransform == null) { ForceCleanup(); return; }

            _activeTransform.position = Vector3.Lerp(
                _activeTransform.position,
                _originalPosition,
                positionLerpSpeed * Time.deltaTime);

            _activeTransform.rotation = Quaternion.Slerp(
                _activeTransform.rotation,
                _originalRotation,
                rotationLerpSpeed * Time.deltaTime);

            float dist = Vector3.Distance(_activeTransform.position, _originalPosition);
            float angle = Quaternion.Angle(_activeTransform.rotation, _originalRotation);

            if (dist < returnThreshold && angle < 0.5f)
            {
                _activeTransform.position = _originalPosition;
                _activeTransform.rotation = _originalRotation;
                FinishReturn();
            }
        }

        private void FinishReturn()
        {
            _playerMovement?.UnlockInput();
            if (_playerLook != null)
            {
                _playerLook.UnlockLook();
                _playerLook.LockCursor();
            }

            _activeInteractable = null;
            _activeTransform = null;
            _state = State.Idle;

            OnExamineStopped?.Invoke();
        }

        private void TakeItem()
        {
            if (_activeInteractable == null) return;

            ExamineData data = _activeInteractable.Data;

            if (_playerInventory != null)
            {
                _playerInventory.AddItem(data);
            }
            else
            {
                Debug.LogWarning("[ExamineController] No PlayerInventory found on this GameObject. " +
                                 "Item was not stored.", this);
            }

            _activeInteractable.gameObject.SetActive(false);

            OnItemTaken?.Invoke(data);
            OnExamineStopped?.Invoke();

            _playerMovement?.UnlockInput();
            if (_playerLook != null)
            {
                _playerLook.UnlockLook();
                _playerLook.LockCursor();
            }

            _activeInteractable = null;
            _activeTransform = null;
            _state = State.Idle;
        }

        private void ForceCleanup()
        {
            _state = State.Idle;
            _activeInteractable = null;
            _activeTransform = null;

            _playerMovement?.UnlockInput();
            if (_playerLook != null)
            {
                _playerLook.UnlockLook();
                _playerLook.LockCursor();
            }

            OnExamineStopped?.Invoke();
        }
    }
}
