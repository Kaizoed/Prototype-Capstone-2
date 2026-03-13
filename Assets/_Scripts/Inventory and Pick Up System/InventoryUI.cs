using System.Text;
using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private TMP_Text inventoryText;

    [Header("Player Control")]
    [SerializeField] private MonoBehaviour playerMovement;
    [SerializeField] private MonoBehaviour mouseLook;

    private bool isOpen = false;

    // NEW: currently equipped item
    private Item equippedItem;

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

        // NEW: check equip keys while inventory is open
        if (isOpen)
        {
            CheckEquipInput();
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(isOpen);

        if (playerMovement != null)
            playerMovement.enabled = !isOpen;

        if (mouseLook != null)
            mouseLook.enabled = !isOpen;

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
            Item item = Inventory.instance.items[i];

            if (item != null)
            {
                sb.Append((i + 1) + ". " + item.itemName);

                // NEW: show equipped item
                if (item == equippedItem)
                {
                    sb.Append(" [EQUIPPED]");
                }

                sb.AppendLine();
            }
        }

        inventoryText.text = sb.ToString();
    }

    // NEW: number key equip system
    private void CheckEquipInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipItem(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipItem(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipItem(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) EquipItem(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) EquipItem(4);
    }

    private void EquipItem(int index)
    {
        if (Inventory.instance == null) return;

        if (index < Inventory.instance.items.Count)
        {
            equippedItem = Inventory.instance.items[index];

            Debug.Log("Equipped: " + equippedItem.itemName);

            RefreshInventoryUI();
        }
    }

    // OPTIONAL: other scripts can check this
    public Item GetEquippedItem()
    {
        return equippedItem;
    }
}