using OpenCvSharp;
using static OpenCvSharp.Cv2;

namespace VisualServoCore
{
    public class Radar
    {

        // ------ Fields ------ //

        private readonly Scalar _red = new(0, 0, 255);
        private readonly Scalar _green = new(0, 180, 0);
        private readonly Scalar _blue = new(255, 0, 0);
        private readonly Mat _radarCanvas;
        private readonly int _maxWidth;
        private readonly int _maxDistance;


        // ------ Constructors ------ //

        public Radar(int maxWidth, int maxDistance)
        {
            _maxWidth = maxWidth;
            _maxDistance = maxDistance;
            _radarCanvas = new Mat(maxDistance, maxWidth * 2, MatType.CV_8UC3, 0);
            var w2 = _radarCanvas.Width / 2;
            var h = _radarCanvas.Height;
            Circle(_radarCanvas, new Point(w2, h), 1000, _blue, 14);
            Circle(_radarCanvas, new Point(w2, h), 2000, _blue, 14);
            Circle(_radarCanvas, new Point(w2, h), 3000, _blue, 14);
            Circle(_radarCanvas, new Point(w2, h), 4000, _blue, 14);
            Circle(_radarCanvas, new Point(w2, h), 5000, _blue, 14);
            Circle(_radarCanvas, new Point(w2, h), 6000, _blue, 14);
            Circle(_radarCanvas, new Point(w2, h), 7000, _blue, 14);
        }


        // ------ Methods ------ //

        internal Mat GetRadar(object locker, Point[] points, Point? targetPoint = null)
        {
            var radar = _radarCanvas.Clone();
            lock (locker)
            {
                foreach (var p in points)
                    Circle(radar, GetRadarCoordinate(p), 140, _red, FILLED);
                if (targetPoint is Point target)
                    Line(radar, new Point(radar.Width / 2, radar.Height), GetRadarCoordinate(target), new(180, 180, 180), 14);
            }
            return radar;
        }

        private Point GetRadarCoordinate(Point p) =>
            new Point(p.X + _maxWidth, _maxDistance - p.Y);

    }
}
