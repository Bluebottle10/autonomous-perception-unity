using UnityEngine;

public class LookAt : MonoBehaviour
{
    public Transform target;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // transform.position = target.position + new Vector3(0, 2f, 5f);
        transform.LookAt(target.position + new Vector3(0, 1f, 0f) );
        
    }
}
