using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using OpenCvSharp;
using Husty.OpenCvSharp.DepthCamera;

namespace VisualServoCore.Vision
{
    public class DummyBGRXYZStream : IVision<BgrXyzMat>
    {

        // ------ Fields ------ //

        private readonly bool _isFileSource;
        private readonly object _lockObj = new();
        private readonly int _posIndex;
        private readonly BgrXyzMat _red2mz = new(
            new Mat(180, 320, MatType.CV_8UC3, new(0, 0, 180)), 
            new Mat(180, 320, MatType.CV_16UC3, new(0, 0, 2000))
        );


        // ------ Properties ------ //

        public double Fps { get; }

        public int FrameCount { get; }

        public Size FrameSize { get; }


        // ------ Constructors ------ //

        public DummyBGRXYZStream(IDepthCamera device = null)
        {
            Fps = 10;
            FrameCount = -1;
            FrameSize = new(320, 180);
            _isFileSource = false;
        }

        public DummyBGRXYZStream(string sourceFile)
        {
            Fps = 10;
            FrameCount = 1000;
            FrameSize = new(320, 180);
            _isFileSource = true;
            _posIndex = 0;
        }


        // ------ Methods ------ //

        public bool Read(ref BgrXyzMat frame)
        {
            if (_isFileSource)
            {
                try
                {
                    if (_posIndex > FrameCount - 1) return false;
                    lock (_lockObj)
                    {
                        frame = _red2mz;
                    }
                    Thread.Sleep(1000 / (int)Fps);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    lock (_lockObj)
                    {
                        frame = _red2mz;
                    }
                    Thread.Sleep(1000 / (int)Fps);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public IObservable<BgrXyzMat> Connect()
        {
            return Observable.Range(0, int.MaxValue, ThreadPoolScheduler.Instance)
                    .Select(i =>
                    {
                        Thread.Sleep(1000 / (int)Fps);
                        return _red2mz;
                    })
                    .Publish().RefCount();
        }

        public void Disconnect()
        {

        }


    }
}
