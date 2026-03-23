using System.Collections.Generic;
using UnityEngine;

namespace ShakySurvival.Interactions.Examine
{
    [RequireComponent(typeof(Collider))]
    public class PhysicalBag : MonoBehaviour, IInteractable
    {
        [Header("Required Items")]
        [Tooltip("Items the player must deposit into the bag.")]
        [SerializeField] private List<ExamineData> requiredItems = new List<ExamineData>();

        [Header("Bag Examine")]
        [Tooltip("ExamineInteractable on this object (start DISABLED in Inspector).")]
        [SerializeField] private ExamineInteractable bagExamineComponent;

        private readonly HashSet<ExamineData> _depositedItems = new HashSet<ExamineData>();

        public bool IsComplete => _depositedItems.Count >= requiredItems.Count;

        // ── IInteractable ───────────────────────────────────────

        public string InteractionPrompt
        {
            get
            {
                if (IsComplete) return string.Empty;

                int current = _depositedItems.Count;
                int total = requiredItems.Count;

                PlayerInventory inventory = PlayerInventory.Instance;
                if (inventory == null)
                    return $"Deposit Items ({current}/{total})";

                // Check if the player has at least one matching item
                bool hasAny = false;
                foreach (ExamineData item in requiredItems)
                {
                    if (!_depositedItems.Contains(item) && inventory.HasItem(item))
                    {
                        hasAny = true;
                        break;
                    }
                }

                return hasAny
                    ? $"Deposit Items ({current}/{total})"
                    : $"No matching items ({current}/{total})";
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            if (IsComplete) return false;

            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null) return false;

            foreach (ExamineData item in requiredItems)
            {
                if (!_depositedItems.Contains(item) && inventory.HasItem(item))
                    return true;
            }

            return false;
        }

        public void Interact(GameObject interactor)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            // Deposit the first matching item (one per interaction)
            foreach (ExamineData item in requiredItems)
            {
                if (_depositedItems.Contains(item)) continue;

                if (inventory.RemoveItem(item))
                {
                    _depositedItems.Add(item);
                    Debug.Log($"[PhysicalBag] Deposited: {item.objectName} ({_depositedItems.Count}/{requiredItems.Count})");
                    break;
                }
            }

            if (IsComplete)
            {
                Debug.Log("[PhysicalBag] All items deposited. Bag is now examinable.");

                // Switch from deposit point to examinable object
                if (bagExamineComponent != null)
                    bagExamineComponent.enabled = true;

                enabled = false;
            }
        }
    }
}
