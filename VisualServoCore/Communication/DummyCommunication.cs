using System;
using System.Collections.Generic;

namespace VisualServoCore.Communication
{
    public class DummyCommunication : ICommunication<short>
    {

        // ------ Fields ------ //

        private byte _count;


        // ------ Constructors ------ //

        public DummyCommunication()
        {

        }


        // ------ Methods ------ //

        public bool Send(short sendmsg)
        {
            Console.Write("-->");
            Console.WriteLine($"{sendmsg}");
            return true;
        }

        public short Receive()
        {
            if (_count > 255) _count = 0;
            return _count++;
        }

        public void Dispose()
        {

        }

    }
}
