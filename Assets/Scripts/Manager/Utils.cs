using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class ExtensionMethods
{
    public static float GetPathRemainingDistance(this NavMeshAgent navMeshAgent)
    {
        if (navMeshAgent.pathPending ||
            navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
            navMeshAgent.path.corners.Length == 0)
            return -1f;

        float distance = 0.0f;
        for (int i = 0; i < navMeshAgent.path.corners.Length - 1; ++i)
        {
            distance += Vector3.Distance(navMeshAgent.path.corners[i], navMeshAgent.path.corners[i + 1]);
        }

        return distance;
    }
    public static Vector3[] Lerp(this Vector3[] vectors,int count)
    {
        float distance = 0.0f;
        float[] temps = new float[vectors.Length - 1];
        var result = new Vector3[count];
        int currentPointIndex = 0;
        for (int i = 0; i < vectors.Length-1; i++)
        {
            temps[i] = Vector3.Distance(vectors[i], vectors[i + 1]);
            distance += temps[i];
        }
        int part = count - 1;
        for(int i=0;i<vectors.Length-1;i++)
        {
            int currentPart =Mathf.RoundToInt(temps[i] / distance *part);
            for(int j=0;j<currentPart+1;j++)
            {
                if(j==0)
                {
                    result[currentPointIndex] = vectors[i];
                    currentPointIndex++;
                    if (currentPointIndex == count)
                        return result;
                }
                else
                {
                    result[currentPointIndex] = Vector3.Lerp(vectors[i], vectors[i + 1], (float)j / currentPart);
                    currentPointIndex++;
                    if (currentPointIndex == count)
                        return result;
                }
                if(j==currentPart)
                {
                    result[currentPointIndex] = vectors[i + 1];
                    currentPointIndex++;
                    if (currentPointIndex == count)
                        return result;
                }
            }
        }        
        return result;
    }


    public static void ChangeAnimationEventArgs(this AnimationClip clip,UnityEngine.Object obj)
    {
        var old = clip.events[0];
        var result = new AnimationEvent
        {
            functionName = old.functionName,
            floatParameter = old.floatParameter,
            intParameter = old.intParameter,
            time = old.time,
            stringParameter = old.stringParameter,
            objectReferenceParameter = obj,
        };
        clip.events = null;
        clip.AddEvent(result);
    }

    public static List<GameObject> FindAllObjectsByLayer(Transform root, int layerMak,List<GameObject> result=null)
    {
        if (result == null)
            result = new List<GameObject>();
        if (layerMak >> root.gameObject.layer == 1)
            result.Add(root.gameObject);
        for(int i=0;i<root.childCount;i++)
        {
            FindAllObjectsByLayer(root.GetChild(i), layerMak, result);
        }
        return result;
    }

}
