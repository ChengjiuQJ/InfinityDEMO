using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buff : Data, IDataGetable
{
    public int turn;
    public Buff(Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> data) : base(data)
    {

    }

    public Buff(int id, int turn)
    {
        this.id = id;
        this.turn = turn;
        datas = new Dictionary<HighValue, Dictionary<LowValue, IDataGetable>>();
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
        return GetBaseData(high, low, lifeBody);
    }
    public float GetBaseData(HighValue high, LowValue low, LifeBody lifeBody = null)
    {
        if (DataManager.Instance.BuffData.TryGetValue(id, out var data))
        {
            if (data.datas.TryGetValue(high, out Dictionary<LowValue, IDataGetable> kv))
            {
                if (kv.TryGetValue(low, out IDataGetable d))
                {
                    return d.GetData(high, low, lifeBody);
                }
            }
        }
        return 0f;
    }
}
