using System;
using OpenCvSharp;

namespace VisualServoCore.Vision
{
    public interface IVision<TImage>
    {

        public double Fps { get; }

        public int FrameCount { get; }

        public Size FrameSize { get; }

        public bool Read(ref TImage frame);

        public IObservable<TImage> Connect();

        public void Disconnect();

    }
}
