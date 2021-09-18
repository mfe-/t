using System.Net.Sockets;

namespace t.lib.Server
{
    public sealed class ClientConnectionState
    {
        public ClientConnectionState(Socket socket)
        {
            workSocket = socket;
        }
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  

        private byte[] _buffer = new byte[BufferSize];
        public byte[] Buffer
        {
            get { return _buffer; }
            set
            {
                _buffer = value;
                LastAction = DateTime.Now;
            }
        }
        internal Socket workSocket;
        internal Player? Player;
        public DateTime LastAction { get; private set; }
}
}