using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Race:Data,IDataGetable
{   
    public string Name { get; set; }

    public Race(Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> data) : base(data)
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



}
public enum RaceType
{
    None,Human,Zombie
}
