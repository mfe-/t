using System;
using System.Diagnostics;
using System.Net.Sockets;
using t.lib.Game;

namespace t.lib.Server
{
    [DebuggerDisplay("Ip={SocketClient.RemoteEndPoint} Id={Player.PlayerId} Player={Player.Name} ")]
    public sealed class ConnectionState
    {
        public ConnectionState(ISocket socket)
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
                LastModified = DateTime.Now;
            }
        }
        internal ISocket SocketClient;
        internal Player? Player;
        public DateTime LastModified { get; private set; }
        public GameActionProtocol LastPayload { get; internal set; }
}
}