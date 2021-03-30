using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTree
{
    SelectorNode rootNode;
    public LifeBody LifeBody { get; set; }
    public BTState state = BTState.Abort;
    public BehaviourTree(LifeBody body,SelectorNode root)
    {
        LifeBody = body;
        rootNode = root;
    }
    public BehaviourTree(LifeBody body,string InitScript)
    {
        LifeBody = body;
        //

    }
    public BTState Run()
    {
        return rootNode.Run();
    }
}

public enum BTState
{
    Success,Failure,Running,Abort
}
public abstract class BehaviourNodeBase
{
    public BehaviourTree tree;
    public abstract BTState Run();
}


public abstract class LeafNode : BehaviourNodeBase
{

}

public abstract class BranchNode : BehaviourNodeBase
{
    protected List<BehaviourNodeBase> children;
    public BranchNode(BehaviourTree tree = null)
    {
        this.tree = tree;
        children = new List<BehaviourNodeBase>();
    }
    public T AddNode<T>(T node)where T : BehaviourNodeBase
    {
        node.tree = tree;
        children.Add(node);
        return node;
    }
}



public class ConditonNode : LeafNode
{
    public Func<bool> condition;

    public ConditonNode(Func<bool> func)
    {
        condition = func;
    }

    public override BTState Run()
    {
        return condition() ? BTState.Success : BTState.Failure;
    }
}
public class ActionNode : LeafNode
{
    BTState state;
    ActionType actionType;
    Action action;
    Func<IEnumerator> corotoutines;
    public ActionNode(Action action,ActionType actionType)
    {
        this.action = action;
        this.actionType = actionType;
    }
    public ActionNode(Func<IEnumerator> corotoutines,ActionType actionType)
    {
        this.corotoutines = corotoutines;
        this.actionType = actionType;
    }
    public override BTState Run()
    {
        if(action!=null)
        {
            action();
            return BTState.Success;
        }
        else if(corotoutines!=null)
        {
            if (state == BTState.Running)
                return BTState.Running;
            state = BTState.Running;
            tree.LifeBody.AI.OnActionEnd += OnActionEnd;
            tree.LifeBody.AI.StartCoroutine(corotoutines());
            return BTState.Running;
        }
        return BTState.Failure;
    }
    public void OnActionEnd(object sender,EventArgs e)
    {
        var args = (ActionEventArgs)e;
        if(args.status==ActionStatus.Abort)
        {
            tree.LifeBody.AI.OnActionEnd -= OnActionEnd;
            state = BTState.Abort;
            tree.state = state;
            return;
        }    
        if (args.actionType != this.actionType)
            return;
        if (args.status==ActionStatus.Success)
            state = BTState.Success;
        else 
            state = BTState.Failure;
        tree.LifeBody.AI.OnActionEnd -= OnActionEnd;
        tree.state = state;
    }
}

/// <summary>
/// 顺序执行，直到为真
/// </summary>
public class SelectorNode: BranchNode
{
    public override BTState Run()
    {
        foreach(var child in children)
        {
            switch(child.Run())
            {
                case BTState.Success:
                    return BTState.Success;
                case BTState.Failure:
                    break;
                case BTState.Running:
                    return BTState.Running;
                case BTState.Abort:
                    return BTState.Abort;
            }               
        }
        return BTState.Failure;
    }
}

/// <summary>
/// 顺序执行，直到为假
/// </summary>
public class SequenceNode:BranchNode
{
    public override BTState Run()
    {
        foreach (var child in children)
        {
            switch (child.Run())
            {
                case BTState.Success:
                    break;
                case BTState.Failure:
                    return BTState.Failure;
                case BTState.Running:
                    return BTState.Running;
                case BTState.Abort:
                    return BTState.Abort;
            }
        }
        return BTState.Success;
    }
}


