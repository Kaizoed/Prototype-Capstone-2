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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateChecklistUI();
    }

    public void AddItem(string itemName)
    {
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
        }

        UpdateChecklistUI();
        CheckIfComplete();
    }

    void UpdateChecklistUI()
    {
        checklistText.text =
            "GO BAG CHECKLIST\n\n" +
            $"{(hasFlashlight ? "[✓]" : "[ ]")} Flashlight\n" +
            $"{(hasHealthKit ? "[✓]" : "[ ]")} Health Kit\n" +
            $"{(hasWaterBottle ? "[✓]" : "[ ]")} Water Bottle\n" +
            $"{(hasBattery ? "[✓]" : "[ ]")} Battery";
    }

    void CheckIfComplete()
    {
        if (hasFlashlight && hasHealthKit && hasWaterBottle && hasBattery)
        {
            Debug.Log("Go Bag complete!");
            goBagPanel.SetActive(false);

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.SetStep(GameFlowManager.GameStep.LectureDuckCoverHold);
            }
        }
    }

    public void ShowGoBagPanel(bool show)
    {
        goBagPanel.SetActive(show);
    }
}