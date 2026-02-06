using UnityEngine;
using System.IO;


public class CameraCapture : MonoBehaviour
{ 
    public int imageWidth = 512;
    public int imageHeight = 512;
    public int imageDepth = 8;
    Camera _cam;
    private Texture2D QuickAccessTexture = null;
    // Use this for initialization
    void Start()
    {
        _cam = GetComponent<Camera>();
    }

    public void SnapImage(string imgPath)
    {
        SetupCamera();

        _cam.Render();

        RenderTexture.active = _cam.targetTexture;
        QuickAccessTexture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        QuickAccessTexture.Apply();

        byte[] bytes = QuickAccessTexture.EncodeToJPG();
        File.WriteAllBytes(imgPath, bytes);

        _cam.targetTexture = null;
    }

    public byte[] GetBytes()
    {
        SetupCamera();

        _cam.Render();

        RenderTexture.active = _cam.targetTexture;
        QuickAccessTexture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        QuickAccessTexture.Apply();

        byte[] bytes = QuickAccessTexture.EncodeToJPG();

        _cam.targetTexture = null;

        return bytes;
    }

    void SetupCamera()
    {
        //cam = GetComponent<Camera>();
        RenderTexture texture = new RenderTexture(imageWidth, imageHeight, imageDepth, RenderTextureFormat.ARGB32);
        texture.filterMode = FilterMode.Point;
        texture.antiAliasing = 1;
        _cam.targetTexture = texture;
        imageWidth = texture.width;
        imageHeight = texture.height;
        QuickAccessTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGB24, false);
    }
}
