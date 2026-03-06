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

    private bool isOpen;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string InteractionPrompt => isOpen ? closeText : openText;

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
        isOpen = !isOpen;
    }
}