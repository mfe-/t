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
            ActionDictionary.Add(Constants.NewPlayer, OnNewPlayer);
        }

        private void OnNewPlayer(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.NewPlayer) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(Constants.RegisterPlayer)}");

            Player player = GetPlayer(gameActionProtocol);
            if (!_game.Players.Any(a => a.PlayerId == player.PlayerId))
            {
                if (!_game.Players.Any())
                {
                    //start game before register new players
                    int requiredPlayers = GetNumber(gameActionProtocol);
                    _game.NewGame(requiredPlayers);
                }

                _logger.LogInformation($"Adding {(_guid != player.PlayerId ? "new" : "")} PlayerId {{PlayerId)}} {{Name}}", player.PlayerId, player.Name);
                _game.RegisterPlayer(player);
            }
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
                    byte[] sendPayLoad = gameActionProtocol.ToByteArray();
                    // Send the data through the socket.  
                    _logger.LogInformation("PlayerId {gamePlayerId} for {playerName} generated", gameActionProtocol.PlayerId, GetPlayer(gameActionProtocol).Name);
                    int bytesSent = await sender.SendAsync(new ArraySegment<byte>(sendPayLoad), SocketFlags.None);
                    _logger.LogTrace("Sent {0} bytes to server.", bytesSent);
                    _logger.LogInformation("Waiting for remaining players to join");
                    //wait until all players joined
                    GameActionProtocol gameActionProtocolRec = new GameActionProtocol();
                    gameActionProtocolRec.Phase = Constants.Ok;
                    while (gameActionProtocolRec.Phase != Constants.StartGame)
                    {
                        // Receive the response from the remote device.  
                        bytes = new byte[1024];
                        int bytesRec = sender.Receive(bytes);
                        _logger.LogTrace("Received {0} bytes.", bytesRec);
                        gameActionProtocolRec = bytes.AsSpan().Slice(0, bytesRec).ToArray().ToGameActionProtocol(bytesRec);
                        OnMessageReceive(gameActionProtocolRec);
                        //send ok
                        sendPayLoad = GameActionProtocolFactory(Constants.Ok).ToByteArray();
                        bytesSent = await sender.SendAsync(new ArraySegment<byte>(sendPayLoad), SocketFlags.None);
                        _logger.LogTrace("Sent {0} bytes to server.", bytesSent);
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
                    if (se.ErrorCode == 10054)
                    {
                        _logger.LogCritical(se, "Lost connection to {0} SocketException : {1}", serverIpAdress, se.ToString());
                    }
                    else
                    {
                        _logger.LogCritical(se, "Could not connect to {0} SocketException : {1}", serverIpAdress, se.ToString());
                    }
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