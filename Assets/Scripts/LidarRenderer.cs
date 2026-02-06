using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEngine;
using System.Drawing;
using Color = System.Drawing.Color;

public class LidarRenderer : MonoBehaviour
{

    public Material meshMaterial;
    public bool on = true;
    private Mesh pointMesh;
    public Color[] idColors;
    public bool showSegmentationColors = false;
    public float Period;
    private int _totalNumData;
    public List<PointCollection> Points;
    public float XLimit = 1.5f;
    public float YLimit = 30f;
    public bool KeepRecord = false;

    private StreamWriter _writer;
    private Vector3[] vertices;
    private Vector3[] normals;
    private UnityEngine.Color[] colors;
    private float[] displaySizes;
    private int[] indices;
    private GenericRadarSensor radar;
    private bool firstTime = true;
    private float intensity, range;
    private Vector3 normal;
    private int id;
    private int scanId = 0;
    private int numBeams;

    private int numSnapRays;
    private int numPointsPerSnap;
    private List<int> scanIndex;
    private float initialCamRot;
    private float previousT;
    private float _startTime;
    private float[] speed;
    private string _csvPath;



    // Use this for initialization
    void Start()
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en-US");
        radar = GetComponentInChildren<GenericRadarSensor>();

        numBeams = radar.VerticalBeams;
        _totalNumData = radar.VerticalBeams * radar.HorizontalBeams;
        numSnapRays = radar.NumberOfSnapRays;
        numPointsPerSnap = radar.NumberOfPointsPerSnap;
        //scanIndex = radar.ScanIndex;
        initialCamRot = radar.InitialCameraRotation;

        MeshFilter mf = this.gameObject.AddComponent<MeshFilter>();
        pointMesh = mf.GetComponent<MeshFilter>().mesh;

        MeshRenderer mr = this.gameObject.AddComponent<MeshRenderer>();
        mr.material = meshMaterial;
        mr.enabled = on;

        Configure();

        previousT = Time.fixedTime;

        Points = new List<PointCollection>();

        _startTime = Time.fixedTime;

        if (KeepRecord)
        {
            _csvPath = @"C:\Sandbox\Data\Perception\Radar\sim_radar.csv";
            if (File.Exists(_csvPath))
            {
                File.WriteAllText(_csvPath, string.Empty);
            }
        }

    }

    public void Activate(bool isOn)
    {
        MeshRenderer mr = this.gameObject.GetComponent<MeshRenderer>();
        mr.enabled = isOn;
        on = isOn;
    }

    void OnDrawGizmos()
    {
        if (vertices == null)
            return;

        for (int n = 0; n < vertices.Length; ++n)
        {
            Gizmos.color = colors[n];
            Gizmos.DrawWireSphere(this.transform.TransformPoint(vertices[n]), displaySizes[n]);
        }
    }

    public void Configure()
    {
        firstTime = true;

        if (pointMesh != null)
        {
            pointMesh.Clear();
        }

        vertices = new Vector3[_totalNumData];
        normals = new Vector3[_totalNumData];
        colors = new UnityEngine.Color[_totalNumData];
        indices = new int[_totalNumData];
        speed = new float[_totalNumData];
        displaySizes = new float[_totalNumData];

        pointMesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(radar.SensorRange, 1, radar.SensorRange));

        for (int n = 0; n < vertices.Length; n++)
            indices[n] = n;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if ((Time.fixedTime - previousT) > Period)
        {
            UpdatePoints();

            if (KeepRecord)
            {
                using (StreamWriter writer = File.AppendText(_csvPath))
                {
                    for (int n = 0; n < radar.Detections.Count; n++)
                    {
                        var d = radar.Detections[n];
                        float t = Time.fixedTime;
                        float range = d.Range;
                        float snr = d.SNR;
                        float rcs = d.RCS;
                        float speed = d.Speed;
                        Vector3 position = d.Position;
                        writer.WriteLine($"{t},{range},{snr},{rcs},{speed},{position.x},{position.y},{position.z}");

                    }
                }
            }
            previousT = Time.fixedTime;
        }
    }

    public void UpdatePoints()
    {
        // get radar points
        if (radar.UpdateRadarData())
        {
            for (int i = 0; i < _totalNumData; i++)
            {
                if (i < radar.Detections.Count)
                {
                    vertices[i] = radar.Detections[i].Position;
                    normals[i] = radar.Detections[i].Normal;
                    speed[i] = radar.Detections[i].Velocity.magnitude;
                    indices[i] = i;
                    colors[i] = UnityEngine.Color.green;
                    displaySizes[i] = 0.05f;
                    if (radar.Detections[i].SNR > 30)
                    {
                        colors[i] = UnityEngine.Color.red;
                        displaySizes[i] = 0.05f;
                    }
                }
                else
                {
                    vertices[i] = Vector3.zero;
                    normals[i] = Vector3.zero;
                    speed[i] = 0;
                    indices[i] = i;
                }
            }
        }


        pointMesh.vertices = vertices;
        pointMesh.colors = colors;
        pointMesh.normals = normals;
        pointMesh.RecalculateBounds();

        scanId++;

        if (firstTime)
        {
            pointMesh.SetIndices(indices, MeshTopology.Points, 0);
            firstTime = false;
        }
    }

    private void OnApplicationQuit()
    {
        if (_writer != null)
            _writer.Close();
    }
}
