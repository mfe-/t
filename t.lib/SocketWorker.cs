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
        public readonly Queue<TProtocolType> _historyQueue = new Queue<TProtocolType>();
        public SocketWorker(ISocket socket, TProtocol protocol, ILogger logger)
        {
            Socket = socket;
            Protocol = protocol;
            Buffer = new byte[BufferSize];
            _logger = logger;
        }

        public ISocket Socket { get; }
        public TProtocol Protocol { get; }

        public int BufferSize { get; set; } = 1024;

        protected byte[] Buffer { get; set; }
        TaskCompletionSource? WaitBeforeSendingTask { get; set; }
        public Task? RunAsyncTask { get; set; }
        public async Task RunAsync(CancellationToken? cancellationToken = null)
        {
            try
            {
                _logger.LogTrace("ManagedThreadId {ManagedThreadId}", Thread.CurrentThread.ManagedThreadId);
                TProtocolType lastGameActionProtocolReceived = Protocol.DefaultFactory();
                TProtocolType lastGameActionProtocolSent = Protocol.DefaultFactory();

                while (Socket.Connected)
                {
                    Buffer = new byte[BufferSize];
                    if (cancellationToken != null && cancellationToken.Value.IsCancellationRequested)
                    {
                        break;
                    }
                    var byteReceive = Socket.Receive(Buffer);
                    _logger.LogTrace("Received {0} bytes.", byteReceive);
                    var protocolReceived = await Protocol.ByteToProtocolTypeAsync(Buffer.AsSpan(), byteReceive);
                    //wait
                    await Protocol.OnMessageReceivedAsync(protocolReceived, lastGameActionProtocolReceived, lastGameActionProtocolSent, this);

                    var protocolToSend = await Protocol.GenerateMessageAsync(protocolReceived, lastGameActionProtocolReceived, this);
                    protocolToSend = await OverrideGeneratedMessageAsync(protocolToSend);
                    var payload = await Protocol.ProtocolToByteArrayAsync(protocolToSend);

                    await Socket.SendAsync(new ArraySegment<byte>(payload), SocketFlags.None);
                    _historyQueue.Enqueue(protocolToSend);
                    lastGameActionProtocolReceived = protocolReceived;
                    lastGameActionProtocolSent = protocolToSend;
                }
            }
            catch (SocketException e)
            {
                _logger.LogCritical(nameof(RunAsync), e);
            }
        }
        private readonly Stack<TProtocolType> protocolTypeStack = new Stack<TProtocolType>();
        public Task<TProtocolType> OverrideGeneratedMessageAsync(TProtocolType protocolToSend)
        {
            if (protocolTypeStack.Count == 0)
            {
                return Task.FromResult(protocolToSend);
            }
            var protocolReplacement = protocolTypeStack.Pop();
            return Task.FromResult(protocolReplacement);
        }
        public void SetOverrideMessage(IEnumerable<TProtocolType> protocolTypes)
        {
            foreach (var item in protocolTypes)
            {
                protocolTypeStack.Push(item);
            }
        }

    }
}
