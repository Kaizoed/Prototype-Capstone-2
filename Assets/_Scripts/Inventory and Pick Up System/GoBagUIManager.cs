using UnityEngine;
using TMPro;

public class GoBagUIManager : MonoBehaviour
{
    public static GoBagUIManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject goBagPanel;
    [SerializeField] private TMP_Text checklistText;

    private bool hasFlashlight;
    private bool hasHealthKit;
    private bool hasWaterBottle;
    private bool hasBattery;
    private bool isComplete;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (goBagPanel != null)
            goBagPanel.SetActive(false);

        UpdateChecklistUI();
    }

    public void AddItem(string itemName)
    {
        if (isComplete)
            return;

        switch (itemName)
        {
            case "Flashlight":
                hasFlashlight = true;
                break;

            case "Health Kit":
                hasHealthKit = true;
                break;

            case "Water Bottle":
                hasWaterBottle = true;
                break;

            case "Battery":
                hasBattery = true;
                break;

            default:
                Debug.LogWarning("Unknown Go Bag item: " + itemName);
                return;
        }

        UpdateChecklistUI();
        CheckIfComplete();
    }

    private void UpdateChecklistUI()
    {
        if (checklistText == null)
            return;

        checklistText.text =
            "GO BAG CHECKLIST\n\n" +
            $"{(hasFlashlight ? "[DONE]" : "[ ]")} Flashlight\n" +
            $"{(hasHealthKit ? "[DONE]" : "[ ]")} Health Kit\n" +
            $"{(hasWaterBottle ? "[DONE]" : "[ ]")} Water Bottle\n" +
            $"{(hasBattery ? "[DONE]" : "[ ]")} Battery";
    }

    private void CheckIfComplete()
    {
        if (hasFlashlight && hasHealthKit && hasWaterBottle && hasBattery)
        {
            isComplete = true;
            Debug.Log("Go Bag complete!");

            if (goBagPanel != null)
                goBagPanel.SetActive(false);

            IntroCutsceneManager introCutsceneManager = FindFirstObjectByType<IntroCutsceneManager>();
            if (introCutsceneManager != null)
            {
                introCutsceneManager.ContinueDialoguePart2();
            }
            else
            {
                Debug.LogWarning("IntroCutsceneManager not found. Cannot continue dialogue part 2.");
            }
        }
    }

    public void ShowGoBagPanel(bool show)
    {
        if (goBagPanel != null)
            goBagPanel.SetActive(show);

        if (show)
            UpdateChecklistUI();
    }
}