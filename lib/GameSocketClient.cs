using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace t.lib
{
    public class GameSocketClient : GameBase, IDisposable
    {
        private readonly IPAddress serverIpAdress;
        private readonly int serverPort;
        private Socket? sender;
        private string? PlayerName;

        public GameSocketClient(IPAddress serverIpAdress, int serverPort, ILogger logger) : base(logger)
        {
            this.serverIpAdress = serverIpAdress;
            this.serverPort = serverPort;
        }
        public GameSocketClient(IPAddress serverIpAdress, int serverPort, ILogger logger, string playerName)
            : this(serverIpAdress, serverPort, logger)
        {
            PlayerName = playerName;
        }

        // The port number for the remote device.  
        public async Task JoinGameAsync(string name)
        {
            _guid = Guid.NewGuid();
            _game.NewGame();
            // Data buffer for incoming data.  
            byte[] bytes = new byte[1024];

            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // This example uses port 11000 on the local computer.  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                //IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(serverIpAdress, serverPort);

                // Create a TCP/IP  socket.  
                sender = new Socket(serverIpAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    sender.Connect(remoteEP);

                    _logger.LogInformation("Socket connected to {0}", sender.RemoteEndPoint?.ToString() ?? $"Could not determine {nameof(sender.RemoteEndPoint)}");

                    GameActionProtocol gameActionProtocol = GameActionProtocolFactory(Constants.RegisterPlayer, new Player(name, _guid));
                    // Encode the data string into a byte array.  
                    byte[] msg = gameActionProtocol.ToByteArray();
                    // Send the data through the socket.  
                    _logger.LogInformation("PlayerId {gamePlayerId} generated and sending {bytesSent}", gameActionProtocol.PlayerId, msg.Length);
                    int bytesSent = await sender.SendAsync(new ArraySegment<byte>(msg), SocketFlags.None);
                    _logger.LogTrace("Sent {0} bytes to server.", bytesSent);
                    // Receive the response from the remote device.  
                    int bytesRec = sender.Receive(bytes);
                    _logger.LogTrace("Received {0} bytes.", bytesRec);
                    GameActionProtocol gameActionProtocolRec = bytes.ToGameActionProtocol();
                    OnMessageReceive(gameActionProtocol);
                    //wait until all players joined
                    while (gameActionProtocolRec.Phase != Constants.StartGame)
                    {
                        //send ok
                        msg = GameActionProtocolFactory(Constants.Ok).ToByteArray();
                        bytesSent = await sender.SendAsync(new ArraySegment<byte>(msg), SocketFlags.None);
                        _logger.LogTrace("Sent {0} bytes to server.", bytesSent);
                        bytes = new byte[1024];
                        bytesRec = sender.Receive(bytes);
                        _logger.LogTrace("Received {0} bytes.", bytesRec);
                        gameActionProtocolRec = bytes.ToGameActionProtocol();
                        OnMessageReceive(gameActionProtocol);
                    }
                    while (gameActionProtocol.Phase != Constants.PlayerWon)
                    {

                    }

                }
                catch (ArgumentNullException ane)
                {
                    _logger.LogCritical(ane, "ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    _logger.LogCritical(se, "Could not connect to {0} SocketException : {1}", serverIpAdress, se.ToString());
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.ToString());
            }
        }

        protected override void BroadcastMessage(GameActionProtocol gameActionProtocol)
        {

        }
        public void ExitGame()
        {
            // Release the socket.  
            sender?.Shutdown(SocketShutdown.Both);
            sender?.Close();
            sender?.Dispose();
            sender = null;
        }
        public void Dispose()
        {
            ExitGame();
        }
    }
}