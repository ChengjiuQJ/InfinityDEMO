using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnergyBar : MonoBehaviour
{
    public static EnergyBar Instance { get; private set; }
    [SerializeField]
    PlayerController playerController;
    LifeBody LifeBody { get { return playerController.LifeBody; } }
    [SerializeField]
    Slider EnergySlider;
    public bool IsStarted { get; private set; }
    public void UpdateImage()
    {
        if(IsStarted)
            EnergySlider.value = LifeBody.CurrentEnergy / LifeBody.MaxEnergy;
    }
    private void Awake()
    {
        IsStarted = false;
        Instance = this;
    }
    private void Start()
    {
        StartCoroutine(LaterStart());
    }
    private IEnumerator LaterStart()
    {
        while (GameManager.Instance.Player == null)
            yield return null;
        playerController = GameManager.Instance.Player;
        IsStarted = true;
    }
}
