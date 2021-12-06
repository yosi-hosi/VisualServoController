using System;
using System.Threading.Tasks;
using OpenCvSharp;
using System.Diagnostics;

namespace VisualServoCore.Controller
{
    class AngleManager
    {
        private int _rotation;//予想される実際の回転角度
        private enum Direction { Center, Left, Right }
        private Direction _currentCameraDirection;
        private double _targetRotation;//指示する回転角度

        public EventHandler<int> RightFired { get; set; }//get:その値を使えるか、読み取り専用
        public EventHandler<int> LeftFired { get; set; }

        public Point ArrangeCoordinate(Point point)//カメラから見た座標
        {
            var degree = Math.Atan2(point.X, point.Y) * 180 / Math.PI;//カメラから見た角度
            degree += _rotation;//イーブイから見た角度（回転し始めたときから）
            var target = point;
            var thresh_hold = 30;

            if (degree > thresh_hold + _targetRotation && _currentCameraDirection is not Direction.Right)
            {
                if (_currentCameraDirection is Direction.Center)
                    _currentCameraDirection = Direction.Right;
                else if (_currentCameraDirection is Direction.Left)
                    _currentCameraDirection = Direction.Center;

                UpdateRotationAsync(thresh_hold, 1);

                _targetRotation += thresh_hold;
                degree -= thresh_hold;
                RightFired.Invoke(null, thresh_hold);
                Debug.WriteLine(_currentCameraDirection);
            }
            else if (degree < -thresh_hold + _targetRotation && _currentCameraDirection is not Direction.Left)
            {
                if (_currentCameraDirection is Direction.Center)
                    _currentCameraDirection = Direction.Left;
                else if (_currentCameraDirection is Direction.Right)
                    _currentCameraDirection = Direction.Center;

                UpdateRotationAsync(-thresh_hold, 1);

                _targetRotation -= thresh_hold;
                degree += thresh_hold;
                LeftFired.Invoke(null, -thresh_hold);
                Debug.WriteLine(_currentCameraDirection);
            }

            var distance = Math.Sqrt(point.X * point.X + point.Y * point.Y);
            target.Y = (int)(distance * Math.Cos(degree * Math.PI / 180));
            target.X = (int)(distance * Math.Sin(degree * Math.PI / 180));

            return target;//ビークルから見た座標
        }

        private async Task UpdateRotationAsync(int appendAngleDegree, double reachingTimeSecond)//rotationを滑らかに変化させる
        {
            var iterationCount = 10;//10分の1の時間と角度でrotationを計算する
            var angleStep = appendAngleDegree / iterationCount;
            var timeStep = (int)(reachingTimeSecond * 1000 / iterationCount);
            for (int i = 0; i < iterationCount; i++)
            {
                _rotation += angleStep;
                await Task.Delay(timeStep);//for文をゆっくり回す
            }
        }
    }
}
