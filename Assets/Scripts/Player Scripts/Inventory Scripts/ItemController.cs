using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemController : MonoBehaviour
{
    public ItemData referenceItem;

    public void OnHandlePickupItem()
    {
        InventoryManager.Instance.Add(referenceItem);
        Destroy(gameObject);
    }
}
