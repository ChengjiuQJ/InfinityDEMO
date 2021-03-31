using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager 
{
    private static DataManager instance;
    public static DataManager Instance
    {
        get
        {
            if (instance != null)
                return instance;
            instance = new DataManager();
            return instance;
        }
    }
    public Dictionary<int, Race> RaceData;
    public Dictionary<int, Equipment> EquipmentData;
    public Dictionary<int,Buff> BuffData;
    public Dictionary<int,Skill> SkillData;
    public bool Ready { get; private set; }

    public float GetHighValue(LifeBody body,HighValue value)
    {
        float baseValue = GetLowValue(body, value, LowValue.基础值);
        float addValue = GetLowValue(body, value, LowValue.基础附加值);
        float percentValue = GetLowValue(body, value, LowValue.百分比);
        float extraValue = GetLowValue(body, value, LowValue.额外固定值);
        return (baseValue + addValue) * (1 + percentValue) + extraValue;
        
    }

    public float GetLowValue(LifeBody lifeBody,HighValue high, LowValue low)
    {
        lifeBody.TryGetData(high, low, out float result);
        result += DataManager.Instance.RaceData[lifeBody.race].GetData(high, low,lifeBody); 
        foreach(var equip in lifeBody.CurrentEquipments)
        {
            result += equip.GetData(high, low,lifeBody);
        }
        foreach(var buff in lifeBody.CurrentBuffs)
        {
            result += buff.GetData(high, low,lifeBody);
        }
        return result;
    }

    public void LoadData(string path,out Dictionary<int,Dictionary<HighValue,Dictionary<LowValue,IDataGetable>>>datas)
    {
        datas = new Dictionary<int, Dictionary<HighValue, Dictionary<LowValue, IDataGetable>>>();
        string text =  Resources.Load(path).ToString();
        string[] lines = text.Split('\n');
        string[] firstLine = lines[0].Split(new char[] { ','},System.StringSplitOptions.RemoveEmptyEntries);
        Tuple<HighValue, LowValue>[] tuples = new Tuple<HighValue, LowValue>[firstLine.Length - 1];
        for(int i=1;i<firstLine.Length;i++)
        {
            string[] temp = firstLine[i].Split('_');
            HighValue high =(HighValue)Enum.Parse(typeof(HighValue), temp[0]);
            LowValue low = (LowValue)Enum.Parse(typeof(LowValue), temp[1]);
            tuples[i - 1] = Tuple.Create(high, low);
        }
        for(int i=1;i<lines.Length;i++)
        {
            string[] nodes = lines[i].Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            if(int.TryParse(nodes[0],out int ID))
            {
                Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> temp = new Dictionary<HighValue, Dictionary<LowValue, IDataGetable>>();
                for(int j=1;j<nodes.Length;j++)
                {
                    if(float.TryParse(nodes[j],out float data))
                    {
                        if (temp.ContainsKey(tuples[j - 1].Item1))
                            temp[tuples[j - 1].Item1][tuples[j - 1].Item2] = new DataValue(data);
                        else
                            temp.Add(tuples[j - 1].Item1, new Dictionary<LowValue, IDataGetable>() { { tuples[j - 1].Item2, new DataValue(data) } });
                    }
                    else
                    {
                        if (TryParseExp(nodes[j], out ValueExpression exp))
                        {
                            if (temp.ContainsKey(tuples[j - 1].Item1))
                                temp[tuples[j - 1].Item1][tuples[j - 1].Item2] = exp;
                            else
                                temp.Add(tuples[j - 1].Item1, new Dictionary<LowValue, IDataGetable>() { { tuples[j - 1].Item2, exp } });
                        }
                        else
                            temp.Add(tuples[j - 1].Item1, new Dictionary<LowValue, IDataGetable>() { { tuples[j - 1].Item2, new DataValue(nodes[j]) } });
                    }
                }
                datas.Add(ID, temp);
            }
        }
    }

    private void LoadData<T>(string path,out Dictionary<int,T> datas)where T:Data
    {
        LoadData(path, out var data);
        datas = new Dictionary<int, T>();
        foreach (var d in data)
        {
            datas.Add(d.Key, (T)Activator.CreateInstance(typeof(T),d.Value));
        }
    }

    internal void LoadAllData()
    {
        LoadData("Race", out RaceData);
        LoadData("Equipment",out EquipmentData);
        LoadData("Buff",out BuffData);
        LoadData("Skill",out SkillData);
        Ready = true;
    }

    private bool TryParseExp(string source,out ValueExpression result )
    {
        try
        {
            string[] nodes = source.Split('#');
            List<Param> @params = new List<Param>();
            foreach (string s in nodes)
            {
                switch(s)
                {
                    case "+":
                        @params.Add(new Param((float x, float y) => x + y));
                        break;
                    case "-":
                        @params.Add(new Param((float x, float y) => x - y));
                        break;
                    case "*":
                        @params.Add(new Param((float x, float y) => x * y));
                        break;
                    case "/":
                        @params.Add(new Param((float x, float y) => x / y));
                        break;
                    default:
                        if(Enum.TryParse<HighValue>(s,out HighValue high))
                        {
                            @params.Add( new Param(Tuple.Create<HighValue,Func<LifeBody,HighValue,float>>(high,GetHighValue)));
                        }
                        else if(float.TryParse(s,out float f))
                        {
                            @params.Add(new Param(f));
                        }
                        else
                        {
                            result = null;
                            return false;
                        }
                        break;
                            
                }
            }
            result = new ValueExpression(@params);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }       
    }
}

public class Data
{
    public int id;
    public Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> datas;
    protected Data(Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> data)
    {
        datas = data;
    }
    protected Data()
    {
    }
}
