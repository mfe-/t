using Microsoft.Extensions.Hosting;
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
    public class GameSocketClient : GameBase, IHostedService
    {
        private readonly IPAddress serverIpAdress;
        private readonly int serverPort;
        private readonly ILogger logger;
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
        private TaskCompletionSource? TaskCompletionSource;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource = new TaskCompletionSource();
            StartClient();
            return TaskCompletionSource.Task;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        public GameActionProtocol JoinGame(string name)
        {
            var gameActionProtocol = GameActionProtocolFactory(Constants.RegisterPlayer);
            gameActionProtocol.Payload = Encoding.ASCII.GetBytes($"{name}{Environment.NewLine}");
            return gameActionProtocol;
        }
        // The port number for the remote device.  
        public void StartClient()
        {
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
                Socket sender = new Socket(serverIpAdress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    sender.Connect(remoteEP);

                    logger.LogInformation("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    System.Console.WriteLine("Enter your name:");
#if DEBUG
                    string? name = PlayerName;
#else
                    string? name = Console.ReadLine();
#endif
                    GameActionProtocol gameActionProtocol = new GameActionProtocol();

                    if (!String.IsNullOrEmpty(name))
                    {
                        gameActionProtocol = JoinGame(name);
                        logger.LogInformation("PlayerId Generated {gamePlayerId}", gameActionProtocol.PlayerId);
                    }

                    // Encode the data string into a byte array.  
                    byte[] msg = gameActionProtocol.ToByteArray();

                    // Send the data through the socket.  
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.  
                    int bytesRec = sender.Receive(bytes);
                    logger.LogInformation("Echoed test = {0}",
                        Encoding.ASCII.GetString(bytes, 0, bytesRec));

                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

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
    }
}