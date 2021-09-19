using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace t.lib.Server
{
    public partial class GameSocketServer : GameBase, IHostedService
    {
        private readonly int RequiredAmountOfPlayers;
        private readonly int TotalPoints;
        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, ILogger logger) : base(logger)
        {
            if (appConfig.RequiredAmountOfPlayers < 1) throw new ArgumentException("At least two players are required to play the game!");
            if (appConfig.TotalPoints < 10) throw new ArgumentException("At least ten points are expected!");
            RequiredAmountOfPlayers = appConfig.RequiredAmountOfPlayers;
            TotalPoints = appConfig.TotalPoints;
            ServerPort = serverPort;
            ServerIpAdress = serverIpAdress;
            _game.NewPlayerRegisteredEvent += Game_NewPlayerRegisteredEvent;
            _guid = Guid.NewGuid();
        }
        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, ILogger logger, Guid guid)
            : this(appConfig, serverIpAdress, serverPort, logger)
        {
            _guid = guid;
        }
        private void Game_NewPlayerRegisteredEvent(object? sender, EventArgs<Player> e)
        {
            //broadcast the "new player and all existing players" to all other players
            foreach (var protocol in _game.Players.Select(a => GameActionProtocolFactory(Constants.NewPlayer, a)))
            {
                BroadcastMessage(protocol);
            }
            if (_game.Players.Count == RequiredAmountOfPlayers)
            {
                var gameActionProtocol = GameActionProtocolFactory(Constants.StartGame, number: TotalPoints);
                OnStart(gameActionProtocol);
                BroadcastMessage(gameActionProtocol);
            }
        }
        public int ServerPort { get; }
        public string ServerIpAdress { get; }

        private ConcurrentDictionary<Guid, ConnectionState> _playerConnections = new ConcurrentDictionary<Guid, ConnectionState>();

        private async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the
            // host running the application.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress;
            if (String.IsNullOrEmpty(ServerIpAdress))
            {
                ipAddress = ipHostInfo.AddressList[0];
            }
            else
            {
                ipAddress = IPAddress.Parse(ServerIpAdress);
            }
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            await Task.Factory.StartNew(() => Listening(localEndPoint, listener),
                cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }


        private CancellationTokenSource? cancellationTokenSource;
        // Thread signal.  
        protected readonly ManualResetEvent allDone = new ManualResetEvent(false);
        private void Listening(IPEndPoint localEndPoint, Socket listener)
        {
            _logger.LogInformation($"{nameof(Listening)} on {localEndPoint.Address}:{ServerPort}", localEndPoint.Address, ServerPort);
            _logger.LogInformation("ServerId {ServerId} generated", _guid);
            // Bind the socket to the local endpoint and
            // listen for incoming connections.  
            using (cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    _game.NewGame();

                    _logger.LogInformation("Waiting for a connection...");
                    // Start listening for connections.  
                    while (true)
                    {
                        if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                            break;
                        // Set the event to nonsignaled state.  
                        allDone.Reset();

                        // Start an asynchronous socket to listen for connections.
                        listener.BeginAccept(new AsyncCallback(AcceptClientConnection), listener);

                        // Wait until a connection is made before continuing.  
                        allDone.WaitOne();
                    }

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "exception while listing");
                }
            }
            cancellationTokenSource = null;
        }

        private void AcceptClientConnection(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            if (ar.AsyncState is Socket socket)
            {
                Socket listener = socket;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.  
                ConnectionState state = new ConnectionState(handler);
                handler.BeginReceive(state.Buffer, 0, ConnectionState.BufferSize, 0, new AsyncCallback(ReadResult), state);
            }
        }

        private void ReadResult(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            if (ar.AsyncState is ConnectionState connectionState)
            {
                ConnectionState state = connectionState;
                Socket handler = state.SocketClient;

                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    _logger.LogTrace("Received {0} bytes.", bytesRead);

                    GameActionProtocol gameActionProtocol = state.Buffer.ToGameActionProtocol(bytesRead);
                    if (gameActionProtocol.Phase == Constants.RegisterPlayer && !_playerConnections.ContainsKey(gameActionProtocol.PlayerId))
                    {
                        connectionState.Player = GetPlayer(gameActionProtocol);
                        var addResult = _playerConnections.TryAdd(gameActionProtocol.PlayerId, connectionState);
                        _logger.LogInformation("Client {connectionWithPlayerId} {PlayerName} connected and added to Clientlist={addResult}", gameActionProtocol.PlayerId, connectionState.Player?.Name ?? "", addResult);
                    }
                    OnMessageReceive(gameActionProtocol);
                }
            }
            else
            {
                _logger.LogInformation("Received something unknown");
            }
        }
        protected void Send(Socket handler, GameActionProtocol gameActionProtocol)
        {
            // Convert the data to byte data
            byte[] byteData = gameActionProtocol.ToByteArray();

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                if (ar.AsyncState is Socket handler)
                {
                    var connectionState = _playerConnections.Values.First(a => a.SocketClient == handler);

                    int bytesSent = handler.Receive(connectionState.Buffer);
                    GameActionProtocol gameActionProtocol = connectionState.Buffer.AsSpan().Slice(0, bytesSent).ToArray().ToGameActionProtocol(bytesSent);
                    OnMessageReceive(gameActionProtocol);

                    // Complete sending the data to the remote device.  
                    //int bytesSent = handler.EndSend(ar);
                    //int bytesSent = handler.Send(connectionState.Buffer);
                    //_logger.LogTrace("Sent {0} bytes to client.", bytesSent);

                    //handler.Shutdown(SocketShutdown.Both);
                    //handler.Close();
                }
            }
            catch(SocketException e)
            {
                //System.Net.Sockets.SocketException: 'An existing connection was forcibly closed by the remote host.'
                //client ist tot
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(SendCallback));
            }
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
            foreach (var connectionState in _playerConnections.Values)
            {
                _logger.LogInformation("Broadcasting as {ServerId} to {ip} {PlayerName} Phase={Phase}", gameActionProtocol.PlayerId, connectionState.SocketClient.LocalEndPoint, connectionState.Player?.Name ?? "", gameActionProtocol.Phase);
                Send(connectionState.SocketClient, gameActionProtocol);
            }
        }
    }
}