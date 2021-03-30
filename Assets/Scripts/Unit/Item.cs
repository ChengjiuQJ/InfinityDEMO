using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : Data
{
    public Item(Dictionary<HighValue,Dictionary<LowValue,IDataGetable>> data):base(data)
    {
    }
}
