using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.lib.Game;

namespace t.lib
{
    public class SocketWorkerServer : SocketWorker<GameActionServerProtocol, GameActionProtocol>
    {
        public Player? Player { get; set; }
        public SocketWorkerServer(ISocket socket, GameActionServerProtocol protocol, ILogger logger) : base(socket, protocol, logger)
        {

        }
    }
}
