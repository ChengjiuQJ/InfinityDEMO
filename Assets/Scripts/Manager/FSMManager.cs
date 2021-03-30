using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class FSMManager
{
    private Dictionary<StateType, StateBase> allStates;
    public StateBase PreState { get; private set; }
    private StateBase currentState;

    public event EventHandler OnStateChanged;
    public FSMManager()
    {
        allStates = new Dictionary<StateType, StateBase>();
        currentState = null;
        PreState = null;
    }
    public void RegistState(StateType type,StateBase state)
    {
        if (!allStates.ContainsKey(type))
        {
            state.fSM = this;
            allStates.Add(type, state);
        }
            
    }
    public void UnRegistState(StateType type,StateBase state)
    {
        if (allStates.ContainsKey(type))
            allStates.Remove(type);
    }
    public void ChangeState(StateType type,params object[] args)
    {
        if (!allStates.ContainsKey(type))
            Debug.LogError("can not find the state needed to switch (register the state first)");
        if (currentState!=null)
        {
            if (currentState.stateType == type)
                return;
            PreState = currentState;
            currentState.OnExit(args);
        }
        OnStateChanged?.Invoke(null, new StateEventArgs(currentState == null ? StateType.None : currentState.stateType, type));
        currentState = allStates[type];
        currentState.OnEnter(args);          
    }

    public void StopFSM()
    {
        currentState?.OnExit();
        currentState=null;
        allStates.Clear();
    }
    public StateType GetCurrentState()
    {
        if (currentState != null)
            return currentState.stateType;
        return StateType.None;
    }
    public void UpdateState()
    {
        if (currentState != null)
            currentState.Update();
    }

    public void StartFSM(StateType firstState,params object[] args)
    {
        ChangeState(firstState,args);
    }
}

public class StateEventArgs:EventArgs
{
    public StateType oldStateType;
    public StateType newStateType;
    public StateEventArgs(StateType oldState,StateType newState)
    {
        oldStateType = oldState;
        newStateType = newState;
    }
}

public enum StateType
{
    //gameFSM State
    None,FreeMode,BattleMode,UIMode,

    //LifeBodyFSM State
    idle,battleStart,battleIdle,turnBegin, action, turnOver,Dead,

    //ActionState
    ACTION_IDLE,ACTION_CHOOSE,ACTION_PLAYTING
    
}
public abstract class StateBase
{
    public FSMManager fSM;
    public StateType stateType;
    public abstract void OnEnter(params object[] args);
    public abstract void Update(params object[] args);
    public abstract void OnExit(params object[] args);
}



public abstract  class LifeBodyState : StateBase
{
    protected LifeBody LifeBody { get; set; }
    public LifeBodyState(LifeBody body)
    {
        LifeBody = body;
    }
    public override void OnEnter(params object[] args)
    {
        if (Logger.Instance.showFSMLog)
            Debug.Log($"{LifeBody.Name}进入{stateType}");
    }

    public override void OnExit(params object[] args)
    {
        if (Logger.Instance.showFSMLog)
            Debug.Log($"{LifeBody.Name}退出{stateType}");
    }
}

public class BattleState : LifeBodyState
{
    public BattleState(LifeBody body):base(body)
    {

    }
    

    public override void Update(params object[] args)
    {

    }

}

public class IdleState : LifeBodyState
{
    public IdleState(LifeBody lifeBody):base(lifeBody)
    {
        stateType = StateType.idle;
    }
    public override void OnEnter(params object[] args)
    {
        if(LifeBody.CanBeControlled)
            InputManager.Instance.OnMouseDown += LifeBody.OnMouseDown;
        LifeBody.CurrentBattle = null;
    }


    public override void OnExit(params object[] args)
    {
        if(LifeBody.IsPlayer)
            InputManager.Instance.OnMouseDown -= LifeBody.OnMouseDown;
        else
            LifeBody.AI.Stop();
        LifeBody.PlayerController.StopAllActions();
    }

    public override void Update(params object[] args)
    {
        //更新鼠标指针
        if(LifeBody.IsPlayer)
            UpDateCursor();
        else
            LifeBody.AI.Run();
        //更新敌人
        UpDateEnemy();
        //更新属性
        UpdateProperty();
        //
        //UpdateNavMeshDate();
    }

    private void UpdateNavMeshDate()
    {
        var nav = LifeBody.PlayerController.Nav;
    }

    private void UpdateProperty()
    {
        LifeBody.ChangeEnergy(Time.deltaTime * DataManager.Instance.GetHighValue(LifeBody,HighValue.精力恢复速率)/GameManager.secondsPerTurn);
    }

    private void UpDateEnemy()
    {        
        List<LifeBody> enemy = new List<LifeBody>();
        bool mainBattle = LifeBody.IsPlayer;
        if (LifeBody.passerbys.Count == 0)
            return;
        LinkedListNode<LifeBody> node = LifeBody.passerbys.First;
        while(node!=null)
        {
            var unit = node.Value;
            if (unit.IsDead)
            {
                LifeBody.passerbys.Remove(node);
                node = node.Next;
                continue;
            }

            //需要改进:判断敌对的条件 
            if ((LifeBody.IsPlayer != unit.IsPlayer) && Vector3.Distance(unit.Position, LifeBody.Position) < LifeBody.BattleRaius)
            {
                if (unit.IsPlayer)
                    mainBattle = true;
                if (unit.CurrentBattle == null)
                {
                    enemy.Add(unit);
                }
                else
                {
                    unit.CurrentBattle.AddLifeBody(LifeBody, unit);
                    return;
                }
            }
            node = node.Next;
        }
        if (enemy.Count > 0)
            BattleManager.Instance.CreatBattle(new List<LifeBody>() { LifeBody }, enemy,mainBattle);
    }

    private void UpDateCursor()
    {
        if (LifeBody.TryGetFirstHitInfo(Input.mousePosition, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                GameManager.Instance.SetCursor(CursorStyle.Move_Default);
            }
            else if (LifeBody.GetAttackRange() >= Vector3.Distance(LifeBody.GameObject.transform.position, hit.point))
            {
                GameManager.Instance.SetCursor(CursorStyle.Attack_Eable);
            }
            else
            {
                GameManager.Instance.SetCursor(CursorStyle.Attack_Disable);
            }
        }
        else
            GameManager.Instance.SetCursor(CursorStyle.Forbid);
    }
}
/// <summary>
/// 战斗开始阶段,用于战前buff
/// </summary>
public class BattleStartState : BattleState
{
    public BattleStartState(LifeBody body):base(body)
    {
        stateType = StateType.battleStart;
    }
    public override void OnEnter(params object[] args)
    {
        base.OnEnter(args);
        LifeBody.PlayerController.OnBattleStart();
        LifeBody.CurrentBattle =(Battle)args[0];       
    }

}

/// <summary>
/// 战斗待机状态,用于还未轮到行动的对象
/// </summary>
public class BattleIdleState:BattleState
{
    public BattleIdleState(LifeBody body):base(body)
    {
        stateType = StateType.battleIdle;
    }
    public override void Update(params object[] args)
    {
        base.Update(args);
        if (LifeBody.IsDead)
            LifeBody.CurrentBattle.Die(LifeBody);
    }

}
public class TurnBeginState : BattleState
{
    public TurnBeginState(LifeBody body):base(body)
    {
        stateType = StateType.turnBegin;
    }
    public override void OnEnter(params object[] args)
    {
        base.OnEnter(args);
        //动画音效提示回合
        //codexxx
        if (LifeBody.IsPlayer)
            UIManager.Instance.TurnBegin();
        LifeBody.RecoverEnergyPerRound();
        LifeBody.UpdateBuffsPerTurn();
        //移动摄像机到该物体
        CameraManeger.Instance.Follow = LifeBody.GameObject.transform.Find("LookPos");
        CameraManeger.Instance.LookAt = LifeBody.GameObject.transform.Find("LookPos");

        Outline outline = LifeBody.PlayerController.Outline;
        outline.OutlineColor = Color.white;
        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.enabled = true;

        LifeBody.FSM.ChangeState(StateType.action);
    }


    public override void Update(params object[] args)
    {
        base.Update(args);
        if (LifeBody.IsDead)
            LifeBody.CurrentBattle.Die(LifeBody);
    }
}

public class ActionState:BattleState
{
    FSMManager FSM;
    UnityEngine.Events.UnityAction unityAction;
    public ActionState(LifeBody body):base(body)
    {
        stateType = StateType.action;
    }

    public override void OnEnter(params object[] args)
    {
        base.OnEnter(args);
        PlayerController controller = LifeBody.PlayerController;
        unityAction = new UnityEngine.Events.UnityAction(FinishTurn);
        if (LifeBody.CanBeControlled)
        {
            FSM = new FSMManager();
            FSM.RegistState(StateType.ACTION_IDLE,new Action_Idle_State(LifeBody));
            FSM.RegistState(StateType.ACTION_CHOOSE,new Action_Choose_State(LifeBody));
            FSM.RegistState(StateType.ACTION_PLAYTING,new Action_Playing_State(LifeBody));
            FSM.StartFSM(StateType.ACTION_IDLE);
            //允许输入      
            InputManager.Instance.FinishTurnButton.onClick.AddListener(unityAction);
            InputManager.Instance.FinishTurnButton.gameObject.SetActive(true);
        }
        else
        {
            LifeBody.AI.Run();
        }
    }

    public override void OnExit(params object[] args)
    {
        base.OnExit(args);     
        LifeBody.PlayerController.StopAllCoroutines();
        LifeBody.PathRendering = false;
        FSM?.StopFSM();
        FSM = null;
        InputManager.Instance.FinishTurnButton.onClick.RemoveListener(unityAction);
        InputManager.Instance.FinishTurnButton.gameObject.SetActive(false);
        if (!LifeBody.CanBeControlled)
            LifeBody.AI.Stop();
    }

    public override void Update(params object[] args)
    {
        base.Update(args);
        if (LifeBody.IsDead)
        {
            LifeBody.CurrentBattle.Die(LifeBody);
            return;
        }
        if (!LifeBody.IsActing&&LifeBody.CanBeControlled)
        {           
            InputManager.Instance.FinishTurnButton.interactable = true;
        }           
        else
        {
            InputManager.Instance.FinishTurnButton.interactable = false;
        }
        FSM?.UpdateState();
    }
    void FinishTurn()
    {
        LifeBody.FSM.ChangeState(StateType.turnOver);
    }
}


public class Action_Idle_State : BattleState
{
    HashSet<ActionType> acceptActions;
    public Action_Idle_State(LifeBody body):base(body)
    {
        stateType = StateType.ACTION_IDLE;
    }
    public override void OnEnter(params object[] args)
    {
        InitAcceptActions();
        InputManager.Instance.OnMouseDown+=OnMouseDown;
    }

    private void InitAcceptActions()
    {
        acceptActions = new HashSet<ActionType>();
        acceptActions.Add(ActionType.Move);
        acceptActions.Add(ActionType.NormalAttack);
    }

    public override void Update(params object[] args)
    {
        if(!LifeBody.IsActing&&LifeBody.CanBeControlled)
            LifeBody.PlayerController.StartCoroutine(LifeBody.RenderMoveAndAttackLine(Input.mousePosition));
    }

    void OnMouseDown(object sender,EventArgs e)
    {
        var args = (MouseEventArgs)e;
        var actions = InputManager.Instance.GetActions(args.Button);
        Dictionary<ActionType, Tuple<IRunable,object>> temp = new Dictionary<ActionType, Tuple<IRunable,object>>();
        foreach (var actionType in actions)
        {
            if(!acceptActions.Contains(actionType))
                continue;
            IRunable action = ActionFactory.Instance.Create(actionType,LifeBody);
            if(action==null)
                continue;
            var tuple = action.CanRun(args.Position);
            if (tuple.Item1)
            {
                temp.Add(actionType,new Tuple<IRunable, object>(action,tuple.Item2));
            }
        }
        if (temp.Count == 0)
            return;
        List<ActionType> keys = new List<ActionType>(temp.Keys);    
        if (temp.Count>1)
        {
            keys.Sort((x, y) => (int)x > (int)y ? -1 : 1);
            
        }
        fSM.ChangeState(StateType.ACTION_PLAYTING);
        temp[keys[0]].Item1.Run(temp[keys[0]].Item2);      
    }


    public override void OnExit(params object[] args)
    {
        InputManager.Instance.OnMouseDown-=OnMouseDown;
        LifeBody.LineRenderer.enabled = false;
    }

}

public class Action_Choose_State:BattleState
{
    public Action_Choose_State(LifeBody body):base(body)
    {

    }
}

public class Action_Playing_State:BattleState
{
    public Action_Playing_State(LifeBody body):base(body)
    {

    }
    public override void OnEnter(params object[] args)
    {
        LifeBody.OnActionEnd+=OnActionEnd;
    }

    private void OnActionEnd(object sender, EventArgs e)
    {        
        fSM.ChangeState(StateType.ACTION_IDLE);
    }
    public override void OnExit(params object[] args)
    {
        LifeBody.OnActionEnd-=OnActionEnd;
    }
}














public class TurnOverState : BattleState
{
    public TurnOverState(LifeBody body):base(body)
    {
        stateType = StateType.turnOver;
    }

    public override void OnEnter(params object[] args)
    {
        base.OnEnter(args);
        LifeBody.PlayerController.Outline.OutlineMode = Outline.Mode.OutlineHidden;
    }
    public override void Update(params object[] args)
    {
        base.Update(args);
        if (LifeBody.IsDead)
            LifeBody.CurrentBattle.Die(LifeBody);
    }
}
public class DeadState : LifeBodyState
{
    public DeadState(LifeBody body):base(body)
    {
        stateType = StateType.Dead;
    }
    public override void OnEnter(params object[] args)
    {
        if(LifeBody.IsPlayer)
        {
            GameManager.Instance.PlaySE(0);
            GameManager.Instance.MuteBGM();
        }        
        LifeBody.PlayerController.Outline.OutlineMode = Outline.Mode.OutlineHidden;
        LifeBody.CurrentBattle = null;
        LifeBody.PlayerController.StopAllActions();
        LifeBody.PlayerController.StartCoroutine(LifeBody.PlayerController.Die());
    }


    public override void OnExit(params object[] args)
    {

    }

    public override void Update(params object[] args)
    {

    }
}
