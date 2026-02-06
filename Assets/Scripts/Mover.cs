using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Mover : MonoBehaviour
{
    public string WayPoints = @"C:\Sandbox\Data\Perception\Lidar\waypoints.csv";
    public float Speed = 0.1f;
    public float PositionTolerance = 0.02f;
    public float VehicleSize = 1f;

    private List<Transform> _wayPoints = new List<Transform>();
    private Transform _targetFrame;
    private Transform _startFrame;

    private Transform _body;
    private float _t = 0;
    private int _count = 0;

    // Start is called before the first frame update
    void Start()
    {
        // collect waypoints
        using (var reader = new StreamReader(WayPoints))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                var toks = line.Split(",");
                float x = float.Parse(toks[0]);
                float z = float.Parse(toks[1]);

                // calculate get hit to the ground from f,l,r,r
                RaycastHit hit;
                Vector3 rayOrigin = new Vector3(x, 1000f, z);
                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, Mathf.Infinity))
                {
                    float h = 1000f - hit.distance + 1;
                    var pos = new Vector3(x, h, z);
                    GameObject go = new GameObject();
                    go.transform.position = pos;
                    go.transform.rotation = Quaternion.identity;
                    _wayPoints.Add(go.transform);
                }
                else
                {
                    throw new Exception("Please choose waypoints to be inside of the terrain");
                }
            }
        }

        // create sensor frame
        GameObject sensorFrame = new GameObject();
        sensorFrame.name = "sensor frame";
        Camera cam = sensorFrame.AddComponent<Camera>();
        GameObject lidar = Instantiate(Resources.Load("Generic Lidar", typeof(GameObject))) as GameObject;
        lidar.transform.parent = sensorFrame.transform;

        _body = sensorFrame.transform;

        // setup the body
        _startFrame = _wayPoints[_count];
        _body.position = _startFrame.position;
        _targetFrame = _startFrame;
        SetPosition();
        SetPitchNRoll();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (_count < _wayPoints.Count)
        {
            var bodyPos = new Vector2(_body.position.x, _body.position.z);
            var targetPos = new Vector2(_targetFrame.position.x, _targetFrame.position.z);
            if (Vector2.Distance(bodyPos, targetPos) < PositionTolerance)
            {
                _startFrame = _targetFrame;
                if (++_count < _wayPoints.Count)
                {
                    _targetFrame = _wayPoints[_count];
                    _body.LookAt(_targetFrame);
                }
                _t = 0;

            }
            else
            {
                SetPosition();
                SetPitchNRoll();
            }
        }
    }

    void SetPosition()
    {
        // interpolate in x, z
        _t += Time.deltaTime * Speed;
        Vector2 startPos = new Vector2(_startFrame.position.x, _startFrame.position.z);
        Vector2 targetPos = new Vector2(_targetFrame.position.x, _targetFrame.position.z);
        var p = Vector2.Lerp(startPos, targetPos, _t);

        // calculate get hit to the ground from f,l,r,r
        RaycastHit hit;
        Vector3 rayOrigin = new Vector3(p.x, 1000f, p.y);
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, Mathf.Infinity))
        {
            float vehicleHeight = 1000f - hit.distance + 1;
            _body.position = new Vector3(p.x, vehicleHeight, p.y);
        }
        else
        {
            throw new Exception("Please choose waypoints to be inside of the terrain");
        }
    }

    void SetPitchNRoll()
    {
        // get fwrl
        Vector3 forward = _body.position + _body.forward * VehicleSize;
        Vector3 rear = _body.position + _body.forward * -VehicleSize;
        Vector3 right = _body.position + _body.right * VehicleSize;
        Vector3 left = _body.position + _body.right * -VehicleSize;
        float forwardDist = 0;
        float rearDist = 0;
        float rightDist = 0;
        float leftDist = 0;

        // calculate get hit to the ground from f,l,r,r
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(forward, Vector3.down, out hit, Mathf.Infinity))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            Debug.Log($"Did hit forward");
            forwardDist = hit.distance;
        }

        if (Physics.Raycast(rear, Vector3.down, out hit, Mathf.Infinity))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            Debug.Log($"Did hit rear");
            rearDist = hit.distance;
        }

        if (Physics.Raycast(right, Vector3.down, out hit, Mathf.Infinity))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            Debug.Log($"Did hit right");
            rightDist = hit.distance;
        }

        if (Physics.Raycast(left, Vector3.down, out hit, Mathf.Infinity))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            Debug.Log($"Did hit left");
            leftDist = hit.distance;
        }

        // calculate pitch
        float deltaPitch = forwardDist - rearDist;
        float pitch = Mathf.Rad2Deg * Mathf.Atan(deltaPitch / (2 * VehicleSize));

        // calculate roll
        float deltaRoll = leftDist - rightDist;
        float roll = Mathf.Rad2Deg * Mathf.Atan(deltaRoll / (2 * VehicleSize));


        _body.Rotate(Vector3.right, pitch, Space.Self);
        _body.Rotate(Vector3.forward, roll, Space.Self);

        Debug.Log($"Body rotation: {_body.rotation}");
    }
}
