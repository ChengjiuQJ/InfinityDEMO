using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    static BattleManager _instance;
    public static BattleManager Instance { get { return _instance; } }
    private List<Battle> currentBattles;

    private void Awake()
    {
        _instance = this;
        currentBattles = new List<Battle>();
    }

    public void CreatBattle(List<LifeBody> part1,List<LifeBody>part2,bool mainBattle=false)
    {
        Battle battle = new Battle(part1, part2);
        if(mainBattle)
            GameManager.Instance.FSM.ChangeState(StateType.BattleMode);
        currentBattles.Add(battle);
        battle.BattleBegin();
    }

    public void BattleFinish(Battle battle)
    {
        currentBattles.Remove(battle);
    }

    private void Update()
    {
        for(int i=0; i<currentBattles.Count;i++)
        {
            currentBattles[i].Update();
        }
    }
}

public class Battle
{
    public event EventHandler OnBattlePause;
    List<LifeBody> part1;
    private int P1Lives
    {
        get
        {
            int count = 0;
            foreach (var body in part1)
                if (!body.IsDead)
                    count++;
            return count;
        }
    }
    List<LifeBody> part2;
    private int P2Lives
    {
        get
        {
            int count = 0;
            foreach (var body in part2)
                if (!body.IsDead)
                    count++;
            return count;
        }
    }
    public List<LifeBody> CurrentQueue { get; private set; }
    public Battle(List<LifeBody> p1,List<LifeBody> p2)
    {
        part1 = p1;
        part2 = p2;        
    }
    public void Update()
    {
        if(IsOver)
        {
            BattleFinish();
        }
    }
    private List<LifeBody> GetLives()
    {
        List<LifeBody> temp = new List<LifeBody>();
        foreach (var body in part1)
        {
            if (!body.IsDead)
                temp.Add(body);
        }
        foreach (var body in part2)
        {
            if (!body.IsDead)
                temp.Add(body);
        }
        temp.Sort((x, y) => -x.Speed.CompareTo(y.Speed));
        return temp;
    }

    public void BattleBegin()
    {
        GameManager.Instance.PlayBGM(1);
        foreach (var p1 in part1)
        {
            p1.FSM.ChangeState(StateType.battleStart, this);
        }
        foreach (var p2 in part2)
        {
            p2.FSM.ChangeState(StateType.battleStart, this);
        }

        foreach (var p1 in part1)
        {
            p1.FSM.ChangeState(StateType.battleIdle);
        }
        foreach (var p2 in part2)
        {
            p2.FSM.ChangeState(StateType.battleIdle);
        }
        UIManager.Instance.BattleStart();
        BattleManager.Instance.StartCoroutine(TurnBegin());      
    }

    public IEnumerator TurnBegin()
    {
        yield return new WaitWhile(() => UIManager.Instance.Playing);
        var bodies = GetLives();
        while(bodies.Count>0)
        {
            LifeBody body = bodies[0];
            if (body.FSM.GetCurrentState() == StateType.Dead)
            {
                bodies.Remove(body);
                continue;
            }
            body.FSM.ChangeState(StateType.turnBegin);
            while (body.FSM.GetCurrentState()!=StateType.turnOver&&body.FSM.GetCurrentState()!=StateType.Dead)
            {
                yield return new WaitForSeconds(0.5f);
            }
            bodies.Remove(body);
        }
        TurnOver();
    }
    private bool IsOver
    {
        get
        {
            return P1Lives == 0 || P2Lives == 0;
        }
    }

    private void TurnOver()
    {
        foreach(var body in GetLives())
        {
            body.FSM.ChangeState(StateType.battleIdle);
        }
        BattleManager.Instance.StartCoroutine(TurnBegin());
    }

    private void BattleFinish()
    {
        GameManager.Instance.PlayBGM(0);
        foreach(var body in GetLives())
        {
            body.PlayerController.OnBattleEnd();
            body.FSM.ChangeState(StateType.idle);
        }
        BattleManager.Instance.BattleFinish(this);
        GameManager.Instance.FSM.ChangeState(StateType.FreeMode);
        Debug.Log("战斗结束!");
    }

    public void Die(LifeBody lifeBody)
    {
        lifeBody.PlayerController.OnBattleEnd();
        lifeBody.FSM.ChangeState(StateType.Dead);
    }

    public LifeBody GetRandomEnemy(LifeBody self)
    {
        if(part1.Contains(self))
        {
            List<LifeBody> temp = new List<LifeBody>();
            foreach(var body in part2)
            {
                if (!body.IsDead)
                    temp.Add(body);
            }
            return temp.Count>0?temp[UnityEngine.Random.Range(0, temp.Count)]:null;
        }
        else if(part2.Contains(self))
        {
            List<LifeBody> temp = new List<LifeBody>();
            foreach (var body in part1)
            {
                if (!body.IsDead)
                    temp.Add(body);
            }
            return temp.Count>0?temp[UnityEngine.Random.Range(0, temp.Count)]:null;
        }
        return null;
    }

    public void AddLifeBody(LifeBody add,LifeBody enemy)
    {
        if(part1.Contains(enemy))
        {
            part2.Add(add);
        }
        else if(part2.Contains(enemy))
        {
            part1.Add(add);
        }
        CameraManeger.Instance.StartCoroutine(CameraManeger.Instance.FocusTemporary(add.GameObject.transform.Find("LookPos"),1f));
        add.FSM.ChangeState(StateType.battleStart,this);
        add.FSM.ChangeState(StateType.battleIdle);
    }
}