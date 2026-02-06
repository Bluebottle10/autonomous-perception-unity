using System;
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class GenericRadarSensor : MonoBehaviour
{

    //User configurable shader
    public Shader DepthShader;
    public ComputeShader ComputeShaderSource;
    private ComputeShader depthTextureToPolarDataShader;

    public float HorizontalFOV = 130f;
    public float VerticalFOV = 15f;
    public int VerticalBeams = 10;
    public int HorizontalBeams = 30;
    public float SensorRange = 120f;
    
    public float MinRange = 0.1f;
    public List<RadarData> Detections;
    public Dictionary<int, List<RadarData>> Objects;

    public float RangeSigma;
    public float AmplitudeSigma;
    public float AddedNoise = 0.5f;
    public float MaxAmplitude = 253;
    public float MinAmplitude = 20;
    public int MaxObjects = 16;


    [HideInInspector] public int NumberOfSnapRays;
    [HideInInspector] public int TotalNumberOfData;
    [HideInInspector] public int NumberOfPointsPerSnap;
    [HideInInspector] public float SnapHorizontalFieldOfView = 120f;
    [HideInInspector] public int NumberOfPointsInARing;
    [HideInInspector] public float InitialCameraRotation;

    public int NumberOfSnaps = 3;
    private Vector2[] UVs;
    private float Timestamp;
    private float HalfAngularRange;
    private Camera cam;
    private RenderTexture InternalTargetTexture;
    private int computeShaderId = -1;
    private int threadsX = 1024;
    private ComputeBuffer lidarDataBuffer;
    private ComputeBuffer xz;
    private int yawBins;
    private int pitchBins;
    private float camWidth;
    private float camHeight;
    private List<int> pointIndices;
    private string scanPatternFile = string.Empty;
    private float verticalResolution;
    private float horizontalResolution;
    private float timePrev;
    private float[] _prevRanges;
    private float[] _currRanges;
    private System.Random _random;
    private float VerticalFOVStart;

    private Vector4[] Rays;
    private float CameraRotationOffset;
    //Output array
    private Vector4[] lidarData1 = null;
    private Vector4[] lidarData2 = null;
    private bool data1 = false;
    private string _workingDir;
    private Quaternion InitialRotation;
    private List<int> ids;

    //Material _mat;


    // sigma factor * range gives appropriate sigma for gaussian noise 
    // ref https://autonomoustuff.com/wp-content/uploads/2017/08/M8_Datasheet.pdf
    private const float sigmaFactor = (0.03f / 50);

    private float nextTimestamp;

    void Start()
    {
        // assign shader
        depthTextureToPolarDataShader = Instantiate<ComputeShader>(ComputeShaderSource);

        timePrev = Time.fixedTime;

        _random = new System.Random();

        VerticalFOVStart = -VerticalFOV * 0.5f;

        // get pattern
        GeneratePattern();

        // configure
        Configure();

        // initialize ranges
        _prevRanges = new float[TotalNumberOfData];
    }


    public void Configure()
    {
        //setup data array
        lidarData1 = new Vector4[TotalNumberOfData];
        lidarData2 = new Vector4[TotalNumberOfData];


        //load compute shader
        computeShaderId = depthTextureToPolarDataShader.FindKernel("DepthTextureToPolarRanges");

        //setup data buffer
        lidarDataBuffer = new ComputeBuffer(/*count*/ TotalNumberOfData, /*stride*/ sizeof(float) * 4);
        xz = new ComputeBuffer(/*count*/ NumberOfPointsPerSnap, /*stride*/ sizeof(float) * 2);

        xz.SetData(UVs);
        depthTextureToPolarDataShader.SetBuffer(computeShaderId, "result", lidarDataBuffer);
        depthTextureToPolarDataShader.SetBuffer(computeShaderId, "xz", xz);
        depthTextureToPolarDataShader.SetInt("yawBins", Mathf.CeilToInt(NumberOfPointsPerSnap / VerticalBeams));
        depthTextureToPolarDataShader.SetInt("pitchBins", VerticalBeams);

        //setup all the compute shader variables that will not change during updates
        depthTextureToPolarDataShader.SetInt("numTotalPoints", NumberOfPointsPerSnap);

        // state
        Timestamp = Time.fixedTime;
        nextTimestamp = float.MinValue;
    }

    void FixedUpdate()
    {
        // update state
        nextTimestamp = Time.fixedTime;
    }

    void RadarUpdate()
    {
        for (int snapIndex = 0; snapIndex < NumberOfSnaps; snapIndex++)
        {
            cam.farClipPlane = SensorRange;
            cam.RenderWithShader(DepthShader, "");

            depthTextureToPolarDataShader.SetInt("snapIndex", snapIndex);
            depthTextureToPolarDataShader.SetTexture(computeShaderId, "source", InternalTargetTexture);
            depthTextureToPolarDataShader.Dispatch(computeShaderId, (NumberOfPointsPerSnap + threadsX - 1) / threadsX, 1, 1);

            transform.Rotate(Vector3.up, SnapHorizontalFieldOfView);
        }

        if (data1)
        {
            data1 = false;
            lidarDataBuffer.GetData(lidarData2);
        }
        else
        {
            data1 = true;
            lidarDataBuffer.GetData(lidarData1);
        }

        Timestamp = nextTimestamp;
        transform.localRotation = InitialRotation;
    }

    void OnDestroy()
    {
        if (lidarDataBuffer != null)
        {
            lidarDataBuffer.Release();
            lidarDataBuffer = null;
        }
    }

    public Vector4[] XZLookUp
    {
        get { return Rays; }
    }

    public void GetRangeAndIntensityAndIdAndNormal(int pointIndex, out float range, out float intensity, out int id, out Vector3 normal, bool current = true)
    {
        bool firstBuffer = data1;
        if (!current)
            firstBuffer = !firstBuffer;

        if (firstBuffer)
        {
            Vector4 data = lidarData1[pointIndex];
            float depth = new Vector3(data.x, data.y, data.z).magnitude;
            if (depth >= 0.999)
                depth = 0;
            range = depth * SensorRange;
            intensity = data.w - (int)data.w;
            id = (int)data.w;

            normal = new Vector3(data.x, data.y, data.z).normalized;
        }
        else
        {
            Vector4 data = lidarData2[pointIndex];
            float depth = new Vector3(data.x, data.y, data.z).magnitude;
            if (depth >= 0.999)
                depth = 0;
            range = depth * SensorRange;
            intensity = data.w - (int)data.w;
            id = (int)data.w;

            normal = new Vector3(data.x, data.y, data.z).normalized;
        }
    }

    public bool UpdateRadarData()
    {
        // update radar
        RadarUpdate();

        // assign default
        float trueRange = 0f;
        float intensity = 0f;
        float snr = 0f;
        float rcs = 0f;
        int id = 0;
        Vector3 normal = Vector3.zero;
        float currentT = Time.fixedTime;
        float delT = currentT - timePrev;
        _currRanges = new float[TotalNumberOfData];

        // loop through all the points and find detections

        Objects = new Dictionary<int, List<RadarData>>();
        var random = new System.Random();
        Detections = new List<RadarData>();

        for (int pointIndex = 0; pointIndex < TotalNumberOfData; pointIndex++)
        {
            // get range & volume
            GetRangeAndIntensityAndIdAndNormal(pointIndex, out trueRange, out intensity, out id, out normal);
            if (trueRange < SensorRange && trueRange > MinRange /*&& id > 3*/)
            {
                // check dictionary element
                List<RadarData> radarData = new List<RadarData>();
                if (!Objects.TryGetValue(id, out radarData))
                {
                    radarData = new List<RadarData>();
                    Objects[id] = radarData;
                }

                // get xz and compute xyz
                int snap = Mathf.FloorToInt(pointIndex / NumberOfPointsPerSnap);
                int xzIndex = pointIndex - snap * NumberOfPointsPerSnap;
                var xz = XZLookUp[xzIndex];

                // set min range in col
                float range = NextGaussian(trueRange, RangeSigma);

                // range to cartesian point
                float yaw = snap * SnapHorizontalFieldOfView + InitialCameraRotation;
                Quaternion rot = Quaternion.Euler(new Vector3(0, yaw, 0));
                float rho = (float)Mathf.Sqrt(xz.x * xz.x + 1 + xz.y * xz.y);
                float fac = (float)range / rho;

                var point = new Vector3(xz.x * fac, xz.y * fac, fac);
                point = rot * point;
                // noise

                //point = new Vector3(NextGaussian(point.x, AddedNoise),
                //                    NextGaussian(point.y, AddedNoise),
                //                    NextGaussian(point.z, AddedNoise));
                //point = new Vector3(point.x, 0, point.z);

                // ros angle
                Vector3 rosPos = new Vector3(point.z, -point.x, point.y);
                float r = Vector3.Magnitude(rosPos);
                float elAngle = Mathf.Asin(rosPos.z / (r + 0.0001f));
                float azAngle = Mathf.Asin(rosPos.y / ((r * Mathf.Cos(elAngle)) + 0.0001f));
                float elAngleDeg = Mathf.Rad2Deg * elAngle;
                float azAngleDeg = Mathf.Rad2Deg * azAngle;
                //Debug.Log($"elAngle:{elAngleDeg}, azAngle:{azAngleDeg}");

                // calculate velocity
                float speedRadial = 0;
                List<float> prevRanges = new List<float>();
                if (_prevRanges[pointIndex] != 0)
                    speedRadial = (range - _prevRanges[pointIndex]) / delT;
                _currRanges[pointIndex] = range;
                var direc = new Vector3(point.x, point.y, point.z);
                direc.Normalize();
                var velocity = direc * speedRadial;

                float amplitude = Mathf.Max(-10f * range + 70f, 0);
                //amplitude = Mathf.Max(NextGaussian(amplitude, 10f), 0);

                //// power and noise
                float noise = 20f;
                float power = amplitude + noise;

                // material dependent data
                if (id is > 100 and <= 200)
                {
                    // snr
                    // models of snr for pedestrian,
                    // choose any of below or use average
                    // slope: [-1.28334782] and intercept: 47.10297174722072
                    // slope: [-1.18736907] and intercept: 46.26180494337473
                    // slope: [-1.20745118] and intercept: 45.255478270532805
                    // slope: [-1.62041753] and intercept: 49.36705622043556
                    // slope: [-1.32281402] and intercept: 47.1968706407151
                    // slope: [-1.11404624] and intercept: 45.412327475938895

                    float slope = (float)-1.28334782;
                    float inter = (float)47.10297174722072;
                    snr = slope * trueRange + inter;

                    // rcs
                    // no correlation to distance from the radar
                    float mu = (float)0.1571157863070265;
                    float sigma = (float)0.19025438821146773;
                    rcs = NextGaussian(mu, sigma);
                    //Debug.LogError("We found pedestrian");
                }
                else
                {
                    // default environment noise goes here
                    snr = NextGaussian(Random.Range(16, 29), 0.6f);
                    rcs = NextGaussian(0, 0.05f);
                }

                Objects[id].Add(new RadarData(pointIndex,
                    id,
                    trueRange,
                    range,
                    amplitude,
                    speedRadial,
                    azAngle,
                    elAngle,
                    noise,
                    rcs,
                    power,
                    snr,
                    new Vector3(point.x, point.y, point.z),
                    new Vector3(velocity.x, velocity.y, velocity.z),
                    new Vector3(normal.x, normal.y, normal.z),
                    new Vector3(xz.x, xz.y, xz.z)));

            }
        }



        // add noise

        //Detections = new List<RadarData>();
        // find shortest measure
        Detections = new List<RadarData>();
        foreach (int key in Objects.Keys)
        {
            // check if 
            if (key is > 100 and <= 200)
            {
                // model of number of data points
                // a = -.7619
                // b = 10.1428
                // numPoints = a * x + b where x = range
                var data = Objects[key];
                var dist = data.Select(d => d.TrueRange).Average();
                float a = -.7619f;
                float b = 10.1428f;
                int numPoints = (int)MathF.Ceiling(a * dist + b);

                // need to reduce number of points
                if (numPoints > 0 && data.Count > numPoints)
                {
                    Detections.AddRange(data.OrderBy(d => Guid.NewGuid()).Take(numPoints).ToList());
                    //for (int i = 0; i < numPoints; i++)
                    //{
                    //    var idx = random.Next(0, data.Count);
                    //    Objects[key].Add(data[idx]);
                    //}
                }
                // skip if numPoints <= 0
                else if (numPoints <= 0)
                {
                    
                }
            }
            else
            {
                Detections.AddRange(Objects[key]);
            }
        }

        timePrev = currentT;
        _currRanges.CopyTo(_prevRanges, 0);
        return true;
    }

    private void GeneratePattern()
    {
        verticalResolution = VerticalFOV / (VerticalBeams - 1);
        horizontalResolution = HorizontalFOV / (HorizontalBeams);

        var vAngles = Enumerable.Range(0, VerticalBeams).Select(i => (VerticalFOVStart + i * verticalResolution) * Utils.DEG2RAD).ToList();
        NumberOfSnaps = Mathf.CeilToInt(HorizontalFOV / 120f);
        SnapHorizontalFieldOfView = HorizontalFOV / NumberOfSnaps;

        int horizontalBins = Mathf.CeilToInt(SnapHorizontalFieldOfView / horizontalResolution);
        float hStart = (-SnapHorizontalFieldOfView / 2f) + (horizontalResolution / 2);

        // collect xz coordinate where y = 1
        List<Vector4> xz = new List<Vector4>();
        for (int row = 0; row < vAngles.Count; row++)
        {
            for (int col = 0; col < horizontalBins; col++)
            {
                // get horizontal and vertical angles
                float hAngle = (hStart + horizontalResolution * col) * Utils.DEG2RAD;
                float vAngle = vAngles[row];

                // compute coordinate
                float hi = 1 / Mathf.Cos(hAngle);
                float zi = hi * Mathf.Tan(vAngle);
                float xi = hi * Mathf.Sin(hAngle);

                // store
                xz.Add(new Vector4(xi, zi, row, col));
            }
        }

        //  calculate uv min and max
        float xmin = xz.Select(p => p.x).Min();
        float xmax = xz.Select(p => p.x).Max();
        float zmin = xz.Select(p => p.y).Min();
        float zmax = xz.Select(p => p.y).Max();

        List<Vector2> uv = new List<Vector2>();
        foreach (var point in xz)
        {
            // get xz
            var xi = point.x;
            var zi = point.y;

            // compute uv
            float u = (xi - xmin) / (xmax - xmin);
            float v = 0.5f;
            if (VerticalBeams > 1)
                v = (zi - zmin) / (zmax - zmin);

            // store
            uv.Add(new Vector2(u, v));
        }

        // update
        UVs = uv.ToArray();
        Rays = xz.ToArray();
        NumberOfPointsPerSnap = UVs.Length;

        // debug
        //using (var csv = new StreamWriter(@"D:\SandBox\VPG\Data\gc.csv"))
        //{
        //    for (int i = 0; i < Rays.Length; i++)
        //        csv.WriteLine(string.Format("{0},{1},{2},{3},{4}", Rays[i].x, Rays[i].y, Rays[i].z, UVs[i].x, UVs[i].y));
        //}

        //set camera
        cam = GetComponent<Camera>();
        Vector3 InitialAngles = cam.transform.localEulerAngles;
        CameraRotationOffset = -(float)NumberOfSnaps * 0.5f * SnapHorizontalFieldOfView;

        InitialCameraRotation = -((float)NumberOfSnaps - 1.0f) * 0.5f * SnapHorizontalFieldOfView;

        InitialRotation = Quaternion.Euler(InitialAngles.x, InitialCameraRotation, InitialAngles.z);

        cam.transform.localRotation = InitialRotation;

        // set frustum
        float tolerance4SingleBeam = VerticalBeams > 1 ? 0 : 0.01f;

        float top = zmax + (float)Utils.EPSILON + tolerance4SingleBeam;
        float bottom = zmin - (float)Utils.EPSILON - tolerance4SingleBeam;
        float right = xmax + (float)Utils.EPSILON;
        float left = xmin - (float)Utils.EPSILON;
        float near = .01f;
        float far = SensorRange;

        float ratio = near;
        cam.projectionMatrix = PerspectiveOffCenter(left * ratio, right * ratio, bottom * ratio, top * ratio, near, far);

        // cliping plane

        //cam.farClipPlane = SensorRange;

        // target texture
        InternalTargetTexture = cam.targetTexture;

        // initialize beam points

        TotalNumberOfData = NumberOfPointsPerSnap * NumberOfSnaps;
        NumberOfSnapRays = Mathf.CeilToInt(SnapHorizontalFieldOfView / horizontalResolution);
        NumberOfPointsInARing = Mathf.CeilToInt(HorizontalFOV / horizontalResolution);

    }

    internal float NextGaussian(float mu = 0, float sigma = 1)
    {
        // use Box-Muller transform to generate Gaussian distribution N(mu,signma) from uniform distribution U[0,1]
        var u1 = UnityEngine.Random.Range(0f, 1f);
        var u2 = UnityEngine.Random.Range(0f, 1f);

        var randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);

        var randNormal = mu + sigma * randStdNormal;

        return randNormal;
    }

    internal Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0F * near / (right - left);
        float y = 2.0F * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;

        return m;
    }

}

public struct RadarData
{
    public int DataId;
    public int ObjectId;
    public float TrueRange;
    public float Range;
    public float Amplitude;
    public float Speed;
    public float AzAngle;
    public float ElAngle;
    public float Noise;
    public float RCS;
    public float Power;
    public float SNR;
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 Normal;
    public Vector3 Xz;

    public RadarData(int dataId,
        int objectId,
        float trueRange,
        float range,
        float amplitude,
        float speed,
        float azAngle,
        float elAngle,
        float noise,
        float rcs,
        float power,
        float snr,
        Vector3 position,
        Vector3 velocity,
        Vector3 normal,
        Vector3 xz)
    {
        DataId = dataId;
        ObjectId = objectId;
        TrueRange = trueRange;
        Range = range;
        Amplitude = amplitude;
        Speed = speed;
        AzAngle = azAngle;
        ElAngle = elAngle;
        Noise = noise;
        RCS = rcs;
        Power = power;
        SNR = snr;
        Position = position;
        Velocity = velocity;
        Normal = normal;
        Xz = xz;
    }
}
