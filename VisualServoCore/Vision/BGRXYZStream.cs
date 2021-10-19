using System;
using System.Linq;
using System.Reactive.Linq;
using OpenCvSharp;
using Husty.OpenCvSharp.DepthCamera;

namespace VisualServoCore.Vision
{
    public class BGRXYZStream : IVision<BgrXyzMat>
    {

        // ------ Fields ------ //

        private readonly IDepthCamera _cap;
        private readonly BgrXyzPlayer _plr;
        private readonly bool _isFileSource;
        private readonly object _lockObj = new();
        private int _posIndex;
        private IDisposable _intenalConnector;
        private BgrXyzMat _frame = new();


        // ------ Properties ------ //

        public double Fps { get; }

        public int FrameCount { get; }

        public Size FrameSize { get; }


        // ------ Constructors ------ //

        public BGRXYZStream(IDepthCamera device)
        {
            _cap = device;
            Fps = device.Fps;
            FrameCount = -1;
            FrameSize = _cap.FrameSize;
            _isFileSource = false;
        }

        public BGRXYZStream(string sourceFile)
        {
            _plr = new(sourceFile);
            Fps = _plr.Fps;
            FrameCount = _plr.FrameCount;
            FrameSize = _plr.ColorFrameSize;
            _isFileSource = true;
            _posIndex = 0;
        }


        // ------ Methods ------ //

        public bool Read(ref BgrXyzMat frame)
        {
            lock (_lockObj)
            {
                if (_isFileSource)
                {
                    try
                    {
                        if (_posIndex > FrameCount - 1) return false;
                        frame = _plr.GetOneFrameSet(_posIndex++);
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
                        frame = _cap.Read();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        public IObservable<BgrXyzMat> Connect()
        {
            if (_isFileSource)
                return _plr?.Start(0).Select(f => f.Frame).Publish().RefCount();
            else
                return _cap?.Connect().Publish().RefCount();
        }

        public void Disconnect()
        {
            _intenalConnector?.Dispose();
            _cap?.Disconnect();
            _plr?.Dispose();
        }

    }
}
