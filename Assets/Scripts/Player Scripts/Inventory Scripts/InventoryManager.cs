using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    private Dictionary<ItemData, Item> itemDictionary; 
    public List<Item> inventory { get; private set; }

    private void Awake()
    {
        Instance = this; 
        inventory = new List<Item>();
        itemDictionary = new Dictionary<ItemData, Item>();
    }

    public void Add(ItemData referenceData)
    {
        if (itemDictionary.TryGetValue(referenceData, out Item value))
        {
            return;
        }
        else
        {
            Item newItem = new Item(referenceData);
            inventory.Add(newItem);
            itemDictionary.Add(referenceData, newItem);
        }
    }

    public void Remove(ItemData referenceData)
    {
        if (itemDictionary.TryGetValue(referenceData, out Item value))
        {
            inventory.Remove(value);
            itemDictionary.Remove(referenceData);
        }
    }

    public Item Get(ItemData referenceData)
    {
        itemDictionary.TryGetValue(referenceData, out Item value);
        return value; 
    }
}
