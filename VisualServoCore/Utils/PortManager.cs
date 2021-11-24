//namespace VisualServoCore
//{
//    public struct Baudrate
//    {
//        public const int BAUD_110        = 110;
//        public const int BAUD_300        = 300;
//        public const int BAUD_600        = 600;
//        public const int BAUD_1200       = 1200;
//        public const int BAUD_2400       = 2400;
//        public const int BAUD_4800       = 4800;
//        public const int BAUD_9600       = 9600;
//        public const int BAUD_14400      = 14400;
//        public const int BAUD_19200      = 19200;
//        public const int BAUD_38400      = 38400;
//        public const int BAUD_57600      = 57600;
//        public const int BAUD_115200     = 115200;
//        public const int BAUD_230400     = 230400;
//        public const int BAUD_460800     = 460800;
//        public const int BAUD_921600     = 921600;
//    }

//    public static class PortManager
//    {

//        // ------ public methods ------ //

//        public static string SearchPort(int baudrate, string[] keyPetterns)
//        {
//            var activePort = "";
//            foreach (var p in SerialPort.GetPortNames())
//            {
//                try
//                {
//                    var port = new SerialPort(p, baudrate, 300);
//                    var exists = false;
//                    var count = 0;
//                    while (true)
//                    {
//                        var line = port.ReadLine();
//                        foreach (var key in keyPetterns)
//                        {
//                            if (line.Contains(key))
//                            {
//                                exists = true;
//                                break;
//                            }
//                        }
//                        if (count++ is 10) break;
//                    }
//                    port.Dispose();
//                    if (exists)
//                    {
//                        activePort = p;
//                        break;
//                    }
//                }
//                catch { }
//            }
//            return activePort;
//        }

//    }
//}
