using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logger : MonoBehaviour
{
    public static Logger Instance { get; private set; }

    private void Awake()
    {
        Instance = GetComponent<Logger>();
    }
    public bool showFSMLog;
    public bool showActionLog;
    public bool showPlayerControlActionLog;
}
