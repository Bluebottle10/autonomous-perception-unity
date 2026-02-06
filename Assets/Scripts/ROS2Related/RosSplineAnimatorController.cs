using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class RosSplineAnimatorController : MonoBehaviour
{
    public SplineAnimate splineAnim;
    
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<BoolMsg>("stopngo", ShouldStop);
    }

    void ShouldStop(BoolMsg msg)
    {
        if (msg.data == true)
        {
            splineAnim.Pause();
        }
        else
        {
            splineAnim.Play();
        }
    }
}