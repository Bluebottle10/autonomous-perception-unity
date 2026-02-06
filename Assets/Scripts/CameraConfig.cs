using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraConfig : MonoBehaviour
{
    Camera[] _cams;
    // Start is called before the first frame update
    void Start()
    {
        _cams = GetComponentsInChildren<Camera>();
        foreach (Camera c in _cams)
        {
            c.fieldOfView = 91f;
            c.aspect = 1280f / 960f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
