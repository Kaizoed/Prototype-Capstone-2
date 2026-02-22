using UnityEngine;
using UnityEngine.Events;

//Ilagay sa object na gusto niyo na may "Press E" kineme
//Set the Game Object named "Interact" in the Canvas to off
public class InteractableObject : MonoBehaviour
{
    public string interactionText = "Press E to Interact";
    public UnityEvent onInteract;

    public string GetInteractionText()
    {
        return interactionText;
    }

    public void Interact()
    {
        onInteract.Invoke();
    }
}