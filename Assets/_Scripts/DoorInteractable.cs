using UnityEngine;
using ShakySurvival.Interactions;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Pivot")]
    [SerializeField] private Transform doorPivot;

    [Header("Side Detection Reference")]
    [SerializeField] private Transform sideCheckTransform;

    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 4f;

    [Header("Prompt Text")]
    [SerializeField] private string openText = "Open Door";
    [SerializeField] private string closeText = "Close Door";
    [SerializeField] private string lockedText = "Door is locked";

    [Header("Flow Lock")]
    [SerializeField] private bool lockedUntilEvacuation = false;

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

        if (sideCheckTransform == null)
            sideCheckTransform = transform;

        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation;
    }

    private void Update()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;

        doorPivot.localRotation = Quaternion.Slerp(
            doorPivot.localRotation,
            targetRotation,
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

        if (!isOpen)
        {
            SetOpenDirection(interactor);
            isOpen = true;
        }
        else
        {
            CloseDoor();
        }
    }

    public void OpenDoorFrom(GameObject opener)
    {
        if (IsLocked()) return;
        if (isOpen) return;

        SetOpenDirection(opener);
        isOpen = true;
    }

    public void OpenDoorForward()
    {
        if (IsLocked()) return;
        if (isOpen) return;

        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        isOpen = true;
    }

    public void OpenDoorBackward()
    {
        if (IsLocked()) return;
        if (isOpen) return;

        openRotation = closedRotation * Quaternion.Euler(0f, -openAngle, 0f);
        isOpen = true;
    }

    public void CloseDoor()
    {
        isOpen = false;
    }

    private void SetOpenDirection(GameObject opener)
    {
        if (opener == null)
        {
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            return;
        }

        Vector3 toOpener = opener.transform.position - sideCheckTransform.position;
        float dot = Vector3.Dot(sideCheckTransform.forward, toOpener);
        float finalAngle = dot >= 0f ? openAngle : -openAngle;

        openRotation = closedRotation * Quaternion.Euler(0f, finalAngle, 0f);
    }

    private bool IsLocked()
    {
        if (!lockedUntilEvacuation)
            return false;

        if (hasBeenUnlocked)
            return false;

        if (GameFlowManager.Instance == null)
            return true;

        if (GameFlowManager.Instance.currentStep == GameFlowManager.GameStep.Evacuate)
        {
            hasBeenUnlocked = true;
            return false;
        }

        return true;
    }
}