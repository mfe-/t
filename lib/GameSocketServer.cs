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
    public partial class GameSocketServer : GameSocketBase, IHostedService
    {
        private readonly int RequiredAmountOfPlayers;
        private readonly int TotalPoints;
        private readonly Queue<Func<Task>> _EventQueue = new Queue<Func<Task>>();
        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, ILogger logger) : base(logger)
        {
            if (appConfig.RequiredAmountOfPlayers < 1) throw new ArgumentException("At least two players are required to play the game!");
            if (appConfig.TotalPoints < 10) throw new ArgumentException("At least ten points are expected!");
            ActionDictionary.Add(Constants.PlayerReported, OnPlayerReportedAsync);
            RequiredAmountOfPlayers = appConfig.RequiredAmountOfPlayers;
            TotalPoints = appConfig.TotalPoints;
            ServerPort = serverPort;
            ServerIpAdress = serverIpAdress;
            Game.NewPlayerRegisteredEvent += Game_NewPlayerRegisteredEvent;
            Game.NextRoundEvent += Game_NextRoundEvent;
            Game.PlayerWonEvent += Game_EndPlayerWonEvent;
            _guid = Guid.NewGuid();
        }
        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, ILogger logger, Guid guid)
            : this(appConfig, serverIpAdress, serverPort, logger)
        {
            _guid = guid;
        }
        private void Game_NextRoundEvent(object? sender, NextRoundEventArgs e)
        {
            Task GenerateNextRoundBroadcast()
            {
                var gameActionProtocol = GameActionProtocolFactory(Constants.NextRound, nextRoundEventArgs: e);
                return BroadcastMessageAsync(gameActionProtocol, null);
            }
            _EventQueue.Enqueue(GenerateNextRoundBroadcast);
        }
        private void Game_EndPlayerWonEvent(object? sender, EventArgs<IEnumerable<Player>> e)
        {
            Task GenerateGameEndWinner()
            {
                var gameActionProtocol = GameActionProtocolFactory(Constants.PlayerWon, e.Data.First());
                return BroadcastMessageAsync(gameActionProtocol, null);
            }
            _EventQueue.Enqueue(GenerateGameEndWinner);
        }
        protected virtual async Task OnPlayerReportedAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            int pickedNumber = GetNumber(gameActionProtocol);
            Card pickedCard = new Card(pickedNumber);
            var player = Game.Players.First(a => a.PlayerId == gameActionProtocol.PlayerId);
            Game.PlayerReport(player, pickedCard);
            if (!Game.GetRemainingPickCardPlayers().Any())
            {
                //finally tell every player which cards they took
                foreach (var gameAction in Game.gameActions.Where(a => a.Round == Game.Round).ToArray())
                {
                    var gameActionPro = GameActionProtocolFactory(Constants.PlayerScored, player: gameAction.Player, number: gameAction.Offered);
                    await BroadcastMessageAsync(gameActionPro, null);
                }
                Game.NextRound();
            }
        }

        private void Game_NewPlayerRegisteredEvent(object? sender, EventArgs<Player> e)
        {
            //make sure the event is not blocking any processing tcp event
            Task.Factory.StartNew(OnNewPlayerAsync, TaskCreationOptions.DenyChildAttach);
        }
        private async Task OnNewPlayerAsync()
        {
            //broadcast the "new player and all existing players" to all other players
            foreach (var player in Game.Players.ToArray())
            {
                var protocol = GameActionProtocolFactory(Constants.NewPlayer, player, number: RequiredAmountOfPlayers);
                _logger.LogTrace($"Created {nameof(GameActionProtocol)} with {nameof(GameActionProtocol.Phase)}={{Phase}} for Player {{player}} {{PlayerId}} ", protocol.Phase, player.Name, player.PlayerId);
                await BroadcastMessageAsync(protocol, null);
            }
            if (Game.Players.Count == RequiredAmountOfPlayers)
            {
                var gameActionProtocol = GameActionProtocolFactory(Constants.StartGame, number: TotalPoints);
                await OnStartAsync(gameActionProtocol, null);
                await BroadcastMessageAsync(gameActionProtocol, null);
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

                    Game.NewGame(RequiredAmountOfPlayers);

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
            if (ar.AsyncState is ISocket socket)
            {
                ISocket listener = socket;
                ISocket handler = listener.EndAccept(ar);

                // Create the state object.  
                ConnectionState state = new ConnectionState(handler);
                handler.BeginReceive(state.Buffer, 0, ConnectionState.BufferSize, 0, new AsyncCallback(ReadResult), state);
            }
        }

        private async void ReadResult(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the handler socket  
                // from the asynchronous state object.  
                if (ar.AsyncState is ConnectionState connectionState)
                {
                    ConnectionState state = connectionState;
                    ISocket handler = state.SocketClient;

                    // Read data from the client socket.
                    int bytesRead = handler.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        _logger.LogTrace("Received {0} bytes.", bytesRead);

                        GameActionProtocol gameActionProtocol = state.Buffer.AsSpan().Slice(0, bytesRead).ToArray().ToGameActionProtocol(bytesRead);
                        if (gameActionProtocol.Phase == Constants.RegisterPlayer && !_playerConnections.ContainsKey(gameActionProtocol.PlayerId))
                        {
                            connectionState.Player = GetPlayer(gameActionProtocol);
                            var addResult = _playerConnections.TryAdd(gameActionProtocol.PlayerId, connectionState);
                            _logger.LogInformation("Client {connectionWithPlayerId} {PlayerName} connected and added to Clientlist={addResult}", gameActionProtocol.PlayerId, connectionState.Player?.Name ?? "", addResult);
                        }

                        await OnMessageReceiveAsync(gameActionProtocol, null);

                        //Task.Factory.StartNew(() => OnMessageReceive(gameActionProtocol), TaskCreationOptions.DenyChildAttach);
                    }
                }
                else
                {
                    _logger.LogInformation("Received something unknown");
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, nameof(ReadResult));
            }

        }
        protected void Send(ConnectionState connectionState, GameActionProtocol gameActionProtocol)
        {
            connectionState.LastPayload = gameActionProtocol;
            // Convert the data to byte data
            byte[] byteData = gameActionProtocol.ToByteArray();

            // Begin sending the data to the remote device.  
            connectionState.SocketClient.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), connectionState);
        }

        private async void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                if (ar.AsyncState is ConnectionState connectionState)
                {
                    connectionState.SocketClient.EndSend(ar);
                    connectionState.Buffer = new byte[ConnectionState.BufferSize];
                    int bytesSent = connectionState.SocketClient.Receive(connectionState.Buffer);
                    GameActionProtocol gameActionProtocol = connectionState.Buffer.AsSpan().Slice(0, bytesSent).ToArray().ToGameActionProtocol(bytesSent);
                    await OnMessageReceiveAsync(gameActionProtocol, null);

                    // Complete sending the data to the remote device.  
                    //int bytesSent = handler.EndSend(ar);
                    //int bytesSent = handler.Send(connectionState.Buffer);
                    //_logger.LogTrace("Sent {0} bytes to client.", bytesSent);

                    //handler.Shutdown(SocketShutdown.Both);
                    //handler.Close();
                }
            }
            catch (SocketException e)
            {
                //System.Net.Sockets.SocketException: 'An existing connection was forcibly closed by the remote host.'
                //client ist tot
                _logger.LogError(e, nameof(SendCallback));
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
        protected override async Task BroadcastMessageAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            foreach (var connectionState in _playerConnections.Values)
            {
                _logger.LogDebug("Broadcasting as {ServerId} to {ip} {PlayerName} Phase={Phase}", gameActionProtocol.PlayerId, connectionState.SocketClient.RemoteEndPoint, connectionState.Player?.Name ?? "", Constants.ToString(gameActionProtocol.Phase));
                Send(connectionState, gameActionProtocol);
            }
            //process events which occoured during broadcasting
            while (_EventQueue.Count != 0)
            {
                await _EventQueue.Dequeue().Invoke();
            }
        }
    }
}