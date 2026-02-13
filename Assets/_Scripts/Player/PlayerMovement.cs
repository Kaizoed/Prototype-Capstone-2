using UnityEngine;
using UnityEngine.InputSystem;

namespace ShakySurvival.Player
{
    public enum MoveState
    {
        Idle,
        Walking,
        Running,
        Crouching
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float crouchSpeed = 2f;

        [Header("Physics")]
        [SerializeField] private float gravity = -20f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private float animBlendSpeed = 8.9f;
        [Tooltip("Velocity scale for walking animations (matches Blend Tree positions)")]
        [SerializeField] private float walkAnimScale = 2f;
        [Tooltip("Velocity scale for running animations (matches Blend Tree positions)")]
        [SerializeField] private float runAnimScale = 6f;
        [Tooltip("Velocity scale for crouching animations (matches Blend Tree positions)")]
        [SerializeField] private float crouchAnimScale = 1.5f;

        public MoveState CurrentMoveState { get; private set; } = MoveState.Idle;
        public bool IsInputLocked { get; private set; }
        public bool IsCrouching => CurrentMoveState == MoveState.Crouching;
        public Vector3 Velocity => _velocity;
        public float SpeedMultiplier { get; private set; } = 1f;
        public Vector3 ExternalDrift { get; private set; } = Vector3.zero;
        
        private CharacterController _controller;
        private Vector3 _velocity;
        private bool _isGrounded;

        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;

        private Vector2 _moveInput;
        private bool _sprintPressed;
        private bool _crouchPressed;

        // Animation
        private int _xVelHash;
        private int _yVelHash;
        private int _crouchHash;
        private Vector2 _animationVelocity;
        private bool _hasAnimator;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            // Initialize animator
            if (animator == null)
            {
                _hasAnimator = TryGetComponent(out animator);
            }
            else
            {
                _hasAnimator = true;
            }

            // Hash animator parameters for performance
            _xVelHash = Animator.StringToHash("X_Velocity");
            _yVelHash = Animator.StringToHash("Y_Velocity");
            _crouchHash = Animator.StringToHash("Crouch");

            SetupInputActions();
        }

        private void OnEnable() { EnableInput(); }
        private void OnDisable() { DisableInput(); }

        private void Update()
        {
            CheckGrounded();
            HandleMovement();
            UpdateAnimator();
        }

        private void SetupInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("[PlayerMovement] InputActionAsset not assigned!");
                return;
            }

            _playerActionMap = inputActions.FindActionMap("Player");
            if (_playerActionMap == null)
            {
                Debug.LogError("[PlayerMovement] 'Player' action map not found!");
                return;
            }

            _moveAction = _playerActionMap.FindAction("Move");
            _sprintAction = _playerActionMap.FindAction("Sprint");
            _crouchAction = _playerActionMap.FindAction("Crouch");
        }

        private void EnableInput()
        {
            if (_playerActionMap == null) return;

            _playerActionMap.Enable();

            if (_moveAction != null)
            {
                _moveAction.performed += OnMove;
                _moveAction.canceled += OnMove;
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed += OnSprint;
                _sprintAction.canceled += OnSprint;
            }

            if (_crouchAction != null)
            {
                _crouchAction.performed += OnCrouch;
                _crouchAction.canceled += OnCrouch;
            }
        }

        private void DisableInput()
        {
            if (_playerActionMap == null) return;

            if (_moveAction != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMove;
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed -= OnSprint;
                _sprintAction.canceled -= OnSprint;
            }

            if (_crouchAction != null)
            {
                _crouchAction.performed -= OnCrouch;
                _crouchAction.canceled -= OnCrouch;
            }

            _playerActionMap.Disable();
        }

        private void OnMove(InputAction.CallbackContext context) { _moveInput = context.ReadValue<Vector2>(); }
        private void OnSprint(InputAction.CallbackContext context) { _sprintPressed = context.performed; }
        private void OnCrouch(InputAction.CallbackContext context) { _crouchPressed = context.performed; }

        public void LockInput()
        {
            IsInputLocked = true;
            _velocity.x = 0f;
            _velocity.z = 0f;
        }

        public void UnlockInput() { IsInputLocked = false; }

        public void SetSpeedMultiplier(float multiplier)
        {
            SpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
        }

        public void SetExternalDrift(Vector3 drift) { ExternalDrift = drift; }

        private void CheckGrounded()
        {
            _isGrounded = _controller.isGrounded;

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            float horizontal = IsInputLocked ? 0f : _moveInput.x;
            float vertical = IsInputLocked ? 0f : _moveInput.y;

            bool wantsToCrouch = !IsInputLocked && _crouchPressed;
            bool wantsToRun = !IsInputLocked && _sprintPressed && !wantsToCrouch;

            Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
            moveDir = Vector3.ClampMagnitude(moveDir, 1f);

            float currentSpeed = walkSpeed;
            MoveState newState = MoveState.Idle;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                if (wantsToCrouch)
                {
                    newState = MoveState.Crouching;
                    currentSpeed = crouchSpeed;
                }
                else if (wantsToRun)
                {
                    newState = MoveState.Running;
                    currentSpeed = runSpeed;
                }
                else
                {
                    newState = MoveState.Walking;
                    currentSpeed = walkSpeed;
                }
            }
            else
            {
                if (wantsToCrouch)
                {
                    newState = MoveState.Crouching;
                }
                else
                {
                    newState = MoveState.Idle;
                }
            }

            CurrentMoveState = newState;

            Vector3 horizontalMove = moveDir * currentSpeed * SpeedMultiplier;
            horizontalMove += ExternalDrift;
            
            _velocity.x = horizontalMove.x;
            _velocity.z = horizontalMove.z;
            _velocity.y += gravity * Time.deltaTime;

            // Only move if controller is enabled (not during cover transitions)
            if (_controller.enabled)
            {
                _controller.Move(_velocity * Time.deltaTime);
            }
        }

        private void UpdateAnimator()
        {
            if (!_hasAnimator) return;

            // Calculate target animation velocity based on movement state
            // Scale values match Blend Tree positions
            float animScale = CurrentMoveState switch
            {
                MoveState.Running => runAnimScale,
                MoveState.Walking => walkAnimScale,
                MoveState.Crouching => crouchAnimScale,
                _ => 0f // Idle
            };

            // Target velocity: raw input scaled to match Blend Tree positions
            Vector2 targetVelocity = IsInputLocked ? Vector2.zero : _moveInput * animScale;

            // Smoothly interpolate animation velocity towards target
            _animationVelocity.x = Mathf.Lerp(_animationVelocity.x, targetVelocity.x, animBlendSpeed * Time.deltaTime);
            _animationVelocity.y = Mathf.Lerp(_animationVelocity.y, targetVelocity.y, animBlendSpeed * Time.deltaTime);

            // Set animator parameters
            animator.SetFloat(_xVelHash, _animationVelocity.x);
            animator.SetFloat(_yVelHash, _animationVelocity.y);
            animator.SetBool(_crouchHash, IsCrouching);
        }
    }
}
