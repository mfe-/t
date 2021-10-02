using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace t.lib
{
    public interface ISocket : IDisposable
    {
        /// <summary>
        /// Summary:
        ///    Gets a value that indicates whether a System.Net.Sockets.Socket is connected
        ///    to a remote host as of the last Overload:System.Net.Sockets.Socket.Send or Overload:System.Net.Sockets.Socket.Receive
        ///    operation.
        ///
        /// Returns:
        ///    true if the System.Net.Sockets.Socket was connected to a remote resource as of
        ///    the most recent operation; otherwise, false.
        /// </summary>
        public bool Connected { get; }
        //
        // Summary:
        //     Ends a pending asynchronous send.
        //
        // Parameters:
        //   asyncResult:
        //     An System.IAsyncResult that stores state information for this asynchronous operation.
        //
        // Returns:
        //     If successful, the number of bytes sent to the System.Net.Sockets.Socket; otherwise,
        //     an invalid System.Net.Sockets.Socket error.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     asyncResult is null.
        //
        //   T:System.ArgumentException:
        //     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginSend(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
        //     method.
        //
        //   T:System.InvalidOperationException:
        //     System.Net.Sockets.Socket.EndSend(System.IAsyncResult) was previously called
        //     for the asynchronous send.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        public int EndSend(IAsyncResult asyncResult);

        //
        // Summary:
        //     Sends data asynchronously to a connected System.Net.Sockets.Socket.
        //
        // Parameters:
        //   buffer:
        //     An array of type System.Byte that contains the data to send.
        //
        //   offset:
        //     The zero-based position in the buffer parameter at which to begin sending data.
        //
        //   size:
        //     The number of bytes to send.
        //
        //   socketFlags:
        //     A bitwise combination of the System.Net.Sockets.SocketFlags values.
        //
        //   callback:
        //     The System.AsyncCallback delegate.
        //
        //   state:
        //     An object that contains state information for this request.
        //
        // Returns:
        //     An System.IAsyncResult that references the asynchronous send.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket. See remarks section below.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     offset is less than 0. -or- offset is less than the length of buffer. -or- size
        //     is less than 0. -or- size is greater than the length of buffer minus the value
        //     of the offset parameter.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        public IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state);

        //
        // Summary:
        //     Ends a pending asynchronous read.
        //
        // Parameters:
        //   asyncResult:
        //     An System.IAsyncResult that stores state information and any user defined data
        //     for this asynchronous operation.
        //
        // Returns:
        //     The number of bytes received.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     asyncResult is null.
        //
        //   T:System.ArgumentException:
        //     asyncResult was not returned by a call to the System.Net.Sockets.Socket.BeginReceive(System.Byte[],System.Int32,System.Int32,System.Net.Sockets.SocketFlags,System.AsyncCallback,System.Object)
        //     method.
        //
        //   T:System.InvalidOperationException:
        //     System.Net.Sockets.Socket.EndReceive(System.IAsyncResult) was previously called
        //     for the asynchronous read.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        public int EndReceive(IAsyncResult asyncResult);

        //
        // Summary:
        //     Begins to asynchronously receive data from a connected System.Net.Sockets.Socket.
        //
        // Parameters:
        //   buffer:
        //     An array of type System.Byte that is the storage location for the received data.
        //
        //   offset:
        //     The zero-based position in the buffer parameter at which to store the received
        //     data.
        //
        //   size:
        //     The number of bytes to receive.
        //
        //   socketFlags:
        //     A bitwise combination of the System.Net.Sockets.SocketFlags values.
        //
        //   callback:
        //     An System.AsyncCallback delegate that references the method to invoke when the
        //     operation is complete.
        //
        //   state:
        //     A user-defined object that contains information about the receive operation.
        //     This object is passed to the System.Net.Sockets.Socket.EndReceive(System.IAsyncResult)
        //     delegate when the operation is complete.
        //
        // Returns:
        //     An System.IAsyncResult that references the asynchronous read.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     System.Net.Sockets.Socket has been closed.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     offset is less than 0. -or- offset is greater than the length of buffer. -or-
        //     size is less than 0. -or- size is greater than the length of buffer minus the
        //     value of the offset parameter.
        public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state);

        //
        // Summary:
        //     Associates a System.Net.Sockets.Socket with a local endpoint.
        //
        // Parameters:
        //   localEP:
        //     The local System.Net.EndPoint to associate with the System.Net.Sockets.Socket.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     localEP is null.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        //
        //   T:System.Security.SecurityException:
        //     A caller higher in the call stack does not have permission for the requested
        //     operation.
        public void Bind(EndPoint localEP);
        //
        // Summary:
        //     Places a System.Net.Sockets.Socket in a listening state.
        //
        // Parameters:
        //   backlog:
        //     The maximum length of the pending connections queue.
        //
        // Exceptions:
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        public void Listen(int backlog);
        //
        // Summary:
        //     Begins an asynchronous operation to accept an incoming connection attempt.
        //
        // Parameters:
        //   callback:
        //     The System.AsyncCallback delegate.
        //
        //   state:
        //     An object that contains state information for this request.
        //
        // Returns:
        //     An System.IAsyncResult that references the asynchronous System.Net.Sockets.Socket
        //     creation.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket object has been closed.
        //
        //   T:System.NotSupportedException:
        //     Windows NT is required for this method.
        //
        //   T:System.InvalidOperationException:
        //     The accepting socket is not listening for connections. You must call System.Net.Sockets.Socket.Bind(System.Net.EndPoint)
        //     and System.Net.Sockets.Socket.Listen(System.Int32) before calling System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).
        //     -or- The accepted socket is bound.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     receiveSize is less than 0.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        public IAsyncResult BeginAccept(AsyncCallback? callback, object? state);
        //
        // Summary:
        //     Asynchronously accepts an incoming connection attempt and creates a new System.Net.Sockets.Socket
        //     to handle remote host communication.
        //
        // Parameters:
        //   asyncResult:
        //     An System.IAsyncResult that stores state information for this asynchronous operation
        //     as well as any user defined data.
        //
        // Returns:
        //     A System.Net.Sockets.Socket to handle communication with the remote host.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     asyncResult is null.
        //
        //   T:System.ArgumentException:
        //     asyncResult was not created by a call to System.Net.Sockets.Socket.BeginAccept(System.AsyncCallback,System.Object).
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket. See the Remarks section
        //     for more information.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        //
        //   T:System.InvalidOperationException:
        //     System.Net.Sockets.Socket.EndAccept(System.IAsyncResult) method was previously
        //     called.
        //
        //   T:System.NotSupportedException:
        //     Windows NT is required for this method.
        public ISocket EndAccept(IAsyncResult asyncResult);

        /// <summary>
        /// Sends data to a connected System.Net.Sockets.Socket using the specified System.Net.Sockets.SocketFlags.
        /// </summary>
        /// <param name="buffer">A span of bytes that contains the data to be sent.</param>
        /// <param name="socketFlags">A bitwise combination of the enumeration values that specifies send and receive behaviors.</param>
        /// <returns>The number of bytes sent to the System.Net.Sockets.Socket.</returns>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="ObjectDisposedException">The System.Net.Sockets.Socket has been closed.</exception>
        Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags);

        //
        // Summary:
        //     Sends data to a connected System.Net.Sockets.Socket.
        //
        // Parameters:
        //   buffer:
        //     An array of type System.Byte that contains the data to be sent.
        //
        // Returns:
        //     The number of bytes sent to the System.Net.Sockets.Socket.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        public int Send(byte[] buffer);

        //
        // Summary:
        //     Receives data from a bound System.Net.Sockets.Socket into a receive buffer.
        //
        // Parameters:
        //   buffer:
        //     An array of type System.Byte that is the storage location for the received data.
        //
        // Returns:
        //     The number of bytes received.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     buffer is null.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        //
        //   T:System.Security.SecurityException:
        //     A caller in the call stack does not have the required permissions.
        public int Receive(byte[] buffer);
        //
        // Summary:
        //     Establishes a connection to a remote host.
        //
        // Parameters:
        //   remoteEP:
        //     An System.Net.EndPoint that represents the remote device.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     remoteEP is null.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        //
        //   T:System.Security.SecurityException:
        //     A caller higher in the call stack does not have permission for the requested
        //     operation.
        //
        //   T:System.InvalidOperationException:
        //     The System.Net.Sockets.Socket has been placed in a listening state by calling
        //     System.Net.Sockets.Socket.Listen(System.Int32).
        void Connect(EndPoint remoteEP);

        //
        // Summary:
        //     Gets the remote endpoint.
        //
        // Returns:
        //     The System.Net.EndPoint with which the System.Net.Sockets.Socket is communicating.
        //
        // Exceptions:
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        EndPoint? RemoteEndPoint { get; }
        //
        // Summary:
        //     Disables sends and receives on a System.Net.Sockets.Socket.
        //
        // Parameters:
        //   how:
        //     One of the System.Net.Sockets.SocketShutdown values that specifies the operation
        //     that will no longer be allowed.
        //
        // Exceptions:
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when attempting to access the socket.
        //
        //   T:System.ObjectDisposedException:
        //     The System.Net.Sockets.Socket has been closed.
        void Shutdown(SocketShutdown how);
        //
        // Summary:
        //     Closes the System.Net.Sockets.Socket connection and releases all associated resources.
        void Close();

    }
}
