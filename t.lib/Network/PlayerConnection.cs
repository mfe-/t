using System;
using System.Collections.Generic;
using System.Diagnostics;
using t.lib.Game;
using t.lib.Network;

namespace t.lib.Server
{
    /// <summary>
    /// Stores all connection relevant information associated to one <see cref="t.lib.Game.Player"/>
    /// </summary>
    [DebuggerDisplay("Ip={SocketClient.RemoteEndPoint} Id={Player.PlayerId} Player={Player.Name} ")]
    public sealed class PlayerConnection
    {
        /// <summary>
        /// Initialize a new <see cref="PlayerConnection"/>
        /// </summary>
        /// <param name="socket">The socket which should be used to communicate with the <see cref="t.lib.Game.Player"/></param>
        public PlayerConnection(ISocket socket)
        {
            SocketClient = socket;
            MessageQueue = new Queue<GameActionProtocol>();
        }
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        private byte[] _buffer = new byte[BufferSize];
        /// <summary>
        /// Get or sets the buffer of the connection
        /// </summary>
        public byte[] Buffer
        {
            get { return _buffer; }
            set
            {
                _buffer = value;
                LastModified = DateTime.Now;
            }
        }
        /// <summary>
        /// the socket which is used to communicate
        /// </summary>
        internal ISocket SocketClient;
        /// <summary>
        /// get or sets the player which belongs to this <see cref="PlayerConnection"/>
        /// </summary>
        internal Player? Player;
        public DateTime LastModified { get; private set; }
        /// <summary>
        /// the last recieved payload of the player
        /// </summary>
        public GameActionProtocol LastRecPayload { get; internal set; }
        /// <summary>
        /// the last payload which was sent to the player
        /// </summary>
        public GameActionProtocol LastSendPayload { get; internal set; }
        /// <summary>
        /// Containts the messages which should be sent to the player
        /// </summary>
        internal Queue<GameActionProtocol> MessageQueue { get; set; }
    }
}