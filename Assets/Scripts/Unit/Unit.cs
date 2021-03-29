using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit
{
    public float CurrentHP { get; protected set; }
    public virtual float MaxHP { get; protected set; }
    public bool IsDead { get; private set; }
    public GameObject GameObject { get; private set; }
    public virtual void ChangeHP(Unit sender,float value)
    {
        CurrentHP = Mathf.Clamp(CurrentHP + value, 0f, MaxHP);
        if (CurrentHP <= 0)
            IsDead = true;
    }
    protected Unit(GameObject gameObject)
    {
        GameObject = gameObject;
        IsDead = false;
    }
}
