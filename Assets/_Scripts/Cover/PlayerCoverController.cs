using System.Collections;
using UnityEngine;
using ShakySurvival.Player;
using ShakySurvival.Camera;

namespace ShakySurvival.Cover
{
    /// <summary>
    /// Controls sequential, animation-driven player transitions into and out of cover.
    /// The camera is attached to the player's head bone, so rotating/moving the body moves the camera.
    /// </summary>
    public class PlayerCoverController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private PlayerStagger playerStagger;
        [SerializeField] private EarthquakeCameraShaker cameraShaker;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;

        [Header("Movement")]
        [Tooltip("Speed (units/sec) the player crawls toward the anchor or exit point.")]
        [SerializeField] private float crawlSpeed = 2f;

        [Tooltip("Rotation speed (degrees/sec) for the final turn to face the hide anchor.")]
        [SerializeField] private float turnSpeed = 120f;

        [Tooltip("How long to wait for an animation crossfade to settle (seconds).")]
        [SerializeField] private float animBlendTime = 0.25f;

        [Header("Ground Snapping")]
        [Tooltip("Layers considered 'ground' for the downward raycast.")]
        [SerializeField] private LayerMask groundLayer = ~0;

        [Tooltip("How far above the target position the ground raycast originates.")]
        [SerializeField] private float groundRayOriginHeight = 2f;

        [Tooltip("Maximum distance for the ground raycast.")]
        [SerializeField] private float groundRayMaxDist = 5f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // ── Animator parameter hashes ──
        private int _crouchHash;
        private int _coverCrawlHash;

        // ── State ──
        private CoverState _currentState = CoverState.Idle;
        private CoverSpot _activeCoverSpot;
        private Coroutine _transitionCoroutine;

        private float _savedColliderHeight;
        private Vector3 _savedColliderCenter;

        // ── Public accessors ──
        public CoverState CurrentState => _currentState;
        public bool IsInCover => _currentState != CoverState.Idle;
        public bool CanInteract => _currentState == CoverState.Idle || _currentState == CoverState.Hidden;

        // ──────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
            if (playerLook == null) playerLook = GetComponent<PlayerLook>();
            if (playerStagger == null) playerStagger = GetComponent<PlayerStagger>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (cameraShaker == null) cameraShaker = FindFirstObjectByType<EarthquakeCameraShaker>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            _crouchHash = Animator.StringToHash("Crouch");
            _coverCrawlHash = Animator.StringToHash("CoverCrawl");
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>Attempts to enter cover at the specified spot.</summary>
        public bool EnterCover(CoverSpot spot)
        {
            if (_currentState != CoverState.Idle)
            {
                if (debugMode) Debug.Log("[PlayerCoverController] Cannot enter cover – not in Idle state.");
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

            // Reserve the cover spot for the player.
            if (!spot.TryOccupy(gameObject))
            {
                if (debugMode) Debug.Log("[PlayerCoverController] Cover spot is occupied!");
                return false;
            }

            _activeCoverSpot = spot;
            _transitionCoroutine = StartCoroutine(EnterCoverRoutine());
            return true;
        }

        /// <summary>Attempts to exit the current cover.</summary>
        public bool ExitCover()
        {
            if (_currentState != CoverState.Hidden)
            {
                if (debugMode) Debug.Log("[PlayerCoverController] Cannot exit cover – not in Hidden state.");
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

        /// <summary>Immediately aborts any transition and returns to idle.</summary>
        public void ForceExit()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            // Reset animator bools
            if (animator != null)
            {
                animator.SetBool(_crouchHash, false);
                animator.SetBool(_coverCrawlHash, false);
            }

            // Reset gameplay modifiers
            if (cameraShaker != null) cameraShaker.ShakeMultiplier = 1f;
            if (playerStagger != null) playerStagger.IsImmune = false;

            // Restore collider
            if (characterController != null)
            {
                characterController.height = _savedColliderHeight;
                characterController.center = _savedColliderCenter;
                characterController.enabled = true;
            }

            // Restore controls
            playerLook?.DisableHorizontalClamp();
            playerLook?.SetVerticalLimits(80f, 80f);
            playerMovement?.UnlockInput();
            playerLook?.UnlockLook();

            // Release the cover spot.
            if (_activeCoverSpot != null)
                _activeCoverSpot.Release(gameObject);

            _activeCoverSpot = null;
            _currentState = CoverState.Idle;

            if (debugMode) Debug.Log("[PlayerCoverController] Force-exited from cover!");
        }

        // ──────────────────────────────────────────────
        // Entry Sequence (4 steps)
        // ──────────────────────────────────────────────

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

            // Shrink collider for under-table clearance (controller stays enabled)
            SaveAndShrinkCollider();

            // ── Step 1: Blend into Crouch Idle ──
            if (debugMode) Debug.Log("[PlayerCoverController] Step 1 – Blending to Crouch Idle");
            SetAnimatorState(crouch: true, crawl: false);
            yield return new WaitForSeconds(animBlendTime);

            // ── Step 2: Crawl toward hideAnchor.position ──
            if (debugMode) Debug.Log("[PlayerCoverController] Step 2 – Crawling to HideAnchor");
            SetAnimatorState(crouch: true, crawl: true);
            yield return StartCoroutine(MoveTowardsTarget(_activeCoverSpot.HideAnchor.position));

            // ── Step 3: Settle back to Crouch Idle ──
            if (debugMode) Debug.Log("[PlayerCoverController] Step 3 – Settling to Crouch Idle");
            SetAnimatorState(crouch: true, crawl: false);
            yield return new WaitForSeconds(animBlendTime);

            // ── Step 4: Turn to match hideAnchor facing direction ──
            float targetYaw = _activeCoverSpot.HiddenFacingYaw;
            if (debugMode) Debug.Log($"[PlayerCoverController] Step 4 – Turning to yaw {targetYaw:F1}°");
            yield return StartCoroutine(TurnTowardsYaw(targetYaw));

            // Set up constrained look while hidden
            playerLook?.SetYaw(targetYaw);
            playerLook?.EnableHorizontalClamp(targetYaw, _activeCoverSpot.MaxLookYaw);
            playerLook?.SetVerticalLimits(_activeCoverSpot.MaxLookUp, _activeCoverSpot.MaxLookDown);
            playerLook?.UnlockLook();

            _currentState = CoverState.Hidden;
            _transitionCoroutine = null;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Hidden");
        }

        // ──────────────────────────────────────────────
        // Exit Sequence (4 steps)
        // ──────────────────────────────────────────────

        private IEnumerator ExitCoverRoutine()
        {
            _currentState = CoverState.Exiting;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Exiting");

            // Lock look during exit
            playerLook?.LockLook();
            playerLook?.DisableHorizontalClamp();

            // ── Step 1: Blend into Crawling ──
            if (debugMode) Debug.Log("[PlayerCoverController] Step 1 – Blending to Crawling");
            SetAnimatorState(crouch: true, crawl: true);
            yield return new WaitForSeconds(animBlendTime);

            // ── Step 2: Crawl toward exitPoint.position ──
            if (debugMode) Debug.Log("[PlayerCoverController] Step 2 – Crawling to ExitPoint");
            yield return StartCoroutine(MoveTowardsTarget(_activeCoverSpot.ExitPoint.position));

            // ── Step 3: Settle back to Crouch Idle ──
            if (debugMode) Debug.Log("[PlayerCoverController] Step 3 – Settling to Crouch Idle");
            SetAnimatorState(crouch: true, crawl: false);
            yield return new WaitForSeconds(animBlendTime);

            // ── Step 4: Return to standing Idle ──
            if (debugMode) Debug.Log("[PlayerCoverController] Step 4 – Returning to Standing Idle");
            SetAnimatorState(crouch: false, crawl: false);

            // Restore collider dimensions
            RestoreCollider();

            // Restore camera shake
            if (cameraShaker != null) cameraShaker.ShakeMultiplier = 1f;
            if (playerStagger != null) playerStagger.IsImmune = false;

            // Restore default look limits and sync yaw to exit direction
            playerLook?.SetVerticalLimits(80f, 80f);
            playerLook?.SetYaw(_activeCoverSpot.ExitPoint.eulerAngles.y);

            // Unlock controls
            playerMovement?.UnlockInput();
            playerLook?.UnlockLook();

            // Release the cover spot.
            if (_activeCoverSpot != null)
                _activeCoverSpot.Release(gameObject);

            _activeCoverSpot = null;
            _currentState = CoverState.Idle;
            _transitionCoroutine = null;
            if (debugMode) Debug.Log("[PlayerCoverController] State -> Idle");
        }

        // ──────────────────────────────────────────────
        // Movement & Rotation Helpers
        // ──────────────────────────────────────────────

        /// <summary>
        /// Moves the player toward <paramref name="targetPos"/> at <see cref="crawlSpeed"/> using
        /// CharacterController.Move(), snapping Y to the ground each frame.
        /// </summary>
        private IEnumerator MoveTowardsTarget(Vector3 targetPos)
        {
            // Pre-snap the target Y to the actual floor beneath it
            targetPos.y = SnapYToGround(targetPos);

            while (true)
            {
                Vector3 currentPos = transform.position;
                Vector3 toTarget = targetPos - currentPos;

                // Only care about horizontal distance for arrival check
                Vector3 horizontalDelta = new Vector3(toTarget.x, 0f, toTarget.z);
                if (horizontalDelta.sqrMagnitude <= 0.01f) break;

                // Desired horizontal step this frame
                float step = crawlSpeed * Time.deltaTime;
                Vector3 moveDir = horizontalDelta.normalized * Mathf.Min(step, horizontalDelta.magnitude);

                // Ground-snap: find true floor height at the next position
                Vector3 nextHorizontalPos = currentPos + moveDir;
                float groundY = SnapYToGround(nextHorizontalPos);

                // Compute the full delta including vertical correction
                float verticalDelta = groundY - currentPos.y;
                Vector3 moveDelta = new Vector3(moveDir.x, verticalDelta, moveDir.z);

                characterController.Move(moveDelta);
                yield return null;
            }

            // Final snap to exact position
            Vector3 finalSnap = targetPos - transform.position;
            if (finalSnap.sqrMagnitude > 0.0001f)
            {
                characterController.Move(finalSnap);
            }
        }

        /// <summary>
        /// Smoothly rotates the player's Y-axis toward <paramref name="targetYaw"/> at <see cref="turnSpeed"/> deg/sec.
        /// </summary>
        private IEnumerator TurnTowardsYaw(float targetYaw)
        {
            Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);

            while (Quaternion.Angle(transform.rotation, targetRot) > 0.5f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                yield return null;
            }

            transform.rotation = targetRot;
        }

        /// <summary>
        /// Casts a ray downward from above <paramref name="pos"/> and returns the ground-snapped Y.
        /// Falls back to the original Y if no ground is hit.
        /// </summary>
        private float SnapYToGround(Vector3 pos)
        {
            Vector3 origin = new Vector3(pos.x, pos.y + groundRayOriginHeight, pos.z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayMaxDist, groundLayer))
            {
                return hit.point.y;
            }

            if (debugMode) Debug.LogWarning($"[PlayerCoverController] Ground raycast missed at {pos}. Using original Y.");
            return pos.y;
        }

        // ──────────────────────────────────────────────
        // Collider Helpers
        // ──────────────────────────────────────────────

        private void SaveAndShrinkCollider()
        {
            if (characterController == null) return;

            _savedColliderHeight = characterController.height;
            _savedColliderCenter = characterController.center;

            float originalBottom = _savedColliderCenter.y - (_savedColliderHeight / 2f);
            float newHeight = _activeCoverSpot.HidingColliderHeight;
            float newCenterY = originalBottom + (newHeight / 2f) + _activeCoverSpot.ColliderVerticalOffset;

            if (debugMode)
            {
                Debug.Log($"[PlayerCoverController] Collider shrink: height {_savedColliderHeight} -> {newHeight}, " +
                          $"center.y {_savedColliderCenter.y} -> {newCenterY}");
            }

            characterController.height = newHeight;
            characterController.center = new Vector3(_savedColliderCenter.x, newCenterY, _savedColliderCenter.z);
        }

        private void RestoreCollider()
        {
            if (characterController == null) return;

            characterController.height = _savedColliderHeight;
            characterController.center = _savedColliderCenter;

            if (debugMode) Debug.Log("[PlayerCoverController] Collider restored.");
        }

        // ──────────────────────────────────────────────
        // Animator Helper
        // ──────────────────────────────────────────────

        private void SetAnimatorState(bool crouch, bool crawl)
        {
            if (animator == null) return;
            animator.SetBool(_crouchHash, crouch);
            animator.SetBool(_coverCrawlHash, crawl);
        }
    }
}
