using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LifeBody : Unit,IDataGetable
{
    private static Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> formula;
    public static Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> Formula
    {
        get
        {
            if (formula != null)
                return formula;
            DataManager.Instance.LoadData("LifeBodyFormula", out var formulas);
            formula = formulas[0];
            return formula;
        }
    }
    public Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> datas;
    public Dictionary<HighValue, Dictionary<LowValue, IDataGetable>> Datas
    {
        get
        {
            if (datas != null)
                return datas;
            datas = new Dictionary<HighValue, Dictionary<LowValue, IDataGetable>>();
            return datas;
        }
    }
    public LinkedList<LifeBody> passerbys;
    public string Name { get { return GameObject.name; } }
    public bool IsPlayer { get { return Name == "Player"; } }
    public Vector3 Position { get { return GameObject.transform.position; } }
    public Battle CurrentBattle { get; set; }
    public PlayerController PlayerController { get; }
    public EnemyAI AI { get; private set; }
    public LineRenderer LineRenderer { get;private set; }
    private HPBar HPBar { get; set; }
    private EnergyBar EnergyBar { get; set; } 
    public bool PathRendering { get; set; }
    public List<Equipment> CurrentEquipments { get; private set;}
    public List<Buff> CurrentBuffs { get; private set; }


    public Outline PointTo { get; set; }
    public bool IsActing { get; private set; }
    public event EventHandler OnActionBegin;
    public event EventHandler OnActionEnd;
    public int Level { get; private set; }
    //种族
    public int race;

    //侦测敌人
    public float WarningRaius { get { return 10f; } }
    private SphereCollider collider;

    public float BattleRaius { get { return 5f; } }

    //战斗属性
    public override float MaxHP
    {
        get
        {
            return DataManager.Instance.GetHighValue(this, HighValue.生命);
        }
    }
    public float Speed
    {
        get
        {
            return DataManager.Instance.GetHighValue(this, HighValue.速度);
        }
    }
    public float CurrentEnergy { get; private set;}
    public float MaxEnergy
    {
        get
        {
            return DataManager.Instance.GetHighValue(this, HighValue.精力);
        }
    }
    public const float recoverEnergyPerTurn = 50f;
    public const float recoverEnergyPerSecond = 20f;

    public bool CanBeControlled { get { return IsPlayer; } }
    
    //逻辑控制
    private FSMManager fsm;
    public FSMManager FSM
    {
        get
        {
            if (fsm != null)
                return fsm;
            fsm = new FSMManager();
            fsm.RegistState(StateType.idle, new IdleState(this));
            fsm.RegistState(StateType.battleStart, new BattleStartState(this));
            fsm.RegistState(StateType.battleIdle, new BattleIdleState(this));
            fsm.RegistState(StateType.turnBegin, new TurnBeginState(this));
            fsm.RegistState(StateType.action, new ActionState(this));
            fsm.RegistState(StateType.turnOver, new TurnOverState(this));
            fsm.RegistState(StateType.Dead, new DeadState(this));          
            return fsm;
        }
    }

    public Equipment CurrentWeapon { get; set; }

    public Dictionary<ActionType, IRunable> actions;
    public LifeBody(GameObject gameObject,PlayerController controller, int lvl):base(gameObject)
    {
        if (IsPlayer)
        {
            controller.Outline.enabled = true;
            HPBar = HPBar.Instance;
            EnergyBar = EnergyBar.Instance;
            race = 0;
            GameManager.Instance.Player = controller;
        }
        else
            race = 1;
        CurrentEquipments = new List<Equipment>();
        CurrentBuffs = new List<Buff>();
        CurrentEnergy = MaxEnergy;
        Level = lvl;
        passerbys = new LinkedList<LifeBody>();
        PlayerController = controller;
        AI = gameObject.GetComponent<EnemyAI>();
        Init(gameObject);
        actions = new Dictionary<ActionType, IRunable>
        {
            { ActionType.Move, new MoveAction(this) },
            { ActionType.NormalAttack, new NormalAttackAction(this) }
        };
        
        CurrentHP = MaxHP;
    }

    private void Init(GameObject gameObject)
    {
        GameObject Empty = new GameObject("Detetor");
        Empty.transform.parent = gameObject.transform;
        Empty.layer = 2;
        Empty.transform.localPosition = Vector3.zero;
        Empty.AddComponent<Detector>().controller = gameObject.GetComponent<PlayerController>();
        LineRenderer = Empty.AddComponent<LineRenderer>();
        LineRenderer.startWidth = 0.2f;
        LineRenderer.endWidth = 0.2f;
        LineRenderer.enabled = false;
        LineRenderer.material = PlayerController.material;
        LineRenderer.textureMode = LineTextureMode.RepeatPerSegment;
        var rigibody = Empty.AddComponent<Rigidbody>();
        rigibody.isKinematic = false;
        rigibody.useGravity = false;
        collider = Empty.AddComponent<SphereCollider>();
        collider.center = Vector3.zero;
        collider.isTrigger = true;
        collider.radius = WarningRaius;
    }

    public override void ChangeHP(Unit sender, float value)
    {
        base.ChangeHP(sender, value);
        HPBar?.UpdateImage();
    }

    public void Die()
    {
        
    }

    public void RecoverEnergyPerRound()
    {
        ChangeEnergy(recoverEnergyPerTurn);
    }


    public bool TryGetFirstHitInfo(Vector2 mousePosition, out RaycastHit raycastHit, string tag = null, bool selectSelf = false)
    {
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if(Physics.Raycast(ray,out raycastHit,GameManager.rayMaxDistance))
        {
            if(raycastHit.collider!=null)
            {
                if(tag==null)
                {
                    if (raycastHit.collider.gameObject.CompareTag("Ground") || raycastHit.collider.gameObject.CompareTag("LifeBody"))
                    {
                        if (selectSelf||raycastHit.collider.gameObject != GameObject)
                            return true;
                    }
                }
                else
                {
                    if(raycastHit.collider.gameObject.CompareTag(tag)) 
                    {
                        if (selectSelf|| raycastHit.collider.gameObject != GameObject)
                            return true;
                    }
                }
            }
        }
        return false;
    }

    public IEnumerator RenderMoveAndAttackLine(Vector2 mousePosition)
    {
        if (PathRendering)
            yield break;
        PathRendering = true;       
        if(TryGetFirstHitInfo(mousePosition,out RaycastHit target))
        {
            var agent = PlayerController.agent;
            agent.isStopped = true;
            agent.SetDestination(target.point);
            while (agent.pathPending)
            {
                yield return null;
            }
            var corners = agent.path.corners;
            var  distance = agent.GetPathRemainingDistance();
            agent.ResetPath();
            agent.isStopped = false;
            float maxMoveRange = MaxMoveRange();
            Gradient gradient = new Gradient();
            gradient.colorKeys = new GradientColorKey[] { new GradientColorKey(Color.green, maxMoveRange / distance), new GradientColorKey(Color.red, 1f) };
            gradient.alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
            gradient.mode = GradientMode.Fixed;
            LineRenderer.colorGradient = gradient;
            corners = corners.Lerp(Mathf.RoundToInt(distance*2));
            LineRenderer.positionCount = corners.Length;
            LineRenderer.SetPositions(corners);
            LineRenderer.enabled = true;
            PathRendering = false;
            if (target.collider.CompareTag("Ground"))
            {
                if (maxMoveRange >= distance)
                    GameManager.Instance.SetCursor(CursorStyle.Move_Eable);
                else
                    GameManager.Instance.SetCursor(CursorStyle.Move_Disable);
                if (PointTo != null)
                {
                    PointTo.OutlineColor = Color.white;
                    PointTo.OutlineMode = Outline.Mode.OutlineHidden;
                    PointTo = null;
                }
            }
            else
            {
                if (CurrentEnergy >= GetMoveCostEnergy(Mathf.Max(distance-GetAttackRange(),0f)) + NormalAttackAction.GetCostEnergy())
                    GameManager.Instance.SetCursor(CursorStyle.Attack_Eable);
                else
                    GameManager.Instance.SetCursor(CursorStyle.Attack_Disable);
                PointTo = target.collider.GetComponent<Outline>();
                PointTo.OutlineColor = Color.red;
                PointTo.OutlineMode = Outline.Mode.OutlineVisible;
            }
        }
        else
            GameManager.Instance.SetCursor(CursorStyle.Forbid);
        PathRendering = false;
    }

    public float MaxMoveRange()
    {
        return Speed * CurrentEnergy / 50;
    }

    public void StartAttack(LifeBody target)
    {
        PlayerController.StartCoroutine(PlayerController.Attack(target));
    }


    public void ChangeEnergy(float change)
    {
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + change, 0, MaxEnergy);
        EnergyBar?.UpdateImage();
    }
    public bool MoveCostEnergy(float distance)
    {
        float energy = distance / Speed * 50;
        if (CurrentEnergy>energy)
        {
            ChangeEnergy(-energy);
            return true;
        }         
        else
            return false;
    }
    public float GetMoveCostEnergy(float distance)
    {
        return distance / Speed * 50;
    }


    public float GetAttackRange()
    {
        return 1.5f;
    }


    internal void OnMouseDown(object sender, EventArgs e)
    {
        var args = (MouseEventArgs)e;
        var actions = InputManager.Instance.GetActions(args.Button);
        Dictionary<ActionType, object> temp = new Dictionary<ActionType, object>();
        foreach (var action in actions)
        {
            if (!this.actions.ContainsKey(action))
                return;
            var tuple = this.actions[action].CanRun(args.Position);
            if (tuple.Item1)
            {
                temp.Add(action,tuple.Item2);
            }
        }
        if (temp.Count == 0)
            return;
        List<ActionType> keys = new List<ActionType>(temp.Keys);    
        if (temp.Count>1)
        {
            keys.Sort((x, y) => (int)x > (int)y ? -1 : 1);
            
        }
        this.actions[keys[0]].Run(temp[keys[0]]);
    }

    public void ActionStart(EventArgs e)
    {
        var args = (ActionEventArgs)e;
        IsActing = true;
        PathRendering = false;
        if(Logger.Instance.showActionLog)
        Debug.Log($"{args.actionType}开始");
        OnActionBegin?.Invoke(null, e);
    }
    public void ActionEnd(EventArgs e)
    {
        var args = (ActionEventArgs)e;
        IsActing = false;
        if (Logger.Instance.showActionLog)
            Debug.Log($"{args.actionType}结束");
        OnActionEnd?.Invoke(null, e);
    }

    public bool TryGetData(HighValue high, LowValue low,out float result)
    {
        if(Formula.TryGetValue(high,out Dictionary<LowValue,IDataGetable> kv))
        {
            if(kv.TryGetValue(low,out IDataGetable data))
            {
                result = data.GetData(high, low, this);
                return true;
            }
        }
        result = 0f;
        return false;
    }

    public float GetData(HighValue high, LowValue low, LifeBody lifeBody = null)
    {
        if (Formula.TryGetValue(high, out Dictionary<LowValue, IDataGetable> kv))
        {
            if (kv.TryGetValue(low, out IDataGetable data))
            {
                return data.GetData(high, low, this);
            }
        }
        return 0f;
    }
}

public interface IDataGetable
{
    public float GetData(HighValue high, LowValue low,LifeBody lifeBody=null);
}


public enum HighValue
{
    肌肉组织强度,神经反射速度,灵魂强度,智力,细胞活性,速度,
    生命,生命恢复速率,精力,精力恢复速率,精力消耗,
    击打伤害,击打抗性, 击打格挡,
    穿刺伤害,穿刺抗性, 穿刺格挡,
    劈砍伤害,劈砍抗性,劈砍格挡,
    攻击半径,








    说明
}
public enum LowValue
{
    基础值,基础附加值,百分比,额外固定值
}


public struct DataValue:IDataGetable
{
    public static DataValue Zero = new DataValue() { data = 0f };
    float data;
    public DataValue(float val)
    {
        data = val;
    }
    public float GetData(HighValue high, LowValue low)
    {
        return data;
    }

    public float GetData(HighValue high, LowValue low, LifeBody lifeBody = null)
    {
        return data;
    }
}

public class ValueExpression : IDataGetable
{
    public List<Param> param;
    public ValueExpression(List<Param> @params)
    {
        param = @params;
    }
    public float GetData(HighValue high, LowValue low,LifeBody lifeBody)
    {
        Stack<Param> stack = new Stack<Param>();
        foreach(var p in param)
        {
            if(p.isOperator)
            {
                var p2 = stack.Pop().GetData(high, low, lifeBody);
                var p1 = stack.Pop().GetData(high, low, lifeBody);
                stack.Push(new Param(p.Operator.Invoke(p1, p2)));
            }
            else
                stack.Push(p);
        }
        return stack.Peek().GetData(high, low, lifeBody);
    }
}
public class Param:IDataGetable
{
    public bool isOperator;
    public Func<float, float, float> Operator;
    DataValue data;
    Tuple<HighValue,Func<LifeBody,HighValue,float>> func;
    public Param(float f)
    {
        data = new DataValue(f);
        isOperator = false;
        Operator = null;
        func = null;
    }
    public Param(Func<float, float, float> op)
    {
        isOperator = true;
        Operator = op;
        func = null;
    }
    public Param(Tuple<HighValue,Func<LifeBody,HighValue, float>> func)
    {
        isOperator = false;
        this.func = func;
        Operator = null;
    }
    public float GetData(HighValue high, LowValue low, LifeBody lifeBody = null)
    {
        if (func != null)
            return func.Item2.Invoke(lifeBody,func.Item1);
        else
            return data.GetData(high, low);
    }
}


