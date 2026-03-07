using UnityEngine;
using System.Collections;

namespace ShakySurvival.Interactions.Behaviors
{
    public enum DoorState
    {
        Closed,
        OpenIn,
        OpenOut
    }

    public enum DetectionAxis
    {
        Forward,
        Up,
        Right
    }

    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private string lockedMessage = "Locked";

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [Tooltip("How long to block re-interaction after triggering an animation.")]
        [SerializeField] private float animationDuration = 1.0f;

        [Header("Side Detection")]
        [Tooltip("Transform whose axes are used for side detection. Defaults to this transform.")]
        [SerializeField] private Transform doorTransform;
        [Tooltip("Which local axis points THROUGH the door (perpendicular to the door face). Check the colored arrows in Scene view.")]
        [SerializeField] private DetectionAxis detectionAxis = DetectionAxis.Forward;

        private static readonly int SideParam = Animator.StringToHash("Side");
        private static readonly int OpenTrigger = Animator.StringToHash("Open");
        private static readonly int CloseTrigger = Animator.StringToHash("Close");

        private DoorState _currentState = DoorState.Closed;
        private bool _isAnimating = false;

        public DoorState CurrentState => _currentState;

        public string InteractionPrompt
        {
            get
            {
                if (isLocked) return lockedMessage;
                if (_isAnimating) return string.Empty;
                return _currentState == DoorState.Closed ? "Open" : "Close";
            }
        }

        private void Awake()
        {
            if (doorTransform == null)
                doorTransform = transform;
        }

        public bool CanInteract(GameObject interactor)
        {
            return !_isAnimating;
        }

        public void Interact(GameObject interactor)
        {
            if (isLocked)
            {
                Debug.Log("Door is locked.");
                return;
            }

            if (_isAnimating) return;

            if (_currentState == DoorState.Closed)
            {
                OpenDoor(interactor);
            }
            else
            {
                CloseDoor();
            }
        }

        private void OpenDoor(GameObject interactor)
        {
            float dot = GetSideDot(interactor.transform);

            _currentState = dot >= 0f ? DoorState.OpenIn : DoorState.OpenOut;

            if (animator != null)
            {
                animator.SetFloat(SideParam, dot);
                animator.SetTrigger(OpenTrigger);
            }

            StartCoroutine(AnimationCooldown());
            Debug.Log($"Door: Open ({(dot >= 0f ? "In" : "Out")}, dot: {dot:F2})");
        }

        private void CloseDoor()
        {
            if (animator != null)
            {
                animator.SetTrigger(CloseTrigger);
            }

            _currentState = DoorState.Closed;
            StartCoroutine(AnimationCooldown());
            Debug.Log("Door: Close");
        }

        private Vector3 GetDetectionDirection()
        {
            switch (detectionAxis)
            {
                case DetectionAxis.Up:      return doorTransform.up;
                case DetectionAxis.Right:   return doorTransform.right;
                case DetectionAxis.Forward:
                default:                    return doorTransform.forward;
            }
        }

        private float GetSideDot(Transform player)
        {
            // Get the configured axis and flatten to horizontal
            Vector3 throughDoor = GetDetectionDirection();
            throughDoor.y = 0f;
            throughDoor.Normalize();

            // Flatten positions to remove height difference
            Vector3 doorPos = doorTransform.position;
            Vector3 playerPos = player.position;
            doorPos.y = 0f;
            playerPos.y = 0f;

            Vector3 toPlayer = (playerPos - doorPos).normalized;
            return Vector3.Dot(throughDoor, toPlayer);
        }

        private IEnumerator AnimationCooldown()
        {
            _isAnimating = true;
            yield return new WaitForSeconds(animationDuration);
            _isAnimating = false;
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }
    }
}
