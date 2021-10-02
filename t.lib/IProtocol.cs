using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib
{
    public interface IProtocol<TProtocolType> : IProtocol<TProtocolType, object?>
    {
    }
    public interface IProtocol<TProtocolType, TContext>
    {
        Task<TProtocolType> ByteToProtocolTypeAsync(Span<byte> buffer, int receivedBytes);
        Task OnMessageReceivedAsync(TProtocolType protocolTypeReceived, TProtocolType lastProtocolType, TContext? obj);
        Task<TProtocolType> GenerateMessageAsync(TProtocolType protocolTypeReceived, TProtocolType lastProtocolType, TContext? obj);
        Task<byte[]> ProtocolToByteArrayAsync(TProtocolType protocolType);
        TProtocolType DefaultFactory();

    }
}
