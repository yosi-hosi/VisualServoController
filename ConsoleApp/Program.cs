using System;
using System.Linq;
using System.Collections.Generic;
using OpenCvSharp;
using Husty.OpenCvSharp.DepthCamera;
using VisualServoCore;
using VisualServoCore.Vision;
using VisualServoCore.Controller;
using VisualServoCore.Communication;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var gain = 1.0;
            var maxWidth = 3000;
            var maxDistance = 8000;

            IDepthCamera camera = new Realsense(new(640, 360));                                                 // カメラデバイス

            IVision<BgrXyzMat> cap = new BGRXYZStream(camera);                                                  // カメラからの映像を流すやつ
            IController<BgrXyzMat, short> controller = new DepthFusedController(gain, maxWidth, maxDistance);   // 制御器本体
            ICommunication<short> server = new DummyCommunication();                                            // 外部と通信するやつ
            DataLogger<short> log = null;
            log = new(new(640, 360));                                                                                        // 記録が不要ならコメントアウト

            var connector = cap.Connect()
                .Subscribe(frame =>
                {
                    var results = controller.Run(frame);
                    server.Send(results.Steer);
                    Cv2.ImShow(" ", frame.BGR);
                    Cv2.WaitKey(1);
                    log?.Write(results);
                    log?.Write(frame);
                });

            while (Console.ReadKey().Key is not ConsoleKey.Enter) ;                         // Enterキーを押すと終了
            connector.Dispose();
            cap.Disconnect();
            server.Dispose();
            log?.Dispose();

        }
    }
}
