using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Husty.OpenCvSharp;
using Husty.OpenCvSharp.DepthCamera;

namespace VisualServoCore.Controller
{
    public class DepthFusedController : IController<BgrXyzMat, short>
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
        private double _dPreSteer;

        // ------ Constructors ------ //

        public DepthFusedController(double gain, int maxWidth, int maxDistance)
        {

            // initialize detector instance and some parameters

            // ここは見なくていいです
            var cfg = "..\\..\\..\\..\\..\\model\\_.cfg";
            var weights = "..\\..\\..\\..\\..\\model\\_.weights";
            var names = "..\\..\\..\\..\\..\\model\\_.names";
            _detector = new(cfg, weights, names, new(512, 288), 0.5f);
            _gain = gain;                       // for steering
            _maxWidth = maxWidth;               // maximum number of X-axis
            _maxDistance = maxDistance;         // maximum number of Z-axis
            _radar = new(maxWidth, maxDistance);

        }


        // ------ Public Methods ------ //

        public LogObject<short> Run(BgrXyzMat input)
        {
            // ここは見なくていいです
            _points = FindBoxes(input).Select(r => GetXZ(input, r)).Where(xz => xz is not null).Select(xz => (Point)xz).ToArray();
            _targetPoint = SelectTarget(input, _points) ?? null;
            if (_targetPoint is Point target)
            {
                _dPreSteer = CalculateSteer(target);
           
            }
            return new(DateTimeOffset.Now, (short)_dPreSteer);
        }

        public Mat GetGroundCoordinateResults()
        {
            return _radar.GetRadar(_locker, _points, _targetPoint);
        }


        // ------ Private Methods ------ //

        private Rect[] FindBoxes(BgrXyzMat input)
        {
            // ここは見なくていいです
            var w = input.BGR.Width;
            var h = input.BGR.Height;
            var results = _detector.Run(input.BGR);
            var boxes = results.Where(r => r.Label is "person")
                .Where(r => r.Probability > 0.5)
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
            // ここは見なくていいです
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

            // ここを埋めてください。
            // 取得した点たち(points)とそれらに含まれる3D情報(input)からターゲットを決めるところです。
            // 距離の閾値としてX方向は_maxWidth, Y方向は_maxDistanceなどのフィールドがユーザー入力で使えます。

            var target = new Point(int.MaxValue, 0);
            foreach(var p in points)
            {
                if (Math.Abs(p.X) < Math.Abs(target.X))
                {
                    target = p;
                }
            }
            return target;
        }

        private double CalculateSteer(Point point)
        {

            // ここを埋めてください
            // ターゲット点を使ってCANに流すステアリング角を返す関数を書くところです。
            // 出力はshort型(整数)で、degreeの10倍だそうです。(例：15度→戻り値は150)
            // _gainというフィールドがユーザー入力で使えるようにしています。

            //var steer = _gain * Math.Atan2(point.X, point.Y) * (180 / Math.PI);
            //var maxSteer = 40;
            //if (steer > maxSteer)
            //{
            //    steer = maxSteer;
            //}
            //else if (steer < -maxSteer)
            //{
            //    steer = -maxSteer;
            //}

            //距離を考慮した場合gain1000
            var steer = _gain * (Math.Atan2(point.X, point.Y) * 180 / Math.PI) / Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2));
            var maxSteer = 40;
            if (steer > maxSteer)
            {
                steer = maxSteer;
            }
            else if (steer < -maxSteer)
            {
                steer = -maxSteer;
            }

            return steer;
        }

    }
}
