using UnityEngine;
using ShakySurvival.Interactions;

public class ClassmateInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ClassmateSequenceController sequenceController;

    public string InteractionPrompt
    {
        get
        {
            if (sequenceController == null) return "";

            switch (sequenceController.CurrentState)
            {
                case ClassmateSequenceController.ClassmateState.Idle:
                    return "Press F to Talk";
                case ClassmateSequenceController.ClassmateState.WaitingForHelp:
                    return "Press F to Help";
                default:
                    return "";
            }
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        if (sequenceController == null) return false;

        return sequenceController.CurrentState == ClassmateSequenceController.ClassmateState.Idle ||
               sequenceController.CurrentState == ClassmateSequenceController.ClassmateState.WaitingForHelp;
    }

    public void Interact(GameObject interactor)
    {
        sequenceController?.HandlePlayerInteract(interactor);
    }
}