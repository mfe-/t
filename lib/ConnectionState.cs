using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace t.lib.Server
{
    [DebuggerDisplay("Ip={SocketClient.LocalEndPoint} Id={Player.PlayerId} Player={Player.Name} ")]
    public sealed class ConnectionState
    {
        public ConnectionState(Socket socket)
        {
            SocketClient = socket;
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
        internal Socket SocketClient;
        internal Player? Player;
        public DateTime LastAction { get; private set; }
}
}