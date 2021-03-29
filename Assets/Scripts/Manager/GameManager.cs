using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } }

    public const float secondsPerTurn = 2f;

    private AudioSource[] audioSources;
    public AudioSource  BGM { get; set; }
    public AudioSource SoundEffect  { get; set; }
    public List<AudioClip> bgms;
    public List<AudioClip> ses;

    public Texture2D[] Cursors;


    public PlayerController Player { get; set; }
    public bool Ready { get; private set; }

    //游戏常数
    public const float rayMaxDistance = 1000f;
    public const int targetFrameRate = 60;
    public const float MaxMoveSpeed = 5f;
    public NavMeshSurface Nav1 { get; private set; }
    public NavMeshSurface Nav2 { get; private set; }

    public delegate IEnumerator TasksAfterStart();
    private FSMManager fsm;
    public FSMManager FSM
    {
        get 
        { 
            if (fsm != null)
                return fsm;
            fsm = new FSMManager();
            fsm.RegistState(StateType.FreeMode, new FreeModeState());
            fsm.RegistState(StateType.BattleMode, new BattleModeState());           
            return fsm;
        }
    }
    private void Awake()
    {
        Ready = false;
        _instance = this;
        audioSources = GetComponents<AudioSource>();
        BGM = audioSources[0];
        SoundEffect = audioSources[1];
        Application.targetFrameRate = targetFrameRate;
        DontDestroyOnLoad(gameObject);
        List<GameObject> objs = new List<GameObject>();
        int laymask = 1 << LayerMask.NameToLayer("Obstacle");
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            objs.AddRange(ExtensionMethods.FindAllObjectsByLayer(root.transform, laymask));
        }
        foreach(var obj in objs)
        {
            var modifier = obj.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = 1;
        }
        Nav1 = gameObject.AddComponent<NavMeshSurface>();
        Nav1.agentTypeID = 0;
        Nav1.ignoreNavMeshAgent = true;
        Nav1.layerMask = new LayerMask() { value = (1 << LayerMask.NameToLayer("Ground") )+laymask};
        Nav1.BuildNavMesh();
        DataManager.Instance.LoadAllData();
    }
    private void Start()
    {
        PlayBGM(0);
        StartCoroutine(AfterStart());
        Ready = true;
    }
    private void Update()
    {
        FSM.UpdateState();
    }

    IEnumerator AfterStart()
    {
        while(!InputManager.Instance.Started||Player==null)
        {
            yield return null;
        }      
        FSM.StartFSM(StateType.FreeMode);
    }

    public void PlayBGM(int index)
    {
        BGM.clip = bgms[index];
        BGM.loop = true;
        BGM.Play();
    }
    public void PlaySE(int index)
    {
        SoundEffect.clip = ses[index];
        SoundEffect.loop = false;
        SoundEffect.Play();
    }

    internal void MuteBGM()
    {
        BGM.pitch = 0f;
    }

    internal void SetCursor(CursorStyle mode)
    {
        Cursor.SetCursor(Cursors[(int)mode], new Vector2(10, 5), CursorMode.Auto);
    }

}
public enum CursorStyle
    {
        Move_Default,Move_Eable,Move_Disable,Attack_Eable, Attack_Disable,
    Forbid
}

public class BattleModeState : StateBase
{
    public BattleModeState()
    {
        stateType = StateType.BattleMode;
    }

    public override void OnEnter(params object[] args)
    {

    }

    public override void OnExit(params object[] args)
    {
    }

    public override void Update(params object[] args)
    {
    }
}

public class FreeModeState : StateBase
{
    public FreeModeState()
    {
        stateType = StateType.FreeMode;
    }
    public override void OnEnter(params object[] args)
    {
        CameraManeger.Instance.Follow = GameManager.Instance.Player.transform.Find("LookPos");
        CameraManeger.Instance.LookAt = CameraManeger.Instance.Follow;
        CameraManeger.Instance.Started = true;
    }

    public override void OnExit(params object[] args)
    {

    }

    public override void Update(params object[] args)
    {

    }
}

