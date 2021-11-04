using System;
using Husty.OpenCvSharp.DepthCamera;
using OpenCvSharp;

namespace VisualServoCore.Controller
{
    public class DummyDepthFusedController : IController<BgrXyzMat, double>
    {

        // ------ Fields ------ //

        private readonly Random _randomGenerator = new();

        // ------ Constructors ------ //

        public DummyDepthFusedController(double a, double b, double c)
        {

        }


        // ------ Methods ------ //

        public LogObject<double> Run(BgrXyzMat input)
        {
            var steer = (_randomGenerator.NextDouble() - 0.5) * 10;
            var speed = 5 + (_randomGenerator.NextDouble() - 0.5) * 5;
            Console.WriteLine($"Steer: {steer:f1} rad,  Speed: {speed:f1} m/s");
            return new(DateTimeOffset.Now, steer, speed);
        }

        public Mat GetGroundCoordinateResults()
        {
            return new(100, 100, MatType.CV_8U, 0);
        }

    }
}
