using UnityEngine;
using ShakySurvival.Interactions;

public class NpcInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueData dialogueData;

    [Header("Quest")]
    [SerializeField] private string questStepId = "TalkToClassmate";
    [SerializeField] private bool completeQuestOnInteract = true;

    private bool _hasCompletedQuestStep = false;

    public string InteractionPrompt => "Press F to Talk";

    public bool CanInteract(GameObject interactor)
    {
        return dialogueManager != null && !dialogueManager.IsDialogueActive;
    }

    public void Interact(GameObject interactor)
    {
        if (dialogueManager != null && dialogueData != null)
        {
            dialogueManager.StartDialogue(dialogueData);
        }

        if (completeQuestOnInteract && !_hasCompletedQuestStep && QuestManager.Instance != null)
        {
            Debug.Log("Completing quest step: " + questStepId);
            QuestManager.Instance.CompleteStep(questStepId);
            _hasCompletedQuestStep = true;
        }
    }
}