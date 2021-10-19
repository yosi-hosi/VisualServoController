using Husty.IO;

namespace VisualServoCore.Communication
{
    public class SocketServer : ICommunication<string>
    {

        // ------ Fields ------ //

        private readonly TcpSocketServer _server;
        private readonly BidirectionalDataStream _stream;


        // ------ Constructors ------ //

        public SocketServer(int port)
        {
            _server = new(port);
            _stream = _server.GetStream();
        }


        // ------ Methods ------ //

        public bool Send(string sendmsg)
        {
            try
            {
                _stream?.WriteString(sendmsg);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string Receive()
        {
            return _stream?.ReadString();
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _server?.Dispose();
        }

    }
}
