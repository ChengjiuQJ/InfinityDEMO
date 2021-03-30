using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManeger : MonoBehaviour
{
    static CameraManeger _instance;
    public static CameraManeger Instance { get { return _instance; } }
    public Camera Camera { get; private set; }
    public bool Started { get;  set; }


    //缩放摄像机
    public float distanceScale = 1.0f;
    public float timeForScrollView = 1.0f;
    private int TimeCount { get; set; }
    private bool scrollView = false;
    public Transform player;

    private float OrignalDistance { get; set; }
    public bool IsStarted { get; private set; }
    private float Distance { get; set; }

    //旋转摄像机
    private bool rotateView = false;
    private Vector3 FollowOffset { get; set; }
    private float honrizonAngle = 0f;
    private float HonrizonAngle
    {
        get { return honrizonAngle; }
        set
        {
            honrizonAngle = value;
            honrizonAngle = honrizonAngle % 360;
        }
    }
    private float verticalAngle = 0f;
    private float VerticalAngle
    {
        get { return verticalAngle; }
        set
        {
            verticalAngle = value;
            verticalAngle = honrizonAngle % 360;
        }
    }

    public Transform LookAt { get;set; }
    public Transform Follow { get; set; }

    private void Awake()
    {
        IsStarted = false;
        _instance = this;
        Camera = GetComponent<Camera>();
    }

    private void Start()
    {
        InputManager.Instance.OnMouseDown += new EventHandler(OnMouseDown);
        InputManager.Instance.OnMouseUp += new EventHandler(OnMouseUp);
        InputManager.Instance.OnMouseScrolled += new EventHandler(OnMouseScrolled);
        StartCoroutine(InitCam());
    }

    private IEnumerator InitCam()
    {
        while (GameManager.Instance.Player == null)
            yield return null;
        Follow = GameManager.Instance.Player.transform.Find("LookPos");
        FollowOffset = transform.position - Follow.position;
        OrignalDistance = Vector3.Distance(transform.position, Follow.position);
        float dis = Vector3.Distance(Follow.position, Camera.transform.position);
        float h = Follow.position.y - Camera.transform.position.y;
        float r = Mathf.Sqrt(dis * dis - h * h);
        float deg = Mathf.Asin(FollowOffset.x / r);
        HonrizonAngle = Mathf.Rad2Deg * deg;
        IsStarted = true;
    }


    private void OnDisable()
    {
        InputManager.Instance.OnMouseDown -= new EventHandler(OnMouseDown);
        InputManager.Instance.OnMouseUp -= new EventHandler(OnMouseUp);
        InputManager.Instance.OnMouseScrolled -= new EventHandler(OnMouseScrolled);
    }
    private void OnMouseUp(object sender, EventArgs e)
    {
        var args = (MouseEventArgs)e;
        switch (args.actionTypes[0])
        {
            case ActionType.RotateView:
                {
                    EndRotate();
                    break;
                }
        }
    }

    private void OnMouseDown(object sender, EventArgs e)
    {
        var args = (MouseEventArgs)e;
        switch(args.actionTypes[0])
        {
            case ActionType.RotateView:
                {               
                    StartRotate();
                    break;
                }            
        }
    }

    private void OnMouseScrolled(object sender, EventArgs e)
    {
        var args = (MouseEventArgs)e;
        switch(args.actionTypes[0])
        {
            case ActionType.ScrollViewRange:
                SetDistanceScale(-args.Value);
                break;
        }
    }

    private void Update()
    {
        if (!IsStarted)
            return;
        if(Follow!=null)
            UpdateTransform();
        if(LookAt!=null)
            UpdateRotate();
        ScrollView();
        RotateView();
    }

    private void UpdateTransform()
    {
        Camera.transform.position = Vector3.Lerp(Camera.transform.position ,Follow.position+FollowOffset,Time.deltaTime*10f);
    }
    private void UpdateRotate()
    {
        Quaternion rotate = Quaternion.LookRotation(LookAt.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotate, Time.deltaTime * 10f);
    }

    private void SetDistanceScale(float delta)
    {

        if (distanceScale != Mathf.Clamp(distanceScale + delta / 10, 0.5f, 2f))
        {
            distanceScale = Mathf.Clamp(distanceScale + delta / 10, 0.5f, 2f);
            CalDesTransform();
        }
    }



    private void CalDesTransform()
    {
        Distance = OrignalDistance * distanceScale;
        TimeCount = 0;
        scrollView = true;
    }
    private void ScrollView()
    {
        if (scrollView)
        {
            Vector3 CurrentToPlayer = Follow.transform.position - Camera.transform.position;
            Vector3 CurrentToDestination = CurrentToPlayer * (1 - Distance / CurrentToPlayer.magnitude);
            Vector3 Destination = Camera.transform.position + CurrentToDestination;
            Camera.transform.position = Vector3.Lerp(Camera.transform.position, Destination, TimeCount / (timeForScrollView * 60));
            FollowOffset = transform.position - Follow.position;
            TimeCount++;
            if (TimeCount == timeForScrollView * 60)
            {
                scrollView = false;
                TimeCount = 0;
            }
        }
    }

    public void StartRotate()
    {
        rotateView = true;
    }
    public void EndRotate()
    {
        rotateView = false;
    }

    internal IEnumerator FocusTemporary(Transform Target,float time)
    {
        Transform old = Follow;
        Follow = Target;
        LookAt = Target;
        yield return new WaitForSeconds(time);
        Follow = old;
        LookAt = old;
    }

    private void RotateView()
    {
        if (rotateView)
        {
            //左右旋转
            //HonrizonAngle -= ;
            /*float rad = HonrizonAngle * Mathf.Deg2Rad;
            float dis = Vector3.Distance(Follow.position, Camera.transform.position);
            float h = Follow.position.y - Camera.transform.position.y;
            float r =Mathf.Sqrt(dis*dis-h*h);
            float offsetX = r * Mathf.Sin(rad);
            float offsetY = 0;
            float offsetZ = -r*Mathf.Cos(rad);
            Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);
            Vector3 des = Follow.position + new Vector3(0, FollowOffset.y, 0) + offset;
            Camera.transform.position = des;
            */
            //左右旋转
            Camera.transform.RotateAround(Follow.transform.position, Vector3.up, 2*Input.GetAxis("Honrizon Rotate"));
            FollowOffset = Camera.transform.position - Follow.transform.position;


            //上下旋转
            transform.RotateAround(Follow.transform.position, Vector3.Cross(FollowOffset, Vector3.up), -Input.GetAxis("Vertical Rotate"));
            FollowOffset = Camera.transform.position - Follow.transform.position;
        }
    }
}
