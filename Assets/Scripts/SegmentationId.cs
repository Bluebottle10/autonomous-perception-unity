using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentationId : MonoBehaviour {

    public Material baseMaterial;
    public int id = 0;

	// Use this for initialization
	void Start () {
		
        foreach( Renderer mr in GetComponentsInChildren<Renderer>())
        {
            for( int i = 0; i < mr.materials.Length; i++)
            {
                mr.materials[i] = new Material(baseMaterial);
                mr.materials[i].SetFloat("_DetailNormalMapScale", (float)id);
            }
        }
	}
}
