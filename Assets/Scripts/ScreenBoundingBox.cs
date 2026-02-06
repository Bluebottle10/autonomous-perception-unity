using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

// using UnityStandardAssets.ImageEffects;
// using RockVR.Video;

public class ScreenBoundingBox : MonoBehaviour
{
    public struct Bounds
    {
        public uint min_x;
        public uint max_x;
        public uint min_y;
        public uint max_y;
    }

    //User configurable shader
    public Shader BBXShader;
    public ComputeShader ComputeShaderSource;
    public int NumberOfObjects = 1;
    public int Width;
    public int Height;
    public List<Rect> BoundingBoxes;
    public string SaveDir;
    
    private ComputeShader _depthTextureToPolarDataShader;
    private Camera _cam;
    private RenderTexture _internalTargetTexture;
    private int _computeShaderId = -1;
    private int _threadsX = 32, _threadsY = 32;
    private ComputeBuffer _lidarDataBuffer;
    private Bounds[] _buffData = null;
    private Bounds[] _initBuffData = null;
    
    private bool _configured = false;
    private StreamWriter _fs;
    private CameraCapture _capture;
    private int _frameCount = 0;

    void Start()
	{
        //store camera
        _cam = GetComponent<Camera>();
        _capture = GetComponentInChildren<CameraCapture>();

        // GetParamsFromParentCamera();

        //store camera's target texture
        _cam.targetTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.RFloat);
        _cam.targetTexture.wrapMode = TextureWrapMode.Clamp;
        _cam.targetTexture.filterMode = FilterMode.Point;
        _internalTargetTexture = _cam.targetTexture;
        //lidar settings

        _depthTextureToPolarDataShader = Instantiate<ComputeShader>(ComputeShaderSource);
        _computeShaderId = _depthTextureToPolarDataShader.FindKernel("DepthTextureToPolarRanges");

        if (!string.IsNullOrEmpty(SaveDir))
            SetupBoundingBoxFile(SaveDir);
    }

    public void Configure()
    {
        _lidarDataBuffer = new ComputeBuffer(NumberOfObjects, 4 * sizeof(uint));

        _depthTextureToPolarDataShader.SetBuffer(_computeShaderId, "result", _lidarDataBuffer);

        _depthTextureToPolarDataShader.SetInt("yawBins", Width);
        _depthTextureToPolarDataShader.SetInt("pitchBins", Height);

        _buffData = new Bounds[NumberOfObjects];
        _initBuffData = new Bounds[NumberOfObjects];

        for (int i = 0; i < NumberOfObjects; i++)
        {
            _initBuffData[i].min_x = (uint)Width;
            _initBuffData[i].max_x = 0;
            _initBuffData[i].min_y = (uint)Height;
            _initBuffData[i].max_y = 0;
        }
    }
    
    public void SetupBoundingBoxFile(string filePath)
    {
        string outputFile = Path.Join(filePath, "bounding_boxes.csv");
        _fs = new StreamWriter(outputFile, false);
    }

    public void AppendBBData(Bounds [] data, int frameNumber)
    {
        try
        {
            // _fs.Write(string.Format("{0}", frameNumber));
            _fs.Write($"{frameNumber}");
            int i = 0;
            foreach (Bounds bb in data)
            {
                // _fs.Write(string.Format(",{0},{1},{2},{3},{4}", vehicleNames[i++], bb.min_x, bb.min_y, bb.max_x, bb.max_y));
                _fs.Write($",{bb.min_x},{bb.min_y},{bb.max_x},{bb.max_y}");
            }
            _fs.Write(string.Format("\n"));
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("[ScreenBoundingBox::AppendBBData]:: {0}  {1}", ex, ex.StackTrace);
        }
    }

    public void CloseFile()
    {
        //Close file
        if (_fs != null)
            _fs.Close();
    }

    void OnDestroy()
    {
        if (_lidarDataBuffer != null)
        {
            _lidarDataBuffer.Release();
            _lidarDataBuffer = null;
        }
        
        if (!string.IsNullOrEmpty(SaveDir))
            CloseFile();
    }
    
    void LateUpdate ()
	{
        if (!_configured)
        {
            Configure();
            _configured = true;
        }
        else
        {
            _cam.RenderWithShader(BBXShader, "");

            _lidarDataBuffer.SetData(_initBuffData);
            _depthTextureToPolarDataShader.SetTexture(_computeShaderId, "source", _internalTargetTexture);
            _depthTextureToPolarDataShader.Dispatch(_computeShaderId, (Width + _threadsX - 1) / _threadsX, (Height + _threadsY - 1) / _threadsY, 1);
            _lidarDataBuffer.GetData(_buffData);

            BoundingBoxes = new List<Rect>();
            foreach (var b in _buffData)
            {
                Rect r = new Rect();
                r.position = new Vector2(b.min_x, b.min_y);
                r.size = new Vector2(b.max_x - b.min_x, b.max_y - b.min_y);
                BoundingBoxes.Add(r);
            }

            if (!string.IsNullOrEmpty(SaveDir))
            {
                AppendBBData(_buffData, _frameCount);
                string img = Path.Join(SaveDir, $"{_frameCount}.jpg");
                _capture.SnapImage(img);
                _frameCount++;
            }
        }
    }
    
}

