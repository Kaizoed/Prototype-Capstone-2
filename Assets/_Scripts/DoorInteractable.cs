using UnityEngine;

public class DoorInteractablePivot : MonoBehaviour
{
    [Header("Door Pivot (the thing that rotates the door)")]
    [SerializeField] private Transform doorPivot;

    [Header("UI")]
    [SerializeField] private GameObject pressUI;
    [SerializeField] private string openText = "Press F to Open";
    [SerializeField] private string closeText = "Press F to Close";

    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f; // change to -90 if wrong direction
    [SerializeField] private float openSpeed = 4f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private bool inRange;
    private bool isOpen;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        if (!doorPivot)
        {
            Debug.LogError("DoorInteractablePivot: Assign doorPivot (door-rotate-square-d).");
            enabled = false;
            return;
        }

        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        if (pressUI) pressUI.SetActive(false);
        UpdateUIText();
    }

    void Update()
    {
        if (inRange && Input.GetKeyDown(interactKey))
        {
            isOpen = !isOpen;
            UpdateUIText();
        }

        Quaternion target = isOpen ? openRotation : closedRotation;
        doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, target, Time.deltaTime * openSpeed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = true;
        if (pressUI) pressUI.SetActive(true);
        UpdateUIText();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = false;
        if (pressUI) pressUI.SetActive(false);
    }

    void UpdateUIText()
    {
        if (!pressUI) return;

        var tmp = pressUI.GetComponent<TMPro.TMP_Text>();
        if (tmp) tmp.text = isOpen ? closeText : openText;
    }
}