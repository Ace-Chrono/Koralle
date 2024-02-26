using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Item
{
    public ItemData data { get; private set; }
    
    public Item (ItemData source)
    {
        data = source;
    }
}
