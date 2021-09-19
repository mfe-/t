﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace t.lib
{
    public class GameSocketClient : GameBase, IDisposable
    {
        private readonly IPAddress serverIpAdress;
        private readonly int serverPort;
        private readonly ILogger logger;
        private Socket? sender;
        private string? PlayerName;

        public GameSocketClient(IPAddress serverIpAdress, int serverPort, ILogger logger) : base(logger)
        {
            this.serverIpAdress = serverIpAdress;
            this.serverPort = serverPort;
            this.logger = logger;
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

                    logger.LogInformation("Socket connected to {0}", sender.RemoteEndPoint?.ToString() ?? $"Could not determine {nameof(sender.RemoteEndPoint)}");

                    GameActionProtocol gameActionProtocol = GameActionProtocolFactory(Constants.RegisterPlayer, new Player(name, _guid));
                    // Encode the data string into a byte array.  
                    byte[] msg = gameActionProtocol.ToByteArray();
                    // Send the data through the socket.  
                    logger.LogInformation("PlayerId {gamePlayerId} generated and sending {bytesSent}", gameActionProtocol.PlayerId, msg.Length);
                    int bytesSent = await sender.SendAsync(new ArraySegment<byte>(msg), SocketFlags.None);
                    // Receive the response from the remote device.  
                    int bytesRec = sender.Receive(bytes);
                    GameActionProtocol gameActionProtocolRec = bytes.ToGameActionProtocol();

                    OnMessageReceive(gameActionProtocol);

                }
                catch (ArgumentNullException ane)
                {
                    logger.LogCritical(ane, "ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    logger.LogCritical(se, "SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    logger.LogCritical(e, "Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                logger.LogCritical(e, e.ToString());
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