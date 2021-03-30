using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public bool Playing { get; internal set; }

    //战斗
    public Text BattleText_Text;
    public Text Text;
    public GameObject BattleText;
    public GameObject DamageText;
    public Text DamageText_Text;


    private void Awake()
    {
        Instance = GetComponent<UIManager>();
        BattleText.SetActive(false);
        DamageText.SetActive(false);
    }

    internal void TurnBegin()
    {
        
        BattleText_Text.text = "回合开始";
        StartCoroutine(ShowBattleText());
    }
    internal void BattleStart()
    {
        BattleText_Text.text = "战斗开始";
        StartCoroutine(ShowBattleText());
    }
    private IEnumerator ShowBattleText()
    {
        Playing = true;
        RectTransform rect = BattleText.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(-722, 0);
        BattleText.SetActive(true);
        Vector2 target = new Vector2(200, 0);
        while(Vector2.Distance(rect.anchoredPosition,target)>=10f)
        {
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, target, Time.deltaTime*10f);
            yield return null;
        }
        target = new Vector2(0, 0);
        while (Vector2.Distance(rect.anchoredPosition, target) >= 10f)
        {
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, target, Time.deltaTime * 10f);
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
        BattleText.SetActive(false);
        Playing = false;
    }

    public void ShowDamage(string damage,Vector2 position, bool crit = false,float time=1.0f)
    {
        DamageText_Text.text= damage;
        DamageText_Text.rectTransform.anchoredPosition = position;
        DamageText_Text.rectTransform.localScale = Vector3.one;
        StartCoroutine(ShowDamageText(time));
    }
    private IEnumerator ShowDamageText(float time)
    {
        DamageText.SetActive(true);
        float x = DamageText_Text.rectTransform.anchoredPosition.x;
        float y = DamageText_Text.rectTransform.anchoredPosition.y;
        var s = DOTween.Sequence();
        Tweener moveX = DamageText_Text.rectTransform.DOAnchorPosX(x + UnityEngine.Random.Range(-100, 100), time);
        Tweener scale = DamageText_Text.rectTransform.DOScale(1.5f, time);
        Tweener moveY = DamageText_Text.rectTransform.DOAnchorPosY(y+100f, time/2);      
        Tweener moveY2 = DamageText_Text.rectTransform.DOAnchorPosY(y+50f, time / 2);
        s.Append(moveY);
        s.Append(moveY2); 
        //s.Join(moveX);
        //s.Join(scale);                 
        yield return new WaitForSeconds(time);
        DamageText.SetActive(false);
    }
    public void ShowText(string text,Vector2 position,float time=1f)
    {
        Text.text = text;
        Text.rectTransform.anchoredPosition = position;
        StartCoroutine(ShowText(time));
    }
    public IEnumerator ShowText(float time)
    {
        Text.gameObject.SetActive(true);
        yield return new WaitForSeconds(time);
        Text.gameObject.SetActive(false);
    }


}
