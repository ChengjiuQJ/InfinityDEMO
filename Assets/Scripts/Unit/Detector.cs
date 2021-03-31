using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detector : MonoBehaviour
{
    public PlayerController controller;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LifeBody")&&other.gameObject!=controller.gameObject)
        {
            var target = other.gameObject.GetComponent<PlayerController>();
            if(controller.LifeBody.IsPlayer)
                target.Outline.enabled = true;
            controller.LifeBody.passerbys.AddLast(target.LifeBody);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LifeBody")&& other.gameObject != controller.gameObject)
        {
            var target = other.gameObject.GetComponent<PlayerController>();
            if(controller.LifeBody.IsPlayer)
                target.Outline.enabled = false;
            controller.LifeBody.passerbys.Remove(target.LifeBody);
        }
    }
}
