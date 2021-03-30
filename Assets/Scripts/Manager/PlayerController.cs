using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public Outline Outline { get;private set; }
    public float RotateSpeed=10f;
    public LifeBody LifeBody { get; set; }
    public Text CurrentEnergyText;
    public event EventHandler OnActionEnd;
    public Transform LookAtTarget { get; set; }
    public Material material;
    public NavMeshSurface Nav { get; private set; }
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        Outline = gameObject.AddComponent<Outline>();
        Outline.OutlineMode = Outline.Mode.OutlineHidden;
        Outline.enabled = false;
    }
    private void Start()
    {
        StartCoroutine(LaterStart());   
    }

    IEnumerator LaterStart()
    {
        while (!DataManager.Instance.Ready)
            yield return null;
        LifeBody = new LifeBody(gameObject, this, 1);
        LifeBody.FSM.StartFSM(StateType.idle);
    }

    private void Update()
    {
        SwitchAnimation();
        LifeBody?.FSM.UpdateState();
        UpdateRotate();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if(LifeBody!=null&&CurrentEnergyText!=null)
        CurrentEnergyText.text = $"{(int)LifeBody.CurrentEnergy}/{LifeBody.MaxEnergy}";
    }

    private void UpdateRotate()
    {
        if(LookAtTarget!=null)
        {
            Quaternion rotate = Quaternion.LookRotation(LookAtTarget.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotate, Time.deltaTime * RotateSpeed);
        }
    }

    void SwitchAnimation()
    {        
        SwitchMoveAnimation();
    }
    void SwitchMoveAnimation()
    {
        float speed = agent.velocity.magnitude/agent.speed;
        animator.SetFloat("Speed", speed);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args">[0]:Vector3 destination,[1]StopDistance,[2]:LifeBody Target</param>
    /// <returns></returns>
    public IEnumerator Move(params object[] args)
    {
        agent.isStopped = false;
        agent.SetDestination((Vector3)args[0]);
        agent.stoppingDistance = args.Length > 1 ? (float)args[1] : 0f;
        yield return new WaitWhile(()=>agent.pathPending);
        float distance = agent.GetPathRemainingDistance();
        float d = 0f;
        while((d=agent.GetPathRemainingDistance()) >agent.stoppingDistance+ Mathf.Epsilon)
        {
            float delta = distance - d;
            distance = d;
            if(!LifeBody.MoveCostEnergy(delta))
            {
                //能量用尽;
                agent.isStopped = true;
                if(args.Length>=3)
                {
                    var e = new ActionEventArgs(ActionType.Move,ActionStatus.Failed,ActionFailedSituation.NoEnergy,args[2]);
                    OnActionEnd?.Invoke(null,e);
                }
                else
                    OnActionEnd?.Invoke(null,new ActionEventArgs(ActionType.Move,ActionStatus.Success));                
                yield break;
            }
            yield return null;
        }      
        if(args.Length>=3)
            OnActionEnd?.Invoke(null,new ActionEventArgs(ActionType.Move,ActionStatus.Success,args[2]));
        else
            OnActionEnd?.Invoke(null,new ActionEventArgs(ActionType.Move,ActionStatus.Success));      
    }

    public IEnumerator Attack(LifeBody target)
    {
        LookAtTarget=target.GameObject.transform;
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(1/RotateSpeed);
        LookAtTarget = null;
        var clip = animator.GetCurrentAnimatorClipInfo(0)[0].clip;
        clip.ChangeAnimationEventArgs(target.PlayerController);       
        yield return new WaitForSeconds(clip.length);
        OnActionEnd?.Invoke(null, new ActionEventArgs(ActionType.NormalAttack,ActionStatus.Success));
    }




    private void OnHit(PlayerController playerController)
    {
        //判断是否击中

        float baseHitRate = DataManager.Instance.GetHighValue(LifeBody,HighValue.命中率);



        playerController.animator.SetTrigger("OnHit");
        LifeBody enemy = playerController.LifeBody;
        if (LifeBody.CurrentWeapon == null)
            LifeBody.CurrentEquipments.Add(Equipment.UnArmed);
        float dmg1 = DataManager.Instance.GetHighValue(LifeBody, HighValue.击打伤害);
        float percent1 = DataManager.Instance.GetHighValue(enemy, HighValue.击打抗性);
        float shield1 = DataManager.Instance.GetHighValue(enemy, HighValue.击打格挡);
        if (percent1 > 0)
            dmg1 = Mathf.Max(dmg1 / (1 + percent1) - shield1, 0f);
        else
            dmg1 = Mathf.Max(dmg1 * (1 + percent1) - shield1, 0f);

        float dmg2 = DataManager.Instance.GetHighValue(LifeBody, HighValue.劈砍伤害);
        float percent2 = DataManager.Instance.GetHighValue(enemy, HighValue.劈砍抗性);
        float shield2 = DataManager.Instance.GetHighValue(enemy, HighValue.击打格挡);
        if (percent2 > 0)
            dmg2 = Mathf.Max(dmg2 / (2 + percent2) - shield2, 0f);
        else
            dmg2 = Mathf.Max(dmg2 * (2 + percent2) - shield2, 0f);

        float dmg3 = DataManager.Instance.GetHighValue(LifeBody, HighValue.穿刺伤害);
        float percent3 = DataManager.Instance.GetHighValue(enemy, HighValue.穿刺抗性);
        float shield3 = DataManager.Instance.GetHighValue(enemy, HighValue.击打格挡);
        if (percent3 > 0)
            dmg3 = Mathf.Max(dmg3 / (1 + percent3) - shield3, 0f);
        else
            dmg3 = Mathf.Max(dmg3 * (1 + percent3) - shield3, 0f);
        if (LifeBody.CurrentWeapon == null)
            LifeBody.CurrentEquipments.Remove(Equipment.UnArmed);
        float dmg = dmg1 + dmg2 + dmg3;
        Vector3 p = CameraManeger.Instance.Camera.WorldToScreenPoint(playerController.transform.Find("LookPos").position);
        UIManager.Instance.ShowDamage(Mathf.RoundToInt(dmg).ToString(), new Vector2(p.x, p.y));
        playerController.LifeBody.ChangeHP(null, -dmg);
    }
    public void OnBattleStart()
    {
        animator.SetBool("InBattle", true);
    }
    public void OnBattleEnd()
    {
        animator.SetBool("InBattle", false);
    }
    public void StopAllActions()
    {
        StopAllCoroutines();
        agent.isStopped = true;
        agent.ResetPath();

        OnActionEnd?.Invoke(null, new ActionEventArgs(ActionType.none,ActionStatus.Abort));
    }

    public IEnumerator DoSomething()
    {
        yield return new WaitForSeconds(2.0f);
        LifeBody.FSM.ChangeState(StateType.turnOver);
    }

    public IEnumerator Die()
    {
        animator.SetBool("Die", true);
        yield return new WaitForSeconds(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length);
    }

}
