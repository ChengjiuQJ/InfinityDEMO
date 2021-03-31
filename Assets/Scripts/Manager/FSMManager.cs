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


    //IdleState
    IDLE_IDLE,IDLE_CHOOSE,

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
    protected LifeBody lifebody;
    public LifeBodyState(LifeBody body)
    {
        lifebody = body;
    }
    public override void OnEnter(params object[] args)
    {
        if (Logger.Instance.showFSMLog)
            Debug.Log($"{lifebody.Name}进入{stateType}");
    }

    public override void OnExit(params object[] args)
    {
        if (Logger.Instance.showFSMLog)
            Debug.Log($"{lifebody.Name}退出{stateType}");
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
    FSMManager childFSM;
    public IdleState(LifeBody lifeBody):base(lifeBody)
    {
        stateType = StateType.idle;
    }
    public override void OnEnter(params object[] args)
    {
        if(lifebody.CanBeControlled)
        {
            childFSM = new FSMManager();
            childFSM.RegistState(StateType.IDLE_IDLE,new Idle_Idle_State(lifebody));
            childFSM.RegistState(StateType.IDLE_CHOOSE,new Idle_Choose_State(lifebody));
            childFSM.StartFSM(StateType.IDLE_IDLE);
            //InputManager.Instance.OnMouseDown += LifeBody.OnMouseDown;
        }           
        lifebody.CurrentBattle = null;
    }


    public override void OnExit(params object[] args)
    {
        if(lifebody.IsPlayer)
        {
            childFSM?.StopFSM();
        }           
        else
            lifebody.AI.Stop();
        lifebody.PlayerController.StopAllActions();
    }

    public override void Update(params object[] args)
    {
        //更新鼠标指针
        if(lifebody.IsPlayer)
            UpDateCursor();
        else
            lifebody.AI.Run();
        //更新敌人
        UpDateEnemy();
        //更新属性
        UpdateProperty();
        //
        childFSM?.UpdateState();
        //UpdateNavMeshDate();
    }

    private void UpdateNavMeshDate()
    {
        var nav = lifebody.PlayerController.Nav;
    }

    private void UpdateProperty()
    {
        lifebody.ChangeEnergy(Time.deltaTime * DataManager.Instance.GetHighValue(lifebody,HighValue.精力恢复速率)/GameManager.secondsPerTurn);
    }

    private void UpDateEnemy()
    {        
        List<LifeBody> enemy = new List<LifeBody>();
        bool mainBattle = lifebody.IsPlayer;
        if (lifebody.passerbys.Count == 0)
            return;
        LinkedListNode<LifeBody> node = lifebody.passerbys.First;
        while(node!=null)
        {
            var unit = node.Value;
            if (unit.IsDead)
            {
                lifebody.passerbys.Remove(node);
                node = node.Next;
                continue;
            }

            //需要改进:判断敌对的条件 
            if ((lifebody.IsPlayer != unit.IsPlayer) && Vector3.Distance(unit.Position, lifebody.Position) < lifebody.BattleRaius)
            {
                if (unit.IsPlayer)
                    mainBattle = true;
                if (unit.CurrentBattle == null)
                {
                    enemy.Add(unit);
                }
                else
                {
                    unit.CurrentBattle.AddLifeBody(lifebody, unit);
                    return;
                }
            }
            node = node.Next;
        }
        if (enemy.Count > 0)
            BattleManager.Instance.CreatBattle(new List<LifeBody>() { lifebody }, enemy,mainBattle);
    }

    private void UpDateCursor()
    {
        if (lifebody.TryGetFirstHitInfo(Input.mousePosition, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                GameManager.Instance.SetCursor(CursorStyle.Move_Default);
            }
            else if (lifebody.GetAttackRange() >= Vector3.Distance(lifebody.GameObject.transform.position, hit.point))
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

public class Idle_Idle_State : IdleState
{
    HashSet<ActionType> acceptActions;
    public Idle_Idle_State(LifeBody body):base(body)
    {

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

    void OnMouseDown(object sender,EventArgs e)
    {
        var args = (MouseEventArgs)e;
        var actions = InputManager.Instance.GetActions(args.Button);
        Dictionary<ActionType, Tuple<IRunable,object>> temp = new Dictionary<ActionType, Tuple<IRunable,object>>();
        foreach (var actionType in actions)
        {
            if(!acceptActions.Contains(actionType))
                continue;
            IRunable action = ActionFactory.Instance.Create(actionType,lifebody);
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
        temp[keys[0]].Item1.Run(temp[keys[0]].Item2);      
    }

    public override void OnExit(params object[] args)
    {
        InputManager.Instance.OnMouseDown-=OnMouseDown;
    }

}

public class Idle_Choose_State: Idle_Idle_State
{
    Skill currentSkill;
    HashSet<ActionType> acceptActions;
    public Idle_Choose_State(LifeBody body):base(body)
    {

    }
    public override void OnEnter(params object[] args)
    {
        int id =(int)args[0];
        currentSkill = DataManager.Instance.SkillData[id];
        InputManager.Instance.OnMouseDown+=OnMouseDown;
        acceptActions = new HashSet<ActionType>();
        acceptActions.Add(ActionType.UseSkill);
    }

    public override void Update(params object[] args)
    {
        RenderAttackRange();
    }

    public override void OnExit(params object[] args)
    {
        currentSkill = null;
        
    }

    public void RenderAttackRange()
    {

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
            IRunable action = ActionFactory.Instance.Create(actionType,lifebody);
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
        temp[keys[0]].Item1.Run(temp[keys[0]].Item2);      
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
        lifebody.PlayerController.OnBattleStart();
        lifebody.CurrentBattle =(Battle)args[0];       
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
        if (lifebody.IsDead)
            lifebody.CurrentBattle.Die(lifebody);
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
        if (lifebody.IsPlayer)
            UIManager.Instance.TurnBegin();
        lifebody.RecoverEnergyPerRound();
        lifebody.UpdateBuffsPerTurn();
        //移动摄像机到该物体
        CameraManeger.Instance.Follow = lifebody.GameObject.transform.Find("LookPos");
        CameraManeger.Instance.LookAt = lifebody.GameObject.transform.Find("LookPos");

        Outline outline = lifebody.PlayerController.Outline;
        outline.OutlineColor = Color.white;
        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.enabled = true;

        lifebody.FSM.ChangeState(StateType.action);
    }


    public override void Update(params object[] args)
    {
        base.Update(args);
        if (lifebody.IsDead)
            lifebody.CurrentBattle.Die(lifebody);
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
        PlayerController controller = lifebody.PlayerController;
        unityAction = new UnityEngine.Events.UnityAction(FinishTurn);
        if (lifebody.CanBeControlled)
        {
            FSM = new FSMManager();
            FSM.RegistState(StateType.ACTION_IDLE,new Action_Idle_State(lifebody));
            FSM.RegistState(StateType.ACTION_CHOOSE,new Action_Choose_State(lifebody));
            FSM.RegistState(StateType.ACTION_PLAYTING,new Action_Playing_State(lifebody));
            FSM.StartFSM(StateType.ACTION_IDLE);
            //允许输入      
            InputManager.Instance.FinishTurnButton.onClick.AddListener(unityAction);
            InputManager.Instance.FinishTurnButton.gameObject.SetActive(true);
        }
        else
        {
            lifebody.AI.Run();
        }
    }

    public override void OnExit(params object[] args)
    {
        base.OnExit(args);     
        lifebody.PlayerController.StopAllCoroutines();
        lifebody.PathRendering = false;
        FSM?.StopFSM();
        FSM = null;
        InputManager.Instance.FinishTurnButton.onClick.RemoveListener(unityAction);
        InputManager.Instance.FinishTurnButton.gameObject.SetActive(false);
        if (!lifebody.CanBeControlled)
            lifebody.AI.Stop();
    }

    public override void Update(params object[] args)
    {
        base.Update(args);
        if (lifebody.IsDead)
        {
            lifebody.CurrentBattle.Die(lifebody);
            return;
        }
        if (!lifebody.IsActing&&lifebody.CanBeControlled)
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
        lifebody.FSM.ChangeState(StateType.turnOver);
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
        if(!lifebody.IsActing&&lifebody.CanBeControlled)
            lifebody.PlayerController.StartCoroutine(lifebody.RenderMoveAndAttackLine(Input.mousePosition));
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
            IRunable action = ActionFactory.Instance.Create(actionType,lifebody);
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
        lifebody.LineRenderer.enabled = false;
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
        lifebody.OnActionEnd+=OnActionEnd;
    }

    private void OnActionEnd(object sender, EventArgs e)
    {        
        fSM.ChangeState(StateType.ACTION_IDLE);
    }
    public override void OnExit(params object[] args)
    {
        lifebody.OnActionEnd-=OnActionEnd;
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
        lifebody.PlayerController.Outline.OutlineMode = Outline.Mode.OutlineHidden;
    }
    public override void Update(params object[] args)
    {
        base.Update(args);
        if (lifebody.IsDead)
            lifebody.CurrentBattle.Die(lifebody);
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
        if(lifebody.IsPlayer)
        {
            GameManager.Instance.PlaySE(0);
            GameManager.Instance.MuteBGM();
        }        
        lifebody.PlayerController.Outline.OutlineMode = Outline.Mode.OutlineHidden;
        lifebody.CurrentBattle = null;
        lifebody.PlayerController.StopAllActions();
        lifebody.PlayerController.StartCoroutine(lifebody.PlayerController.Die());
    }


    public override void OnExit(params object[] args)
    {

    }

    public override void Update(params object[] args)
    {

    }
}
