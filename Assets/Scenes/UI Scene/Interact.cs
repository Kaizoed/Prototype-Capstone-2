using UnityEngine;
using TMPro;


//Ilagay sa Camera ng Player
//Set the Game Object named "Interact" in the Canvas to off
public class Interact : MonoBehaviour
{
    public Camera playerCamera;
    public float interactionDistance;
    public GameObject interactionText;
    private InteractableObject currentInteractable;

    void Update()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            InteractableObject interactObject = hit.collider.GetComponent<InteractableObject>();
            if (interactObject != null && interactObject != currentInteractable)
            {
                currentInteractable = interactObject;
                interactionText.SetActive(true);
                TextMeshProUGUI textComponent = interactionText.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = currentInteractable.GetInteractionText();
                }
            }
        }

        else
        {
            currentInteractable = null;
            interactionText.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable?.Interact();
        }
    }
}