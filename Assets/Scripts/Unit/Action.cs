using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ActionFactory
{
    private static ActionFactory instance;
    public static ActionFactory Instance
    {
        get
        {
            if(instance==null)
                instance = new ActionFactory();
            return instance;
        }
    }

    public IRunable Create(ActionType actionType,LifeBody lifeBody)
    {
        switch(actionType)
        {
            case ActionType.Move:
                return new MoveAction(lifeBody);
            case ActionType.NormalAttack:
                return new NormalAttackAction(lifeBody);
            default:
            return null;
        }
    }



}



public class ActionEventArgs : EventArgs
{
    public ActionType actionType;
    public ActionStatus status;
    public object[] args;
    public ActionEventArgs(ActionType action,ActionStatus status,params object[] args)
    {
        actionType = action;
        this.status = status;
        this.args = args;
    }
}
public enum ActionStatus
{
    Success,Failed,Running,Abort
}

public enum ActionFailedSituation
{
    NoEnergy
}

public class MoveAction : IRunable
{
    LifeBody self;
    StateType State { get => self.FSM.GetCurrentState(); }
    Tuple<bool,object> False = new Tuple<bool, object>(false,null);
    bool InBattle { get => State == StateType.action; }

    public MoveAction(LifeBody lifeBody)
    {
        self = lifeBody;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="args">position</param>
    /// <returns></returns>
    public Tuple<bool, object> CanRun(params object[] args)
    {
        if (InBattle && self.IsActing)
            return False;
        if (self.TryGetFirstHitInfo((Vector2)args[0], out RaycastHit hitInfo, "Ground", false))
        {
            return Tuple.Create<bool, object>(true, hitInfo.point);
        }
        return False;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="args">[0]:Vector3 destination,[1]:StoppingDistance,[2]LifeBody Target</param>
    public void Run(params object[] args)
    {
        self.ActionStart(new ActionEventArgs(ActionType.Move,ActionStatus.Running));
        if (self.MaxMoveRange() < 0.5)
        {
            PlayerController_OnActionEnd(null, new ActionEventArgs(ActionType.Move,ActionStatus.Failed,ActionFailedSituation.NoEnergy));
            return;
        }
        self.PlayerController.OnActionEnd += PlayerController_OnActionEnd;
        self.PlayerController.StopAllCoroutines();
        self.PlayerController.StartCoroutine(self.PlayerController.Move(args));
    }
    public void PlayerController_OnActionEnd(object sender, EventArgs e)
    {
        self.LineRenderer.enabled = false;
        self.PlayerController.OnActionEnd -= PlayerController_OnActionEnd;
        self.ActionEnd(e);
    }
}
public class NormalAttackAction : IRunable
{
    LifeBody body;
    StateType State { get => body.FSM.GetCurrentState(); }
    bool InBattle { get => State == StateType.action; }
    Tuple<bool, object> False = new Tuple<bool, object>(false, null);
    public NormalAttackAction(LifeBody lifeBody)
    {
        body = lifeBody;
    }
    public Tuple<bool, object> CanRun(params object[] args)
    {
        if (InBattle && body.IsActing)
            return False;
        if (body.TryGetFirstHitInfo((Vector2)args[0], out RaycastHit hitInfo, "LifeBody", false))
        {
            if (!hitInfo.collider.gameObject.GetComponent<PlayerController>().LifeBody.IsDead)
                return Tuple.Create<bool, object>(true, hitInfo.collider.gameObject.GetComponent<PlayerController>().LifeBody);
        }
        return False;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args">[0]:LifeBody tatget,</param>
    public void Run(params object[] args)
    {
        body.ActionStart(new ActionEventArgs(ActionType.NormalAttack,ActionStatus.Running));
        body.LineRenderer.enabled = false;
        body.PlayerController.OnActionEnd += PlayerController_OnActionEnd;
        LifeBody target = (LifeBody)args[0];
        if (Vector3.Distance(body.GameObject.transform.position, target.GameObject.transform.position) < body.GetAttackRange())
        {
            if (EnoughEnergy())
            {
                body.ChangeEnergy(-GetCostEnergy());
                body.StartAttack(target);
            }
            else
            {
                PlayerController_OnActionEnd(null, new ActionEventArgs(ActionType.NormalAttack,ActionStatus.Failed,ActionFailedSituation.NoEnergy));
            }
        }
        else
        {
            body.actions[ActionType.Move].Run(target.GameObject.transform.position, body.GetAttackRange(), target);
        }
    }
    public void PlayerController_OnActionEnd(object sender, EventArgs e)
    {
        var args = (ActionEventArgs)e;
        if (args.actionType == ActionType.Move)
        {
            if (args.status==ActionStatus.Success)
            {
                if (EnoughEnergy())
                    {
                        body.ChangeEnergy(-GetCostEnergy());
                        body.StartAttack((LifeBody)args.args[0]);
                        return;
                    }
                    else
                    {
                        body.PlayerController.OnActionEnd -= PlayerController_OnActionEnd;
                        body.ActionEnd(new ActionEventArgs(ActionType.NormalAttack,ActionStatus.Failed,ActionFailedSituation.NoEnergy));
                        return;
                    }
            }
            else
            {
                body.PlayerController.OnActionEnd -= PlayerController_OnActionEnd;
                body.ActionEnd(new ActionEventArgs(ActionType.NormalAttack,ActionStatus.Failed,args.args[0]));
            }

        }
        body.PlayerController.OnActionEnd -= PlayerController_OnActionEnd;
        body.ActionEnd(e);
    }

    public bool EnoughEnergy()
    {
        return body.CurrentEnergy >= GetCostEnergy();
    }
    public static float GetCostEnergy()
    {
        return 50f;
    }
}

public interface IRunable
{
    public Tuple<bool, object> CanRun(params object[] args);
    public void Run(params object[] args);
}