using UnityEngine;
using ShakySurvival.Interactions;

public class HelpFallenNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string helpPrompt = "Help Student Up";
    [SerializeField] private NPCEarthquakeReaction npcReaction;
    [SerializeField] private string helpQuestStepId = "help_fallen_npc";

    [Header("Safe Area Marker")]
    [SerializeField] private GameObject safeAreaMarker;

    private bool hasBeenHelped = false;

    public string InteractionPrompt => hasBeenHelped ? "" : helpPrompt;

    private void Awake()
    {
        if (npcReaction == null)
            npcReaction = GetComponent<NPCEarthquakeReaction>();

        if (safeAreaMarker != null)
            safeAreaMarker.SetActive(false);
    }

    public bool CanInteract(GameObject interactor)
    {
        if (hasBeenHelped) return false;
        if (npcReaction == null) return false;

        return npcReaction.IsCurrentlyFallen();
    }

    public void Interact(GameObject interactor)
    {
        if (hasBeenHelped) return;
        if (npcReaction == null) return;
        if (!npcReaction.IsCurrentlyFallen()) return;

        hasBeenHelped = true;

        npcReaction.HelpUp();

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteStep(helpQuestStepId);
        }

        if (safeAreaMarker != null)
        {
            safeAreaMarker.SetActive(true);
        }

        Debug.Log("Student helped up.");
    }
}