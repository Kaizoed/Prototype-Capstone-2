using TMPro;
using UnityEngine;

public class PlayerItemUse : MonoBehaviour
{
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private GameObject flashlightObject;
    [SerializeField] private TMP_Text feedbackText;

    private bool flashlightOn = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            UseEquippedItem();
        }
    }

    public void UseEquippedItem()
    {
        if (inventoryUI == null) return;

        Item equipped = inventoryUI.GetEquippedItem();

        if (equipped == null)
        {
            ShowFeedback("No item equipped.");
            return;
        }

        if (equipped.itemName == "Flashlight")
        {
            flashlightOn = !flashlightOn;

            if (flashlightObject != null)
                flashlightObject.SetActive(flashlightOn);

            ShowFeedback(flashlightOn ? "Flashlight turned on." : "Flashlight turned off.");
        }
        else if (equipped.itemName == "HealthKit")
        {
            ShowFeedback("First aid kit ready for emergencies.");
        }
        else if (equipped.itemName == "WaterBottle")
        {
            ShowFeedback("Water bottle selected.");
        }
        else if (equipped.itemName.Contains("Battery"))
        {
            ShowFeedback("Battery selected.");
        }
        else
        {
            ShowFeedback("Used: " + equipped.itemName);
        }
    }

    private void ShowFeedback(string message)
    {
        Debug.Log(message);

        if (feedbackText != null)
            feedbackText.text = message;
    }
}