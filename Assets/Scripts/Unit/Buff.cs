using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buff :Data,IDataGetable
{
    public int turn;
    protected Buff(Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> data):base(data)
    {
    }

    public Buff(int id,int turn)
    {
        this.id = id;
        this.turn=turn;
        datas= new Dictionary<HighValue, Dictionary<LowValue, IDataGetable>>();
    }
    public float GetData(HighValue high, LowValue low, LifeBody lifeBody = null)
    {
        if (datas.TryGetValue(high, out Dictionary<LowValue, IDataGetable> kv))
        {
            if (kv.TryGetValue(low, out IDataGetable data))
            {
                return data.GetData(high, low, lifeBody);
            }
        }
        return DataManager.Instance.RaceData[id].GetData(high,low,lifeBody);
    }
}
