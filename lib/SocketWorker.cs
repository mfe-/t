using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib
{
    public class SocketWorker
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        private readonly ILogger _logger;

        // Receive buffer.  
        private byte[] _buffer = new byte[BufferSize];
        protected Dictionary<byte, Func<GameActionProtocol, object?, Task<GameActionProtocol>>> ActionDictionary = new();
        public SocketWorker(ISocket socket, ILogger logger)
        {
            Socket = socket;
            this._logger = logger;
        }
        public async Task RunAsync()
        {
            GameActionProtocol gameActionProtocolReceive = new GameActionProtocol();
            while (gameActionProtocolReceive.Phase != Constants.PlayerWon)
            {
                var bytesRead = Socket.Receive(_buffer);

                gameActionProtocolReceive = _buffer.AsSpan().Slice(0, bytesRead).ToArray().ToGameActionProtocol(bytesRead);

                GameActionProtocol gameActionProtocol = await OnMessageReceiveAsync(gameActionProtocolReceive, null);
                var payLoad = gameActionProtocol.ToByteArray();
                await Socket.SendAsync(new ArraySegment<byte>(payLoad), System.Net.Sockets.SocketFlags.None);
            }

        }
        public ISocket Socket { get; }

        protected virtual async Task<GameActionProtocol> OnMessageReceiveAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            _logger.LogInformation("OnMessageReceive from {player} with Phase {Phase}", gameActionProtocol.PlayerId, Constants.ToString(gameActionProtocol.Phase));
            using (_logger.BeginScope(new Dictionary<string, object> {
                { nameof(gameActionProtocol.Phase), gameActionProtocol.Phase }
               ,{ nameof(gameActionProtocol.PlayerId), gameActionProtocol.PlayerId}
            }))
            {
                if (ActionDictionary.ContainsKey(gameActionProtocol.Phase))
                {
                    return await ActionDictionary[gameActionProtocol.Phase].Invoke(gameActionProtocol, obj);
                }
                else
                {
                    return await ActionDictionary[Constants.ErrorOccoured].Invoke(gameActionProtocol, obj);
                }
            }
        }
    }
}
