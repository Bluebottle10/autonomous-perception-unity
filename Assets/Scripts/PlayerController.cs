using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GenericRadarRenderer Renderer;
    private Animator _anim;

    private float _v = 1.5f;

    private float _omega = 50f;
    private Queue<Transform> _wayPoints = new Queue<Transform>();

    private Transform _currentTransform;
    // Start is called before the first frame update
    void Start()
    {
        _anim = GetComponent<Animator>();
        var T = GameObject.Find("Waypoints").transform;
        foreach (Transform t in T)
        {
            _wayPoints.Enqueue(t);
        }

        // add current position
        _wayPoints.Enqueue(transform);

        _currentTransform = _wayPoints.Dequeue();
    }

    // Update is called once per frame
    void Update()
    {
        if (_wayPoints.Count > 0)
        {
            if ((_currentTransform.position - transform.position).magnitude < 0.01)
            {
                if (_wayPoints.Count > 0)
                {
                    _currentTransform = _wayPoints.Dequeue();
                }
            }
            else
            {
                transform.LookAt(_currentTransform.position);
                transform.Translate(0, 0, _v * Time.deltaTime);
                _anim.SetBool("isWalking", true);
                _anim.SetFloat("direction", 1f);
            }
        }
        else
        {
            _anim.SetBool("isWalking", false);
            Renderer.KeepRecord = false;
        }

    }
}
