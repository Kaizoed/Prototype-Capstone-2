using UnityEngine;
using UnityEngine.Events;

namespace ShakySurvival.Interactions.Behaviors
{
    // Just a placeholder until we implemented items
    // A basic item interaction
    public class ItemInteractable : MonoBehaviour, IInteractable
    {
        [Header("Item Data")]
        [SerializeField] private string itemName = "Item";
        [SerializeField] private bool destroyOnPickup = true;
        
        [Header("Events")]
        public UnityEvent OnPickup;

        public string InteractionPrompt => $"Pick up {itemName}";

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        public void Interact(GameObject interactor)
        {
            Debug.Log($"Picked up {itemName}");
            
            OnPickup?.Invoke();

            // Add inventory logic here (just in case we added inventory)
            // interactor.GetComponent<Inventory>().AddItem(itemData); something along these lines

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }
}
