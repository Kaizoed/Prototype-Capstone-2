using UnityEngine;

namespace ShakySurvival.Cover
{
    public class CoverSpot : MonoBehaviour
    {
        [Header("Occupancy")]
        [Tooltip("Read-only. Shows who is currently occupying this cover spot.")]
        [SerializeField] private GameObject currentOccupant;

        public bool IsOccupied => currentOccupant != null;

        public GameObject Occupant => currentOccupant;

        public bool TryOccupy(GameObject requester)
        {
            if (currentOccupant == null || currentOccupant == requester)
            {
                currentOccupant = requester;
                return true;
            }
            return false;
        }

        public void Release(GameObject requester)
        {
            if (currentOccupant == requester)
                currentOccupant = null;
        }

        public void ForceRelease()
        {
            currentOccupant = null;
        }
        [Header("Transforms")]
        [Tooltip("Where the NPC/player is positioned while hiding.")]
        [SerializeField] private Transform hideAnchor;

        [Tooltip("Where the NPC should navigate to BEFORE crawling under the table. " +
                 "Must be ON the NavMesh (outside the NavMeshObstacle carve).")]
        [SerializeField] private Transform entryPoint;
        
        [Tooltip("Where the NPC/player exits to after leaving cover.")]
        [SerializeField] private Transform exitPoint;

        [Header("Camera Settings")]
        [Tooltip("Maximum horizontal look angle while hidden (degrees from center).")]
        [SerializeField] private float maxLookYaw = 45f;
        
        [Tooltip("Maximum vertical look up while hidden.")]
        [SerializeField] private float maxLookUp = 20f;
        
        [Tooltip("Maximum vertical look down while hidden.")]
        [SerializeField] private float maxLookDown = 30f;
        
        [Header("Player Collider")]
        [Tooltip("Player collider height while hiding.")]
        [SerializeField] private float hidingColliderHeight = 0.5f;
        
        [Tooltip("Vertical offset for the hiding collider. Positive = raise, Negative = lower.")]
        [SerializeField] private float colliderVerticalOffset = 0f;
        
        [Header("Gameplay Modifiers")]
        [Tooltip("If true, player is immune to stagger while in this cover.")]
        [SerializeField] private bool grantStaggerImmunity = true;
        
        [Tooltip("Camera shake multiplier while in this cover (0 = no shake, 1 = full shake).")]
        [SerializeField, Range(0f, 1f)] private float shakeMultiplier = 0.25f;

        [Header("Validation")]
        [Tooltip("Maximum angle from the entry direction that the player can approach from.")]
        [SerializeField] private float maxApproachAngle = 60f;

        // Public accessors
        public Transform HideAnchor => hideAnchor;
        public Transform EntryPoint => entryPoint;
        public Transform ExitPoint => exitPoint;

        public float MaxLookYaw => maxLookYaw;
        public float MaxLookUp => maxLookUp;
        public float MaxLookDown => maxLookDown;
        public float HidingColliderHeight => hidingColliderHeight;
        public float ColliderVerticalOffset => colliderVerticalOffset;
        public bool GrantStaggerImmunity => grantStaggerImmunity;
        public float ShakeMultiplier => shakeMultiplier;

        public float MaxApproachAngle => maxApproachAngle;


        public float HiddenFacingYaw => hideAnchor != null ? hideAnchor.eulerAngles.y : 0f;

        public bool IsValidApproach(Transform interactor)
        {
            if (hideAnchor == null) return true;
            
            Vector3 directionToPlayer = (interactor.position - hideAnchor.position).normalized;
            directionToPlayer.y = 0;
            
            Vector3 entryForward = hideAnchor.forward;
            entryForward.y = 0;
            
            float angle = Vector3.Angle(entryForward, directionToPlayer);
            return angle <= maxApproachAngle;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw hide anchor
            if (hideAnchor != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(hideAnchor.position, 0.2f);
                Gizmos.DrawRay(hideAnchor.position, hideAnchor.forward * 0.5f);
            }
            
            // Draw exit point
            if (exitPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(exitPoint.position, 0.2f);
                Gizmos.DrawRay(exitPoint.position, exitPoint.forward * 0.5f);
            }
            
            // Draw approach cone
            if (hideAnchor != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Vector3 leftBound = Quaternion.Euler(0, -maxApproachAngle, 0) * hideAnchor.forward;
                Vector3 rightBound = Quaternion.Euler(0, maxApproachAngle, 0) * hideAnchor.forward;
                Gizmos.DrawRay(hideAnchor.position, leftBound * 1.5f);
                Gizmos.DrawRay(hideAnchor.position, rightBound * 1.5f);
            }
        }
    }
}
