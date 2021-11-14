using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace t.lib
{
    /// <summary>
    /// Socket Wrapper
    /// </summary>
    internal class Socket : ISocket
    {
        private readonly System.Net.Sockets.Socket _socket;
        internal Socket(System.Net.Sockets.Socket socket)
        {
            _socket = socket;
        }
        public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _socket = new System.Net.Sockets.Socket(addressFamily, socketType, protocolType);
        }
        /// <inheritdoc />
        public EndPoint? RemoteEndPoint => _socket.RemoteEndPoint;
        /// <inheritdoc />
        public async Task<Socket> AcceptAsync() => (Socket)await _socket.AcceptAsync();
        /// <inheritdoc />
        public void Connect(EndPoint remoteEP) => _socket.Connect(remoteEP);
        /// <inheritdoc />
        public void Dispose() => _socket.Dispose();
        /// <inheritdoc />
        public int Receive(byte[] buffer) => _socket.Receive(buffer);

        public Task<int> ReceiveAsync(byte[] buffers, SocketFlags socketFlags) => _socket.ReceiveAsync(buffers, socketFlags);

        /// <inheritdoc />
        public int Send(byte[] buffer) => _socket.Send(buffer);
        /// <inheritdoc />
        public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags) => _socket.SendAsync(buffer, socketFlags);
        /// <inheritdoc />
        public void Shutdown(SocketShutdown how) => _socket.Shutdown(how);
        /// <inheritdoc />
        public void Close() => _socket.Close();

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state)
            => _socket.BeginReceive(buffer, offset, size, socketFlags, callback, state);

        public void Bind(EndPoint localEP) => _socket.Bind(localEP);

        public void Listen(int backlog) => _socket.Listen(backlog);

        public IAsyncResult BeginAccept(AsyncCallback? callback, object? state) => _socket.BeginAccept(callback, state);

        public ISocket EndAccept(IAsyncResult asyncResult) => (Socket)_socket.EndAccept(asyncResult);

        public int EndReceive(IAsyncResult asyncResult) => _socket.EndReceive(asyncResult);

        public IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state)
            => _socket.BeginSend(buffer, offset, size, socketFlags, callback, state);

        public int EndSend(IAsyncResult asyncResult) => _socket.EndSend(asyncResult);

        public static implicit operator System.Net.Sockets.Socket(Socket d) => d._socket;
        public static explicit operator Socket(System.Net.Sockets.Socket b) => new Socket(b);
    }
}
