using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    PlayerController PlayerController { get; set; }
    LifeBody LifeBody { get { return PlayerController.LifeBody; } }
    public event EventHandler OnActionEnd;
    BehaviourTree BTree { get; set; }
    bool IsStarted;
    bool run = false;
    private void Awake()
    {
        IsStarted = false;
        PlayerController = GetComponent<PlayerController>();        
    }
    private void Start()
    {
       StartCoroutine (InitBTree());
    }

    private IEnumerator InitBTree()
    {
        while (PlayerController.LifeBody == null)
            yield return null;
        SelectorNode node = new SelectorNode();
        BTree = new BehaviourTree(PlayerController.LifeBody,node);
        node.tree = BTree;

        var idleNode = node.AddNode(new SequenceNode());
        idleNode.AddNode(new ConditonNode(()=>LifeBody.FSM.GetCurrentState()==StateType.idle));
        idleNode.AddNode(new ActionNode(MoveAround,ActionType.Move));

        var attackNode = node.AddNode(new SequenceNode());   
        attackNode.AddNode(new ConditonNode(()=>LifeBody.CurrentBattle!=null));
        attackNode.AddNode(new ConditonNode(() => LifeBody.CurrentEnergy >= NormalAttackAction.GetCostEnergy())) ;
        var attackAction = attackNode.AddNode(new ActionNode(Attack,ActionType.NormalAttack));        
        node.AddNode(new ActionNode(TurnOver,ActionType.TurnOver));
        IsStarted = true;
    }

    private void Update()
    {
        if(run&&BTree.state!=BTState.Running)
            BTree.state= BTree.Run();
    }
    public void Run()
    {
        if(IsStarted)
            run = true;
    }

    public IEnumerator Attack()
    {       
        yield return null;
        LifeBody target = LifeBody.CurrentBattle.GetRandomEnemy(LifeBody);
        if (target != null)
        {
            LifeBody.OnActionEnd += LifeBody_OnActionEnd;
            //((NormalAttackAction)PlayerController.LifeBody.actions[ActionType.NormalAttack]).Run(target);
            ActionFactory.Instance.Create(ActionType.NormalAttack,LifeBody).Run(target);
        }
        else
        {
            LifeBody_OnActionEnd(null, new ActionEventArgs(ActionType.NormalAttack,ActionStatus.Failed));
        }
    }


    public IEnumerator MoveAround()
    {
        yield return null;
        LifeBody.OnActionEnd+=LifeBody_OnActionEnd;
        Vector3 pos = LifeBody.Position;
        float offsetX = UnityEngine.Random.Range(-2f,2f);
        float offsetZ = UnityEngine.Random.Range(-2f,2f);
        var move = ActionFactory.Instance.Create(ActionType.Move,LifeBody);
        move.Run(new Vector3(pos.x+offsetX,pos.y,pos.z+offsetZ));        
    }


    private void LifeBody_OnActionEnd(object sender, EventArgs e)
    {
        OnActionEnd?.Invoke(sender, e);
    }

    internal void Stop()
    {
        run = false;
    }

    public void TurnOver()
    {
        LifeBody.FSM.ChangeState(StateType.turnOver);
    }  
}
