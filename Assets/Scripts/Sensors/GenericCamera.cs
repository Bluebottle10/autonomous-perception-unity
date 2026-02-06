using System;
using System.IO;
using UnityEngine;

namespace CameraSensors
{
    public class GenericCamera : MonoBehaviour
    {
        private Camera _cam;
        private bool _isInitialized;
        private string _modelName;
        private DateTime _nextDateTime = DateTime.UtcNow;
        private float _period;

        private Texture2D _quickAccessTexture;
        private RenderTexture _source;
        private RenderTexture _target;

        public int Depth;
        public float FieldOfView;
        public Shader FisheyeShader;
        public int Height;
        public RenderTexture OrlacoTexture;
        //public EventHandler PublishHandler;
        //public Material ShaderMaterial;
        public int Width;

        public void Initialize(float period, string modelName)
        {
            if (modelName == "Orlaco")
            {
                _modelName = "Orlaco";
                InitializeOrlacoFisheye(period);
            }
        }

        void Start()
        {
            InitializeOrlacoFisheye(0.2f);
        }


        public void InitializeOrlacoFisheye(float period)
        {
            Width = 1280;
            Height = 960;
            Depth = 24;
            FieldOfView = 90f;

            _period = period;
            _cam = GetComponent<Camera>();


            _source = Instantiate(OrlacoTexture);
            _target = new RenderTexture(Width, Height, Depth);

            _cam.targetTexture = _source;

            _cam.fieldOfView = FieldOfView;
            _cam.aspect = Width / (float)Height;

            _quickAccessTexture = new Texture2D(Width, Height, TextureFormat.RGB24, false);

            _isInitialized = true;
        }

        // Update is called once per frame
        // ReSharper disable once UnusedMember.Local
        private void LateUpdate()
        {
            if (!_isInitialized)
                return;

            var dateTime = DateTime.UtcNow;

            if (dateTime < _nextDateTime) return;

            _nextDateTime = dateTime.AddSeconds(_period);
            SnapImage();
        }

        public void SnapImage()
        {
            //if (PublishHandler == null)
            //    return;

            var fx = 790.0;
            var fy = 520.0;
            var cx = 640.0;
            var cy = 480.0;
            double k1 = -0.001665184937724915f;
            double k2 = 0.0031854844982924296f;
            double k3 = 0.001285695977955832f;
            double k4 = -0.0020373408220985285f;
            double k5 = 0;


            var cameraSensorData = new CameraSensorData
            {
                FrameHeight = (uint)Height,
                FrameWidth = (uint)Width,
                Step = (uint)Width * 3,
                DistortionModel = "plumb_bob",
                D = new[] { k1, k2, k3, k4, k5 },
                K = new[]
                {
                    fx, 0, cx,
                    0, fy, cy,
                    0, 0, 1
                },
                R = new double[]
                {
                    1, 0, 0,
                    0, 1, 0,
                    0, 0, 1
                },
                P = new[]
                {
                    fx, 0, cx, 0,
                    0, fy, cy, 0,
                    0, 0, 1, 0
                },
                BinningX = 0,
                BinningY = 0,
                RegionOfInterest = new ROI
                {
                    XOffset = 0,
                    YOffset = 0,
                    Height = 0,
                    Width = 0,
                    DoRectify = false
                }
            };


            //if (_modelName == "Orlaco")
            //{
            var mat = new Material(FisheyeShader);
            mat.SetFloat("_HFOV", 120f);
            mat.SetFloat("_VFOV", 90f);
            mat.SetVector("_Distortion", new Vector4((float)k1, (float)k2, (float)k3, (float)k4));

            //apply fish-eye
            Graphics.Blit(_source, _target, mat);
            //}
            //else
            //{
            //    return;
            //}

            RenderTexture.active = _target;

            _quickAccessTexture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            _quickAccessTexture.Apply();

            byte[] bytes = _quickAccessTexture.EncodeToJPG();
            File.WriteAllBytes(@"D:\Sandbox\Data\Images\orlaco.jpg", bytes);

            cameraSensorData.Data = _quickAccessTexture.GetRawTextureData();

            

            //PublishHandler?.Invoke(cameraSensorData, EventArgs.Empty);
        }
    }
}