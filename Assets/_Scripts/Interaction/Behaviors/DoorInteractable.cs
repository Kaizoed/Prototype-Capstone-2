using UnityEngine;

namespace ShakySurvival.Interactions.Behaviors
{
    // Just a placeholder until we implemented doors
    // A basic door interaction that toggles between open and closed states.
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isLocked = false;
        [SerializeField] private string lockedMessage = "Locked";
        
        [Header("Animation/Feedback")]
        [SerializeField] private Animator animator;
        [SerializeField] private string openAnimationParameter = "IsOpen";
        
        // This could be replaced with a robust localization system later
        public string InteractionPrompt => isLocked ? lockedMessage : (isOpen ? "Close" : "Open");

        public bool CanInteract(GameObject interactor)
        {
            // Add custom conditions here (just in case we added more feature)
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (isLocked)
            {
                // Play locked sound or animation
                Debug.Log("Door is locked.");
                return;
            }

            ToggleDoor();
        }

        private void ToggleDoor()
        {
            isOpen = !isOpen;
            
            if (animator != null)
            {
                animator.SetBool(openAnimationParameter, isOpen);
            }
            
            // Play sound or something
            Debug.Log($"Door is now {(isOpen ? "Open" : "Closed")}");
        }
        
        // Helper to unlock via other game logic
        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }
    }
}
