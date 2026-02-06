using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceShading : MonoBehaviour
{
    public Material CylMaterial;

    public GameObject Intruder;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector4 location = Intruder.transform.position;
        
        CylMaterial.SetVector("_Location", location);
    }
}
