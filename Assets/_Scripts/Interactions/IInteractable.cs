using UnityEngine;

namespace ShakySurvival.Interactions
{

    // Contract for any object that the player can interact with.
    public interface IInteractable
    {
        // Interaction prompt to display (e.g., "Open Door", "Hide").
        string InteractionPrompt { get; }

        // Checks if the interaction is valid for the given interactor.
        bool CanInteract(GameObject interactor);

        // Executes the interaction.
        void Interact(GameObject interactor);
    }
}
