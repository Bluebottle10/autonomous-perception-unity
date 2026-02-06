using UnityEngine;
using System;
using System.Collections;

public class ColorDistanceProjectorSetup : MonoBehaviour {

    public Texture2D myTexture;
    public Shader myEqShader;
    public float FieldOfView = 360.0f;
    public float MinPitch = -90.0f;
    public float MaxPitch = 90.0f;
    public float InsideRadius = 0.0f;
    public float OutsideRadius = 100.0f;
    public string HitLayer;
    public bool RealtimePlacement = false;
    public float Opacity = 0.0f;

    private Material myMat;
    private GameObject baseNode;
    private Projector myProj;
    // Use this for initialization
    void Start()
    {
        baseNode = new GameObject("base");
        baseNode.transform.SetParent(this.transform, false);
        baseNode.transform.Rotate( -90.0f, 0f, 0f,Space.Self);

        //Create projector component for gameobject
        myProj = this.gameObject.AddComponent<Projector>();
        myProj.orthographic = true;
        myProj.orthographicSize = OutsideRadius;
        myProj.farClipPlane = OutsideRadius;

        myMat = new Material(myEqShader);
        myProj.material = myMat;
        myMat.SetTexture("_Texture", myTexture);
        myMat.SetTextureOffset("_Texture", new Vector2(0.0f, 0.0f));
        myMat.SetMatrix("_World2LocalProjector", baseNode.transform.worldToLocalMatrix);
        myMat.SetFloat("_InsideDistance", InsideRadius);
        myMat.SetFloat("_OutsideDistance", OutsideRadius);
        myMat.SetFloat("_Fov", FieldOfView);
        myMat.SetFloat("_MinPitch", MinPitch);
        myMat.SetFloat("_MaxPitch", MaxPitch);
        myMat.SetFloat("_Opacity", Opacity);
        int LayerId = LayerMask.NameToLayer(HitLayer);
        myProj.ignoreLayers = ~(1 << LayerId);
    }

    void Update()
    {
        if (RealtimePlacement)
        {
            myMat.SetMatrix("_World2LocalProjector", baseNode.transform.worldToLocalMatrix);
            myMat.SetFloat("_Opacity", Opacity);
            myMat.SetFloat("_InsideDistance", InsideRadius);
            myMat.SetFloat("_OutsideDistance", OutsideRadius);
            myMat.SetFloat("_Fov", FieldOfView);
            myMat.SetFloat("_MinPitch", MinPitch);
            myMat.SetFloat("_MaxPitch", MaxPitch);
            myProj.orthographicSize = OutsideRadius;
            myProj.farClipPlane = OutsideRadius;
        }
    }

}
