using System.Collections;
using UnityEngine;
using ShakySurvival.Player;
using ShakySurvival.Camera;

namespace ShakySurvival.Cover
{
    // Controls player transitions into and out of cover using anchor-based transforms.
    // The camera follows the player's head bone, so moving the player body moves the camera.
    public class PlayerCoverController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private PlayerStagger playerStagger;
        [SerializeField] private EarthquakeCameraShaker cameraShaker;
        [SerializeField] private CharacterController characterController;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // State
        private CoverState _currentState = CoverState.Idle;
        private CoverSpot _activeCoverSpot;
        private Coroutine _transitionCoroutine;
        
        private float _savedColliderHeight;
        private Vector3 _savedColliderCenter;

        public CoverState CurrentState => _currentState;
        public bool IsInCover => _currentState != CoverState.Idle;
        public bool CanInteract => _currentState == CoverState.Idle || _currentState == CoverState.Hidden;

        private void Awake()
        {
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
            if (playerLook == null) playerLook = GetComponent<PlayerLook>();
            if (playerStagger == null) playerStagger = GetComponent<PlayerStagger>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (cameraShaker == null) cameraShaker = FindFirstObjectByType<EarthquakeCameraShaker>();
        }

        // Attempts to enter cover at the specified spot
        public bool EnterCover(CoverSpot spot)
        {
            if (_currentState != CoverState.Idle)
            {
                if (debugMode) Debug.Log("[PlayerCoverController] Cannot enter cover - not in Idle state.");
                return false;
            }

            if (spot == null)
            {
                Debug.LogError("[PlayerCoverController] CoverSpot is null!");
                return false;
            }

            if (!spot.IsValidApproach(transform))
            {
                if (debugMode) Debug.Log("[PlayerCoverController] Invalid approach angle.");
                return false;
            }

            _activeCoverSpot = spot;
            _transitionCoroutine = StartCoroutine(EnterCoverRoutine());
            return true;
        }

        // Attempts to exit the current cover
        public bool ExitCover()
        {
            if (_currentState != CoverState.Hidden)
            {
                if (debugMode) Debug.Log("[PlayerCoverController] Cannot exit cover - not in Hidden state.");
                return false;
            }

            if (_activeCoverSpot == null)
            {
                Debug.LogError("[PlayerCoverController] No active cover spot!");
                return false;
            }

            _transitionCoroutine = StartCoroutine(ExitCoverRoutine());
            return true;
        }

        private IEnumerator EnterCoverRoutine()
        {
            _currentState = CoverState.Entering;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Entering");

            // Lock player controls
            playerMovement?.LockInput();
            playerLook?.LockLook();
            playerMovement?.SetForcedCrouch(true);

            // Apply per-spot gameplay modifiers
            if (playerStagger != null) playerStagger.IsImmune = _activeCoverSpot.GrantStaggerImmunity;
            if (cameraShaker != null) cameraShaker.ShakeMultiplier = _activeCoverSpot.ShakeMultiplier;

            // Save and shrink collider BEFORE disabling controller
            if (characterController != null)
            {
                _savedColliderHeight = characterController.height;
                _savedColliderCenter = characterController.center;
                
                float originalBottom = _savedColliderCenter.y - (_savedColliderHeight / 2f);
                float newHeight = _activeCoverSpot.HidingColliderHeight;
                float newCenterY = originalBottom + (newHeight / 2f) + _activeCoverSpot.ColliderVerticalOffset;
                
                if (debugMode)
                {
                    Debug.Log($"[PlayerCoverController] Collider shrink: height {_savedColliderHeight} -> {newHeight}, center.y {_savedColliderCenter.y} -> {newCenterY}");
                }
                
                characterController.height = newHeight;
                characterController.center = new Vector3(_savedColliderCenter.x, newCenterY, _savedColliderCenter.z);
            }

            // Disable CharacterController for direct position control
            if (characterController != null) characterController.enabled = false;

            // Unified transition to HideAnchor
            if (debugMode) Debug.Log("[PlayerCoverController] Transitioning to HideAnchor");
            yield return StartCoroutine(TransitionRoutine(
                _activeCoverSpot.HideAnchor,
                _activeCoverSpot.EntryTransitionDuration,
                _activeCoverSpot.EntryEasing
            ));

            // Re-enable CharacterController
            if (characterController != null) characterController.enabled = true;
            
            // Set up constrained look while hidden
            float facingYaw = _activeCoverSpot.HiddenFacingYaw;
            playerLook?.SetYaw(facingYaw);
            playerLook?.EnableHorizontalClamp(facingYaw, _activeCoverSpot.MaxLookYaw);
            playerLook?.SetVerticalLimits(_activeCoverSpot.MaxLookUp, _activeCoverSpot.MaxLookDown);
            playerLook?.UnlockLook();

            _currentState = CoverState.Hidden;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Hidden");
        }

        private IEnumerator ExitCoverRoutine()
        {
            _currentState = CoverState.Exiting;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Exiting");

            // Lock look during exit
            playerLook?.LockLook();
            playerLook?.DisableHorizontalClamp();

            if (characterController != null) characterController.enabled = false;

            // Unified transition to ExitPoint
            if (debugMode) Debug.Log("[PlayerCoverController] Transitioning to ExitPoint");
            yield return StartCoroutine(TransitionRoutine(
                _activeCoverSpot.ExitPoint,
                _activeCoverSpot.ExitTransitionDuration,
                _activeCoverSpot.ExitEasing
            ));

            // Restore collider dimensions
            if (characterController != null)
            {
                characterController.height = _savedColliderHeight;
                characterController.center = _savedColliderCenter;
                characterController.enabled = true;
            }

            playerMovement?.SetForcedCrouch(false);

            // Restore camera shake
            if (cameraShaker != null) cameraShaker.ShakeMultiplier = 1f;

            // Disable stagger immunity
            if (playerStagger != null) playerStagger.IsImmune = false;

            // Restore default look limits and sync yaw
            playerLook?.SetVerticalLimits(80f, 80f);
            playerLook?.SetYaw(_activeCoverSpot.ExitPoint.eulerAngles.y);
            
            // Unlock controls
            playerMovement?.UnlockInput();
            playerLook?.UnlockLook();

            _activeCoverSpot = null;
            _currentState = CoverState.Idle;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Idle");
        }
        
        // Smoothly transitions the player to the target transform over the specified duration.
        // Uses AnimationCurve for easing both position and rotation.
        private IEnumerator TransitionRoutine(Transform target, float duration, AnimationCurve easingCurve)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = target.position;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = target.rotation;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                elapsed = Mathf.Min(elapsed, duration);
                
                float t = easingCurve.Evaluate(elapsed / duration);
                
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                
                yield return null;
            }

            // Ensure final position/rotation are exact
            transform.position = targetPos;
            transform.rotation = targetRot;
        }

        /// Force exit from cover (might be useful for other features)
        public void ForceExit()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            // Reset all states immediately
            if (cameraShaker != null) cameraShaker.ShakeMultiplier = 1f;
            if (playerStagger != null) playerStagger.IsImmune = false;
            if (characterController != null)
            {
                characterController.enabled = true;
                characterController.height = _savedColliderHeight;
                characterController.center = _savedColliderCenter;
            }

            
            playerLook?.DisableHorizontalClamp();
            playerLook?.SetVerticalLimits(80f, 80f);
            playerMovement?.UnlockInput();
            playerLook?.UnlockLook();
            playerMovement?.SetForcedCrouch(false);

            _activeCoverSpot = null;
            _currentState = CoverState.Idle;
            
            if (debugMode) Debug.Log("[PlayerCoverController] Force exited from cover!");
        }
    }
}
