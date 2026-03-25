using UnityEngine;
using ShakySurvival.Interactions;

public class ObjectPickUp : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName = "Item";

    public string InteractionPrompt => "Pick up " + itemName;

    public bool CanInteract(GameObject interactor)
    {
        if (GameFlowManager.Instance == null) return false;

        return GameFlowManager.Instance.currentStep == GameFlowManager.GameStep.GoBag;
    }

    public void Interact(GameObject interactor)
    {
        if (GoBagUIManager.Instance != null)
        {
            GoBagUIManager.Instance.AddItem(itemName);
        }

        Destroy(gameObject);
    }
}