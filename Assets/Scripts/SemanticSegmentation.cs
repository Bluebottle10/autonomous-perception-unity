using System.IO;
using UnityEngine;

public class SemanticSegmentation : MonoBehaviour
{
    public Shader SegmentationShader;
    public int Width = 1280;
    public int Height = 960;
    public int Depth = 8;
    public string SaveDir;
    // public RenderTexture targetTexture;
    
    private Camera _cam;
    private CameraCapture _capture;
    private Texture2D _quickAccessTexture;
    private int _frameCount = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //store camera
        _cam = GetComponent<Camera>();
        _capture = GetComponentInChildren<CameraCapture>();

        // GetParamsFromParentCamera();
        
        RenderTexture texture = new RenderTexture(Width, Height, Depth, RenderTextureFormat.ARGB32);
        texture.filterMode = FilterMode.Point;
        texture.antiAliasing = 1;
        _cam.targetTexture = texture;
        //store camera's target texture
        // _cam.targetTexture = targetTexture;
        // Width = _cam.targetTexture.width;
        // Height = _cam.targetTexture.height;
        
        _quickAccessTexture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
    }

    // Update is called once per frame
    void Update()
    {
        _cam.RenderWithShader(SegmentationShader, "");

        if (!string.IsNullOrEmpty(SaveDir))
        {
            RenderTexture.active = _cam.targetTexture;
            _quickAccessTexture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            _quickAccessTexture.Apply();
            byte[] bytes = _quickAccessTexture.EncodeToJPG();
            string maskPath = Path.Join(SaveDir, $"mask-{_frameCount}.jpg");
            File.WriteAllBytes(maskPath, bytes);
            string imgPath = Path.Join(SaveDir, $"{_frameCount}.jpg");
            _capture.SnapImage(imgPath);
            _frameCount++;
        }

    }
}
