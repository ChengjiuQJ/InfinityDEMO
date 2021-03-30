using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Equipment :Item,IDataGetable
{
    public static Equipment UnArmed { get { return DataManager.Instance.EquipmentData[0]; } }
    public EquipmentType type;
    public Equipment(Dictionary<HighValue,Dictionary<LowValue,IDataGetable>> data):base(data)
    {
    }
    public float GetData(HighValue high, LowValue low,LifeBody lifeBody=null)
    {
        if (datas.TryGetValue(high, out Dictionary<LowValue, IDataGetable> kv))
        {
            if (kv.TryGetValue(low, out IDataGetable data))
            {
                return data.GetData(high, low,lifeBody);
            }
        }
        float result = 0f;
        return result;
    }
    public string GetText(HighValue high,LowValue low)
    {
        if (datas.TryGetValue(high, out Dictionary<LowValue, IDataGetable> kv))
        {
            if (kv.TryGetValue(low, out IDataGetable data))
            {
                var d = (DataValue)data;
                return d.GetText();
            }
        }
        return string.Empty;
    }
}
public enum EquipmentType
{
    NONE,WEAPON,HELMET
}
