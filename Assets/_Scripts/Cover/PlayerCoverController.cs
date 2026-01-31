using System.Collections;
using UnityEngine;
using ShakySurvival.Player;
using ShakySurvival.Camera;

namespace ShakySurvival.Cover
{
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
        
        // Saved values for restoration
        private float _savedCameraHeight;
        private float _savedMaxLookUp;
        private float _savedMaxLookDown;
        private float _savedColliderHeight;
        private Vector3 _savedColliderCenter;

        // Public accessors
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

        /// <summary>
        /// Attempts to enter cover at the specified spot.
        /// </summary>
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

        /// <summary>
        /// Attempts to exit the current cover.
        /// </summary>
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
            
            // Apply per-spot gameplay modifiers
            if (playerStagger != null) playerStagger.IsImmune = _activeCoverSpot.GrantStaggerImmunity;
            if (cameraShaker != null) cameraShaker.ShakeMultiplier = _activeCoverSpot.ShakeMultiplier;

            // Save current camera height for restoration
            _savedCameraHeight = playerMovement.GetCurrentCameraHeight();
            
            // Override camera height control
            if (playerMovement != null) playerMovement.IsCameraHeightOverridden = true;

            // Save and shrink collider BEFORE disabling controller
            if (characterController != null)
            {
                _savedColliderHeight = characterController.height;
                _savedColliderCenter = characterController.center;
                
                float originalBottom = _savedColliderCenter.y - (_savedColliderHeight / 2f);
                
                // New height from cover spot
                float newHeight = _activeCoverSpot.HidingColliderHeight;
                
                float newCenterY = originalBottom + (newHeight / 2f) + _activeCoverSpot.ColliderVerticalOffset;
                
                if (debugMode)
                {
                    Debug.Log($"[PlayerCoverController] Collider shrink: height {_savedColliderHeight} -> {newHeight}, center.y {_savedColliderCenter.y} -> {newCenterY} (offset: {_activeCoverSpot.ColliderVerticalOffset})");
                }
                
                characterController.height = newHeight;
                characterController.center = new Vector3(_savedColliderCenter.x, newCenterY, _savedColliderCenter.z);
            }

            // Disable CharacterController for direct position control
            if (characterController != null) characterController.enabled = false;

            // Lower (camera) phase
            if (debugMode) Debug.Log("[PlayerCoverController] Phase 1: Lowering camera");
            yield return StartCoroutine(LowerPhase());

            // Crawl phase
            if (debugMode) Debug.Log("[PlayerCoverController] Phase 2: Crawling to anchor");
            yield return StartCoroutine(CrawlPhase());

            // Camera turn
            if (debugMode) Debug.Log("[PlayerCoverController] Phase 3: Turning around");
            yield return StartCoroutine(TurnPhase());

            // Settle phase
            if (debugMode) Debug.Log("[PlayerCoverController] Phase 4: Settling");
            
            // Re-enable CharacterController
            if (characterController != null)
            {
                characterController.enabled = true;
            }
            
            // Locked camera look
            float facingYaw = _activeCoverSpot.HiddenFacingYaw;
            playerLook?.SetYaw(facingYaw);
            playerLook?.EnableHorizontalClamp(facingYaw, _activeCoverSpot.MaxLookYaw);
            playerLook?.SetVerticalLimits(_activeCoverSpot.MaxLookUp, _activeCoverSpot.MaxLookDown);
            playerLook?.UnlockLook();

            _currentState = CoverState.Hidden;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Hidden");
        }

        private IEnumerator LowerPhase()
        {
            float elapsed = 0f;
            float duration = _activeCoverSpot.LowerDuration;
            float startHeight = _savedCameraHeight;
            float targetHeight = _activeCoverSpot.HidingCameraHeight;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = _activeCoverSpot.EntryEasing.Evaluate(elapsed / duration);
                
                float currentHeight = Mathf.Lerp(startHeight, targetHeight, t);
                playerMovement?.SetCameraHeight(currentHeight);
                
                yield return null;
            }

            playerMovement?.SetCameraHeight(targetHeight);
        }

        private IEnumerator CrawlPhase()
        {
            float elapsed = 0f;
            float duration = _activeCoverSpot.CrawlDuration;
            
            Vector3 startPos = transform.position;
            Vector3 targetPos = _activeCoverSpot.HideAnchor.position;
            
            // Keep current rotation during crawl (don't change where player is looking)
            Quaternion maintainedRotation = transform.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = _activeCoverSpot.EntryEasing.Evaluate(elapsed / duration);
                
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = maintainedRotation; // Maintain look direction
                
                yield return null;
            }

            transform.position = targetPos;
        }

        private IEnumerator TurnPhase()
        {
            float elapsed = 0f;
            float duration = _activeCoverSpot.TurnDuration;
            
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = _activeCoverSpot.HideAnchor.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = _activeCoverSpot.EntryEasing.Evaluate(elapsed / duration);
                
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                
                yield return null;
            }

            transform.rotation = targetRot;
        }

        private IEnumerator ExitCoverRoutine()
        {
            _currentState = CoverState.Exiting;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Exiting");

            // Lock look during exit
            playerLook?.LockLook();
            playerLook?.DisableHorizontalClamp();

            if (characterController != null) characterController.enabled = false;

            Vector3 startPos = transform.position;
            Vector3 targetPos = _activeCoverSpot.ExitPoint.position;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = _activeCoverSpot.ExitPoint.rotation;

            // crawl out
            if (debugMode) Debug.Log("[PlayerCoverController] Exit Phase 1: Crawling out");
            
            float elapsed = 0f;
            float crawlDuration = _activeCoverSpot.ExitCrawlDuration;

            while (elapsed < crawlDuration)
            {
                elapsed += Time.deltaTime;
                float t = _activeCoverSpot.ExitEasing.Evaluate(elapsed / crawlDuration);
                
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;

            // rise (camera) phase
            if (debugMode) Debug.Log("[PlayerCoverController] Exit Phase 2: Rising");
            
            // Restore collider dimensions before rising
            if (characterController != null)
            {
                characterController.height = _savedColliderHeight;
                characterController.center = _savedColliderCenter;
            }
            
            float startHeight = _activeCoverSpot.HidingCameraHeight;
            float targetHeight = _savedCameraHeight;
            
            elapsed = 0f;
            float riseDuration = _activeCoverSpot.ExitRiseDuration;

            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = _activeCoverSpot.ExitEasing.Evaluate(elapsed / riseDuration);
                
                float currentHeight = Mathf.Lerp(startHeight, targetHeight, t);
                playerMovement?.SetCameraHeight(currentHeight);
                
                yield return null;
            }

            playerMovement?.SetCameraHeight(targetHeight);

            if (characterController != null) characterController.enabled = true;

            // Restore camera shake
            if (cameraShaker != null) cameraShaker.ShakeMultiplier = 1f;

            // Disable stagger immunity
            if (playerStagger != null) playerStagger.IsImmune = false;

            // Restore default look limits and sync yaw
            playerLook?.SetVerticalLimits(80f, 80f); // Default values
            playerLook?.SetYaw(targetRot.eulerAngles.y);
            
            // Release camera height control back to PlayerMovement
            if (playerMovement != null) playerMovement.IsCameraHeightOverridden = false;
            
            // Unlock controls
            playerMovement?.UnlockInput();
            playerLook?.UnlockLook();

            _activeCoverSpot = null;
            _currentState = CoverState.Idle;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Idle");
        }

        /// Force exit from cover (Just in case we added table destruction and such)
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
            if (playerMovement != null) playerMovement.IsCameraHeightOverridden = false;
            playerMovement?.SetCameraHeight(_savedCameraHeight);
            playerMovement?.UnlockInput();
            playerLook?.UnlockLook();

            _activeCoverSpot = null;
            _currentState = CoverState.Idle;
            
            if (debugMode) Debug.Log("[PlayerCoverController] Force exited from cover!");
        }
    }
}
