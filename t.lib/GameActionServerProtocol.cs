using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace t.lib
{
    public class GameActionServerProtocol : GameActionBaseProtocol, IProtocol<GameActionProtocol>
    {

        public Task<GameActionProtocol> ByteToProtocolTypeAsync(Span<byte> buffer, int receivedBytes)
        {
            return Task.FromResult(buffer.Slice(0, receivedBytes).ToArray().ToGameActionProtocol(receivedBytes));
        }

        public GameActionProtocol DefaultFactory()
        {
            return new GameActionProtocol() { Payload = new byte[0], PayloadSize = 0 };
        }

        public Task<GameActionProtocol> GenerateMessageAsync(GameActionProtocol protocolTypeReceived, GameActionProtocol lastProtocolTypeReceived, object? obj)
        {
            return Task.FromResult(protocolTypeReceived);
        }

        public Task OnMessageReceivedAsync(GameActionProtocol gameActionProtocol, GameActionProtocol lastProtocolTypeReceived, object? obj)
        {
            return Task.CompletedTask;
        }

        public Task<byte[]> ProtocolToByteArrayAsync(GameActionProtocol protocolType)
        {
            return Task.FromResult(ToByteArray(protocolType));
        }
    }
}
