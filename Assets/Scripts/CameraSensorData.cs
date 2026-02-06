using System;
using System.Collections.Generic;
using System.Text;

namespace CameraSensors
{
    public class CameraSensorData
    {
        public uint FrameHeight { get; set; }
        public uint FrameWidth { get; set; }
        public uint Step { get; set; }
        public byte[] Data { get; set; }
        public string DistortionModel { get; set; }
        public double[] D { get; set; }
        public double[] K { get; set; }
        public double[] R { get; set; }
        public double[] P { get; set; }
        public UInt32 BinningX { get; set; }
        public UInt32 BinningY { get; set; }
        public ROI RegionOfInterest { get; set; }
    }

    public class ROI
    {
        public UInt32 XOffset { get; set; }
        public UInt32 YOffset { get; set; }
        public UInt32 Height { get; set; }
        public UInt32 Width { get; set; }
        public bool DoRectify { get; set; }
    }
}
