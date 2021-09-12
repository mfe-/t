using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace t.lib.Server
{
    public class GameSocketServer : GameBase, IHostedService
    {

        public GameSocketServer(int serverPort, ILogger logger) : base(logger)
        {
            ServerPort = serverPort;
            _game.NewPlayerRegisteredEvent += Game_NewPlayerRegisteredEvent;
            _guid = Guid.NewGuid();
            ActionDictionary.Add(Constants.RegisterPlayer, OnPlayerRegister);
        }
        public GameSocketServer(int serverPort, ILogger logger, Guid guid)
            : this(serverPort, logger)
        {
            _guid = guid;
        }
        protected virtual void OnPlayerRegister(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.RegisterPlayer) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(Constants.RegisterPlayer)}");

            string playername = Encoding.ASCII.GetString(gameActionProtocol.Payload);

            Player player = new Player(playername, gameActionProtocol.PlayerId);
            _game.RegisterPlayer(player);
        }
        private void Game_NewPlayerRegisteredEvent(object? sender, EventArgs<Player> e)
        {
            //broadcast the new player to all other players
            GameActionProtocol gameActionProtocol = GameActionProtocolFactory(Constants.NewPlayer, e.Data);
            BroadcastMessage(gameActionProtocol);
        }
        public int ServerPort { get; }

        private async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the
            // host running the application.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            await Task.Factory.StartNew(() => Listening(localEndPoint, listener),
                cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }
        private CancellationTokenSource? cancellationTokenSource;
        private void Listening(IPEndPoint localEndPoint, Socket listener)
        {
            _logger.LogInformation($"{nameof(Listening)} on {ServerPort}", ServerPort);
            // Data buffer for incoming data.  
            byte[] bytes = new byte[1024];
            // Bind the socket to the local endpoint and
            // listen for incoming connections.  
            using (cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    _game.NewGame();

                    // Start listening for connections.  
                    while (true)
                    {
                        _logger.LogInformation("Waiting for a connection...");
                        // Program is suspended while waiting for an incoming connection.  
                        Socket handler = listener.Accept();
                        // An incoming connection needs to be processed.  
                        while (true)
                        {
                            //check if a cancellation was requested
                            if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                                break;
                            int bytesRec = handler.Receive(bytes);
                            _logger.LogTrace("received : {0}", bytesRec);
                            GameActionProtocol gameActionProtocol = bytes.ToGameActionProtocol(bytesRec);
                            OnMessageReceive(gameActionProtocol);

                        }

                        // Show the data on the console.  


                        // Echo the data back to the client.  


                        //handler.Send(msg);
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "exception while listing");
                }
            }
            cancellationTokenSource = null;
        }
        private void StopListening()
        {
            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {

            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return StartListeningAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopListening();
            return Task.CompletedTask;
        }

        protected override void BroadcastMessage(GameActionProtocol gameActionProtocol)
        {
            
        }
    }
}