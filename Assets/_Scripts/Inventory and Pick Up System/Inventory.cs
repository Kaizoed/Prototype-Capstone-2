using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    public List<Item> items = new List<Item>();
    public int inventorySize = 20;

    private InventoryUI inventoryUI;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        inventoryUI = FindFirstObjectByType<InventoryUI>();
    }

    public bool AddItem(Item item)
    {
        if (items.Count >= inventorySize)
        {
            Debug.Log("Inventory Full!");
            return false;
        }

        items.Add(item);
        Debug.Log(item.itemName + " added to inventory.");

        if (inventoryUI != null)
            inventoryUI.RefreshInventoryUI();

        return true;
    }
}