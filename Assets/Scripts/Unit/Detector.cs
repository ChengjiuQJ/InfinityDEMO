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
            Debug.Log($"{other.gameObject.name}进入{controller.gameObject.name}检测范围");
            controller.LifeBody.passerbys.AddLast(other.gameObject.GetComponent<PlayerController>().LifeBody);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LifeBody")&& other.gameObject != controller.gameObject)
        {
            Debug.Log($"{other.gameObject.name}离开{controller.gameObject.name}检测范围");
            controller.LifeBody.passerbys.Remove(other.gameObject.GetComponent<PlayerController>().LifeBody);
        }
    }
}
