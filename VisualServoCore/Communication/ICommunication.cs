using System.Collections.Generic;

namespace VisualServoCore.Communication
{
    public interface ICommunication<T>
    {

        public bool Send(T sendmsg);

        public T Receive();

        public void Dispose();

    }
}
