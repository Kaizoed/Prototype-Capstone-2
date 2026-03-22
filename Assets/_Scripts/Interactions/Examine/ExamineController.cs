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

        // ── Lifecycle ───────────────────────────────────────────

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
                // Snap exactly
                _activeTransform.position = _originalPosition;
                _activeTransform.rotation = _originalRotation;
                FinishReturn();
            }
        }

        private void FinishReturn()
        {
            // Unlock player
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
