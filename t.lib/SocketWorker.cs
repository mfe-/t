using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace t.lib
{
    public class SocketWorker<TProtocol, TProtocolType> where TProtocol : IProtocol<TProtocolType>
    {
        private readonly ILogger _logger;
        protected readonly Queue<TProtocolType> _queue = new Queue<TProtocolType>();
        public event EventHandler<TProtocolType>? MessageReceivedEvent;
        public SocketWorker(ISocket socket, TProtocol protocol, ILogger logger)
        {
            Socket = socket;
            Protocol = protocol;
            Buffer = new byte[BufferSize];
            _logger = logger;
            _protocolTypeReplacement = Protocol.DefaultFactory();
        }

        public ISocket Socket { get; }
        public TProtocol Protocol { get; }

        public int BufferSize { get; set; } = 1024;

        protected byte[] Buffer { get; set; }
        TaskCompletionSource? WaitTask { get; set; }
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                TProtocolType lastGameActionProtocol = Protocol.DefaultFactory();
                while (Socket.Connected)
                {
                    Buffer = new byte[BufferSize];
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    var byteReceive = Socket.Receive(Buffer);
                    _logger.LogTrace("Received {0} bytes.", byteReceive);
                    var protocolReceived = await Protocol.ByteToProtocolTypeAsync(Buffer.AsSpan(), byteReceive);
                    await Protocol.OnMessageReceivedAsync(protocolReceived, lastGameActionProtocol, this);

                    var protocolToSend = await Protocol.GenerateMessageAsync(protocolReceived, lastGameActionProtocol, this);
                    
                    if (WaitTask != null)
                    {
                        await WaitTask.Task;
                    }
                    protocolToSend = await OnBeforeSendingProtocolAsync(protocolToSend, lastGameActionProtocol);

                    var payload = await Protocol.ProtocolToByteArrayAsync(protocolToSend);

                    await Socket.SendAsync(new ArraySegment<byte>(payload), SocketFlags.None);
                    lastGameActionProtocol = protocolReceived;
                }
            }
            catch (SocketException e)
            {
                _logger.LogCritical(nameof(RunAsync), e);
            }
        }
        TProtocolType _protocolTypeReplacement;
        public void OverrideGenerateMessage(TProtocolType protocolTypeReplacement)
        {
            _protocolTypeReplacement = protocolTypeReplacement;
        }
        protected virtual Task<TProtocolType> OnBeforeSendingProtocolAsync(TProtocolType protocolTypeGenerated, TProtocolType lastProtocolTypeFromReceived)
        {
            //return the value from OverrideGenerateMessage()
            var defaultproto = Protocol.DefaultFactory();
            if (defaultproto != null && !defaultproto.Equals(_protocolTypeReplacement) || defaultproto == null)
            {
                return Task.FromResult(_protocolTypeReplacement);
            }
            return Task.FromResult(protocolTypeGenerated);
        }
    }
}
