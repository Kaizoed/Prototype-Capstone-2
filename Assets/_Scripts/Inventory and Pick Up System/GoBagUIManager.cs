using UnityEngine;
using TMPro;
using ShakySurvival.Player;

public class GoBagUIManager : MonoBehaviour
{
    public static GoBagUIManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject goBagPanel;
    [SerializeField] private TMP_Text checklistText;

    [Header("Hard Hat Integration")]
    [SerializeField, Tooltip("Reference to the HardHatImmersiveManager on the player. " +
        "When the Helmet is picked up, this unlocks the equip ability.")]
    private HardHatImmersiveManager hardHatManager;

    private bool hasFlashlight;
    private bool hasHealthKit;
    private bool hasWaterBottle;
    private bool hasBattery;
    private bool hasWhistle;
    private bool hasMask;
    private bool hasHelmet;
    private bool isComplete;
    private bool hardhatTutorialShown;

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

            case "Whistle":
                hasWhistle = true;
                break;

            case "Mask":
                hasMask = true;
                break;

            case "Helmet":
                hasHelmet = true;

                if (hardHatManager != null)
                    hardHatManager.GiveHardHat();

                if (!hardhatTutorialShown && TutorialManager.Instance != null)
                {
                    hardhatTutorialShown = true;
                    TutorialManager.Instance.ShowTutorial("Press H to wear hardhat.", 3f);
                }
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
            $"{(hasBattery ? "[DONE]" : "[ ]")} Battery\n" +
            $"{(hasWhistle ? "[DONE]" : "[ ]")} Whistle\n" +
            $"{(hasMask ? "[DONE]" : "[ ]")} Mask\n" +
            $"{(hasHelmet ? "[DONE]" : "[ ]")} Helmet";
    }

    private void CheckIfComplete()
    {
        if (hasFlashlight && hasHealthKit && hasWaterBottle && hasBattery && hasWhistle && hasMask && hasHelmet)
        {
            if (isComplete) return;

            isComplete = true;
            Debug.Log("Go Bag complete!");

            if (TutorialObjectiveUI.Instance != null)
            {
                TutorialObjectiveUI.Instance.CompleteObjective("go_bag");
            }

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