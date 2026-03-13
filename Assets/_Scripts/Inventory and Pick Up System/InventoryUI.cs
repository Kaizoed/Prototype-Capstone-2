using System.Text;
using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private TMP_Text inventoryText;

    private bool isOpen = false;

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(isOpen);

        if (isOpen)
        {
            RefreshInventoryUI();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void RefreshInventoryUI()
    {
        if (inventoryText == null || Inventory.instance == null) return;

        if (Inventory.instance.items.Count == 0)
        {
            inventoryText.text = "Inventory is empty.";
            return;
        }

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < Inventory.instance.items.Count; i++)
        {
            if (Inventory.instance.items[i] != null)
            {
                sb.AppendLine("- " + Inventory.instance.items[i].itemName);
            }
        }

        inventoryText.text = sb.ToString();
    }
}