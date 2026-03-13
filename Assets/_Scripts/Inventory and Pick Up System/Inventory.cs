using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    public List<Item> items = new List<Item>();
    public int inventorySize = 20;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void AddItem(Item item)
    {
        if (items.Count >= inventorySize)
        {
            Debug.Log("Inventory Full!");
            return;
        }

        items.Add(item);
        Debug.Log(item.itemName + " added to inventory.");
    }
}