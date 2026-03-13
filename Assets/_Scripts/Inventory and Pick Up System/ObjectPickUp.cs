using UnityEngine;
using ShakySurvival.Interactions;

public class ObjectPickUp : MonoBehaviour, IInteractable
{
    [SerializeField] private Item item;
    [SerializeField] private string pickupPrompt = "Pick up";

    public string InteractionPrompt
    {
        get
        {
            if (item != null)
                return $"{pickupPrompt} {item.itemName}";
            return pickupPrompt;
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        if (item == null) return false;
        if (Inventory.instance == null) return false;
        if (Inventory.instance.items.Count >= Inventory.instance.inventorySize) return false;

        return true;
    }

    public void Interact(GameObject interactor)
    {
        if (item == null)
        {
            Debug.LogWarning("[ObjectPickUp] No item assigned.");
            return;
        }

        if (Inventory.instance == null)
        {
            Debug.LogWarning("[ObjectPickUp] Inventory instance not found.");
            return;
        }

        bool added = Inventory.instance.AddItem(item);

        if (added)
        {
            Destroy(gameObject);
        }
    }
}