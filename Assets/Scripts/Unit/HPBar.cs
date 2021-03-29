using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    public static HPBar Instance { get; private set; }
    [SerializeField]
    PlayerController playerController;
    LifeBody LifeBody { get { return playerController.LifeBody; } }
    [SerializeField]
    Image health_buffer;
    [SerializeField]
    Image health;
    [SerializeField]
    float CurrentPercent;
    public void UpdateImage()
    {
        CurrentPercent= LifeBody.CurrentHP / LifeBody.MaxHP;
        health.fillAmount = CurrentPercent;
    }
    private void Update()
    {
        if (health_buffer.fillAmount != CurrentPercent)
        {
            health_buffer.fillAmount = Mathf.Lerp(health_buffer.fillAmount, CurrentPercent, Time.deltaTime * 10);
        }
    }
    private void Awake()
    {
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
    }
}
