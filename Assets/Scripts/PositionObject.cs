using UnityEngine;

public class PositionObject : MonoBehaviour
{
    public float startingHeight = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Helper.PutOnTheGroundWithInitialHeight(transform, startingHeight);
    }
}
