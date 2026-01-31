using UnityEngine;

namespace ShakySurvival.Interactions.Behaviors
{
    public class NPCInteractable : MonoBehaviour, IInteractable
    {
        [Header("NPC Settings")]
        [SerializeField] private string npcName = "Stranger";
        [SerializeField] private bool canTalk = true;

        public string InteractionPrompt => $"Talk to {npcName}";

        public bool CanInteract(GameObject interactor)
        {
            // Example: Check if NPC is busy or dead (just in case we need certain conditions met)
            return canTalk;
        }

        public void Interact(GameObject interactor)
        {
            if (!canTalk) return;

            Debug.Log($"Started conversation with {npcName}");
            
            // Trigger Interaction System
            
            // Face the player
            Vector3 direction = (interactor.transform.position - transform.position).normalized;
            direction.y = 0; // Keep looking flat (we can just adjust this if we need to)
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
