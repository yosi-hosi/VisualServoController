using System;

namespace VisualServoCore.Communication
{
    public class DummyCommunication : ICommunication<string>
    {

        // ------ Fields ------ //

        private byte _count;


        // ------ Constructors ------ //

        public DummyCommunication()
        {

        }


        // ------ Methods ------ //

        public bool Send(string sendmsg)
        {
            Console.Write("-->");
            Console.WriteLine(sendmsg);
            return true;
        }

        public string Receive()
        {
            if (_count > 255) _count = 0;
            return $"{_count++}";
        }

        public void Dispose()
        {

        }

    }
}
