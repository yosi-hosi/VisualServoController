using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Husty.OpenCvSharp;
using Husty.OpenCvSharp.DepthCamera;
using System.Diagnostics;
using System.IO.Ports;

namespace VisualServoCore.Controller
{
    public class DepthFusedController : IController<BgrXyzMat, double>
    {

        // ------ Fields ------ //

        private readonly double _gain;
        private readonly int _maxWidth;
        private readonly int _maxDistance;
        private readonly YoloDetector _detector;
        private readonly object _locker = new();
        private readonly Radar _radar;
        private Point[] _points;
        private Point? _targetPoint;
        private double _steer;
        private double _speed;
        private readonly PositionPID _PID;
        private readonly AngleManager _angleManager;

        private SerialPort _port;

        // ------ Constructors ------ //

        public DepthFusedController(double gain, int maxWidth, int maxDistance)
        {

            // initialize detector instance and some parameters

            var cfg = "..\\..\\..\\..\\..\\model\\_.cfg";
            var weights = "..\\..\\..\\..\\..\\model\\_.weights";
            var names = "..\\..\\..\\..\\..\\model\\_.names";
            _detector = new(cfg, weights, names, new(512, 288), 0.5f);
            _gain = gain;                       // for steering
            _maxWidth = maxWidth;               // maximum number of X-axis
            _maxDistance = maxDistance;         // maximum number of Z-axis
            _radar = new(maxWidth, maxDistance);
            _PID = new(1, 0.0, 0.0, 0.1);
            _angleManager = new();
            _angleManager.RightFired = (s, e) => _port?.WriteLine(e.ToString());//イベントの登録。これ以降、発火されるたびに実行される
            _angleManager.LeftFired = (s, e) => _port?.WriteLine(e.ToString());

            //var name = SerialPort.GetPortNames()[1];
            //_port = new("COM7", 9600);
            //_port.Open();
        }


        // ------ Public Methods ------ //

        public LogObject<double> Run(BgrXyzMat input)
        {
            _points = FindBoxes(input).Select(r => GetXZ(input, r)).Where(xz => xz is not null).Select(xz => (Point)xz).ToArray();
            _targetPoint = SelectTarget(input, _points) ?? null;
            if (_targetPoint is Point target)
            {
                _steer = CalculateSteer(target);

                if (NotifyAlert(target))
                {
                    _speed = 0;
                }
                _speed = 0.5;
                //var error = (4000 - Math.Sqrt(Math.Pow(target.X, 2) + Math.Pow(target.Y, 2))) / 1000;
                //_speed = (0.5 + _PID.GetControl(error)).InsideOf(0.0, 1.0);            
                
            }          
            return new(DateTimeOffset.Now, _steer, _speed);
        }

        public Mat GetGroundCoordinateResults()
        {
            return _radar.GetRadar(_locker, _points, _targetPoint);
        }


        // ------ Private Methods ------ //

        private Rect[] FindBoxes(BgrXyzMat input)
        {
            var w = input.BGR.Width;
            var h = input.BGR.Height;
            var results = _detector.Run(input.BGR);
            var boxes = results.Where(r => r.Label is "person")
                .Where(r => r.Probability > 0.7)
                .Select(r =>
                {
                    r.DrawBox(input.BGR, new(0, 0, 160), 2);
                    return r.Box.Scale(w, h).ToRect();
                })
                .ToArray();
            return boxes;
        }

        private Point? GetXZ(BgrXyzMat input, Rect box)
        {
            try
            {
                var targetX = 0;
                var targetZ = 0;
                var count = 0;
                var center = box.GetCenter();
                for (int y = center.Y - 2; y < center.Y + 2; y++)
                {
                    for (int x = center.X - 2; x < center.X + 2; x++)
                    {
                        var xyz = input.GetPointInfo(new(x, y));
                        if (xyz.Z is not 0)
                        {
                            targetX += (int)xyz.X;
                            targetZ += (int)xyz.Z;
                            count++;
                        }
                    }
                }
                if (count is 0) throw new Exception();
                return new(targetX / count, targetZ / count);
            }
            catch
            {
                return null;
            }
        }

        private Point? SelectTarget(BgrXyzMat input, Point[] points)
        {
            //with tracking


            //without tracking
            
            if (points.Length is 0) return null;

            var target = new Point(int.MaxValue, 0);
            foreach(var p in points)
            {
                if (p.Y < _maxDistance)
                {
                    if (Math.Abs(p.X) < Math.Abs(target.X))
                    {
                        target = p;
                    }
                }
            }
            
            //target = _angleManager.ArrangeCoordinate(target);

            if (target.X == int.MaxValue)
                return null;
            Debug.WriteLine(target);
            return target;
        }

        private double CalculateSteer(Point point)
        {
            //var steer = (_gain * Math.Atan2(point.X, point.Y) * (180 / Math.PI)).InsideOf(-40, 40);

            //距離を考慮した場合
            var steer = (_gain * (Math.Atan2(point.X, point.Y) * 180 / Math.PI) / (Math.Sqrt(point.X * point.X + point.Y * point.Y) / 1000)).InsideOf(-40, 40);

            //pure pursuit
            //var steer = (Math.Atan2(2 * 2100 * point.X, Math.Pow(point.X, 2) + Math.Pow(point.Y + 2700, 2)) * 180 / Math.PI).InsideOf(-40, 40);

            return steer;
        }

        private bool NotifyAlert(Point point)
        {
            if (Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2)) < 1500)
            {
                return true;
            }
            return false;
        }

    }
}
