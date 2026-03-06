using UnityEngine;
using ShakySurvival.Interactions;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Pivot")]
    [SerializeField] private Transform doorPivot;

    [Header("Door Settings")]
    [SerializeField] private float openAngle = -90f;
    [SerializeField] private float openSpeed = 4f;

    [Header("Prompt Text")]
    [SerializeField] private string openText = "Open Door";
    [SerializeField] private string closeText = "Close Door";
    [SerializeField] private string lockedText = "Door is locked";

    [Header("Quest Lock")]
    [SerializeField] private bool lockedUntilQuestStep = true;
    [SerializeField] private string requiredQuestStepId = "Go To Classroom";

    private bool isOpen;
    private bool hasBeenUnlocked;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string InteractionPrompt
    {
        get
        {
            if (IsLocked())
                return lockedText;

            return isOpen ? closeText : openText;
        }
    }

    private void Start()
    {
        if (doorPivot == null)
        {
            Debug.LogError("DoorInteractable: doorPivot not assigned.", this);
            enabled = false;
            return;
        }

        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    private void Update()
    {
        Quaternion target = isOpen ? openRotation : closedRotation;
        doorPivot.localRotation = Quaternion.Slerp(
            doorPivot.localRotation,
            target,
            Time.deltaTime * openSpeed
        );
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public void Interact(GameObject interactor)
    {
        if (IsLocked())
        {
            Debug.Log("Door is locked.");
            return;
        }

        isOpen = !isOpen;
    }

    private bool IsLocked()
    {
        if (!lockedUntilQuestStep)
            return false;

        if (hasBeenUnlocked)
            return false;

        if (QuestManager.Instance == null)
            return true;

        // Unlock permanently once this quest step becomes current
        if (QuestManager.Instance.CurrentStepId == requiredQuestStepId)
        {
            hasBeenUnlocked = true;
            return false;
        }

        return true;
    }
}