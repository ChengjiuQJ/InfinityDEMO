using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    static InputManager _instance;
    private static List<KeyCode> _mouseKeyCodes = new List<KeyCode>() { KeyCode.Mouse0, KeyCode.Mouse1,KeyCode.Mouse2 };
    public static InputManager Instance { get { return _instance; } }
    public bool Started { get; private set; }
    
    public static Vector2 CusorOffset = new Vector2(10, 5);

    public Button FinishTurnButton;
    private Dictionary<ActionType,KeyCode> ShortCuts;
    private Dictionary<StateType, Dictionary<ActionType, KeyCode>> ShortCutsConfig;
    //事件
    public event EventHandler OnMouseDown;
    public event EventHandler OnMouseUp;
    public event EventHandler OnKeyDown;
    //public event EventHandler OnKeyUp;
    public event EventHandler OnMouseScrolled;


    //变量

    private void Awake()
    {
        Started = false;
        _instance = this;
        InitShortCutsConfig();
    }
    private void Start()
    {
        GameManager.Instance.FSM.OnStateChanged += OnStateChanged;
        FinishTurnButton.gameObject.SetActive(false);
        Started = true;
    }

    private void OnDisable()
    {
        GameManager.Instance.FSM.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(object sender, EventArgs e)
    {
        var args = (StateEventArgs)e;
        ChangeInputState(args.newStateType); 
    }

    private void InitShortCutsConfig()
    {
        ShortCutsConfig = new Dictionary<StateType, Dictionary<ActionType, KeyCode>>();
        //free
        ShortCuts = new Dictionary<ActionType, KeyCode>();
        ShortCuts.Add(ActionType.SwitchCamera1, KeyCode.F1);
        ShortCuts.Add(ActionType.SwitchCamera2, KeyCode.F2);
        ShortCuts.Add(ActionType.ScrollViewRange, KeyCode.None);
        ShortCuts.Add(ActionType.RotateView, KeyCode.Mouse2);
        ShortCuts.Add(ActionType.Move, KeyCode.Mouse1);
        ShortCutsConfig.Add(StateType.FreeMode, ShortCuts);

        //battle
        ShortCuts.Clear();
        ShortCuts.Add(ActionType.SwitchCamera1, KeyCode.F1);
        ShortCuts.Add(ActionType.SwitchCamera2, KeyCode.F2);
        ShortCuts.Add(ActionType.ScrollViewRange, KeyCode.None);
        ShortCuts.Add(ActionType.RotateView, KeyCode.Mouse2);
        ShortCuts.Add(ActionType.Move, KeyCode.Mouse1);
        ShortCuts.Add(ActionType.NormalAttack, KeyCode.Mouse1);
        ShortCuts.Add(ActionType.UseSkill,KeyCode.Mouse0);
        ShortCutsConfig.Add(StateType.BattleMode, ShortCuts);
    }
    
    private void Update()
    {
        foreach(KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if(Input.GetKeyDown(keyCode))
            {
                List<ActionType> actions = GetActions(keyCode);
                if (actions.Count == 0)
                    continue;
                if (_mouseKeyCodes.Contains(keyCode))
                {
                    OnMouseDown?.Invoke(null, new MouseEventArgs(keyCode, Input.mousePosition, actions));
                }
                else
                {
                    OnKeyDown?.Invoke(null, new KeyEventArgs(keyCode, actions[0]));
                }
            }
            if(Input.GetKeyUp(keyCode))
            {
                List<ActionType> actions = GetActions(keyCode);
                if (actions.Count == 0)
                    continue;
                if (_mouseKeyCodes.Contains(keyCode))
                {
                    OnMouseUp?.Invoke(null, new MouseEventArgs(keyCode, Input.mousePosition, actions));
                }
                else
                {
                    OnKeyDown?.Invoke(null, new KeyEventArgs(keyCode, actions[0]));
                }
            }
        }
        //Input.mouseScrollDelta只有y值有效
        if (Input.mouseScrollDelta.y != 0)
            OnMouseScrolled?.Invoke(null, new MouseEventArgs(KeyCode.None, Input.mousePosition, new List<ActionType> { ActionType.ScrollViewRange }, Input.mouseScrollDelta.y));

    }

    public void ChangeInputState(StateType stateType)
    {
        ShortCuts = ShortCutsConfig[stateType];
    }

    public List<ActionType> GetActions(KeyCode key)
    {
        List<ActionType> result = new List<ActionType>();
        foreach(var kv in ShortCuts)
        {
            if (kv.Value == key)
                result.Add(kv.Key);
        }
        return result;
    }

}
public class MouseEventArgs:EventArgs
{
    public KeyCode Button { get; }
    public Vector2 Position { get; }
    public float Value { get; }
    public List<ActionType> actionTypes;
    public MouseEventArgs(KeyCode button,Vector2 pos,List<ActionType> actions)
    {
        Button = button;
        Position = pos;
        actionTypes = actions;
    }
    public MouseEventArgs(KeyCode button, Vector2 pos, List<ActionType> actions,float value)
    {
        Button = button;
        Position = pos;
        actionTypes = actions;
        Value = value;
    }
}
public class KeyEventArgs:EventArgs
{
    public KeyCode keyCode;
    public ActionType actionType;
    public KeyEventArgs(KeyCode code, ActionType action)
    {
        keyCode = code;
        actionType = action;
    }
}
public enum ActionType
{
    //空
     none,
    //摄像机行为
    SwitchCamera1,SwitchCamera2,ScrollViewRange,RotateView,

    //人物行为
    Move,NormalAttack,Analyse,UseSkill,
    



    //AI行为
    TurnOver
}