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
using t.lib.EventArgs;
using t.lib.Game;

namespace t.lib.Server
{
    public partial class GameSocketServer : GameSocketBase, IHostedService
    {
        private readonly int RequiredAmountOfPlayers;
        private readonly int TotalPoints;
        private readonly Queue<GameActionProtocol> _EventQueue = new Queue<GameActionProtocol>();
        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, ILogger logger) : base(logger)
        {
            if (appConfig.RequiredAmountOfPlayers < 1) throw new ArgumentException("At least two players are required to play the game!");
            if (appConfig.TotalPoints < 10) throw new ArgumentException("At least ten points are expected!");
            ActionDictionary.Add(Constants.PlayerReported, OnPlayerReportedAsync);
            RequiredAmountOfPlayers = appConfig.RequiredAmountOfPlayers;
            TotalPoints = appConfig.TotalPoints;
            ServerPort = serverPort;
            ServerIpAdress = serverIpAdress;
            _guid = Guid.NewGuid();
        }
        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, ILogger logger, Guid guid)
            : this(appConfig, serverIpAdress, serverPort, logger)
        {
            _guid = guid;
        }
        private Task OnStartAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            int totalPoints = GetTotalPoints(gameActionProtocol);
            Game.Start(totalPoints);
            return Task.CompletedTask;
        }
        protected virtual Task OnPlayerReportedAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            int pickedNumber = GetNumber(gameActionProtocol);
            Card pickedCard = new Card(pickedNumber);
            lock (Game)
            {
                var player = Game.Players.First(a => a.PlayerId == gameActionProtocol.PlayerId);
                Game.PlayerReport(player, pickedCard);
            }
            return Task.CompletedTask;
        }
        private async Task OnNewPlayerAsync()
        {
            //broadcast the "new player and all existing players" to all other players
            Player[] players = Game.Players.ToArray();
            foreach (var player in players)
            {
                var protocol = GameActionProtocolFactory(Constants.NewPlayer, player, number: RequiredAmountOfPlayers);
                _logger.LogTrace($"Created {nameof(GameActionProtocol)} with {nameof(GameActionProtocol.Phase)}={{Phase}} for Player {{player}} {{PlayerId}} ", protocol.Phase, player.Name, player.PlayerId);
                await BroadcastMessageAsync(protocol, null);
            }
        }

        public int ServerPort { get; }
        public string ServerIpAdress { get; }

        private ConcurrentDictionary<Guid, ConnectionState> _playerConnections = new ConcurrentDictionary<Guid, ConnectionState>();

        public bool AllQueueEmpty()
        {
            return _playerConnections.Select(a => a.Value).All(a => a.MessageQueue.Count == 0);
        }
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
        private async void Listening(IPEndPoint localEndPoint, Socket listener)
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
                    listener.Listen(RequiredAmountOfPlayers + 2);

                    Game.NewGame(RequiredAmountOfPlayers);

                    _logger.LogInformation("Waiting for a connection...");
                    // Start listening for connections.  
                    while (true)
                    {
                        if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                            break;
                        var connection = await listener.AcceptAsync();
                        if (Game.Players.Count == RequiredAmountOfPlayers)
                        {
                            var errmsg = "Game started";
                            var msg = GameActionProtocolFactory(Constants.ErrorOccoured, message: errmsg);
                            await connection.SendAsync(msg.ToByteArray(), SocketFlags.None);
                        }
                        else
                        {
                            var connectionState = new ConnectionState(connection);
                            _ = Task.Run(() => ClientHandler(connectionState));
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "exception while listing");
                }
            }
            cancellationTokenSource = null;
        }
        /// <summary>
        /// Guid of first player which joined the game
        /// </summary>
        /// <remarks>
        /// To avoid calling <see cref="GameLogic.NextRound()"/> multiple times only the first player is allowed to do so
        /// </remarks>
        private Guid? _FirstPlayerJoined = null;

        private async Task ClientHandler(ConnectionState connectionState)
        {
            try
            {
                GameActionProtocol gameActionProtocolRec = new GameActionProtocol();
                gameActionProtocolRec.Phase = Constants.Ok;
                GameActionProtocol gameActionProtocolSend = new GameActionProtocol();
                gameActionProtocolSend.Phase = Constants.Ok;
                byte[] bytes;
                bool allPlayersAvailable = false;
                while (gameActionProtocolSend.Phase != Constants.NextRound)
                {
                    int bytesRead = await connectionState.SocketClient.ReceiveAsync(connectionState.Buffer, SocketFlags.None);
                    _logger.LogTrace("Received {0} bytes.", bytesRead);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    gameActionProtocolRec = connectionState.Buffer.AsSpan().Slice(0, bytesRead).ToArray().ToGameActionProtocol(bytesRead);

                    lock (Game)
                    {
                        allPlayersAvailable = Game.Players.Count == RequiredAmountOfPlayers;
                    }

                    if (gameActionProtocolRec.Phase == Constants.RegisterPlayer && !_playerConnections.ContainsKey(gameActionProtocolRec.PlayerId))
                    {
                        var player = GetPlayer(gameActionProtocolRec);
                        connectionState.Player = player;
                        if (_FirstPlayerJoined == null)
                        {
                            _FirstPlayerJoined = connectionState.Player.PlayerId;
                            _logger.LogDebug($"Set FirstPlayerJoined={_FirstPlayerJoined}", _FirstPlayerJoined);
                        }
                        if (connectionState.Player == null)
                        {
                            throw new InvalidOperationException();
                        }
                        var addResult = _playerConnections.TryAdd(gameActionProtocolRec.PlayerId, connectionState);
                        _logger.LogInformation("Client {connectionWithPlayerId} {PlayerName} connected and added to Clientlist={addResult}", gameActionProtocolRec.PlayerId, connectionState.Player?.Name ?? "", addResult);

                        //process message and execute proper game action
                        await OnMessageReceiveAsync(gameActionProtocolRec, null);
                        //broadcast all new players by adding the message to the queue
                        await OnNewPlayerAsync();
                    }
                    else if (gameActionProtocolRec.Phase == Constants.Ok)
                    {
                        gameActionProtocolSend = GameActionProtocolFactory(Constants.WaitingPlayers);
                        await Task.Delay(TimeSpan.FromSeconds(1));

                        if (allPlayersAvailable && AllQueueEmpty())
                        {

                            {
                                //start game all required players are here
                                if (connectionState.Player?.PlayerId == _FirstPlayerJoined)
                                {
                                    var gameActionProtocol = GameActionProtocolFactory(Constants.StartGame, number: TotalPoints);
                                    //start new game
                                    await OnStartAsync(gameActionProtocol, null);
                                    await BroadcastMessageAsync(gameActionProtocol, null);
                                    //first "next round"
                                    gameActionProtocol = CreateNextRoundGameActionProtocol();
                                    await BroadcastMessageAsync(gameActionProtocol, null);
                                }
                            }
                        }
                    }
                    //a messages awaits in the queue - send this message instead of the "regular" one
                    if (connectionState.MessageQueue.Count != 0)
                    {
                        gameActionProtocolSend = connectionState.MessageQueue.Dequeue();
                    }
                    bytes = gameActionProtocolSend.ToByteArray();
                    _logger.LogTrace("Send to {player}@{destination} Phase={Phase}", connectionState.Player?.Name ?? "unkown", connectionState.SocketClient.RemoteEndPoint, Constants.ToString(gameActionProtocolSend.Phase));
                    await connectionState.SocketClient.SendAsync(bytes, SocketFlags.None);
                    connectionState.LastPayload = gameActionProtocolRec;
                }


                while (gameActionProtocolSend.Phase != Constants.PlayerWon)
                {
                    int bytesRead = await connectionState.SocketClient.ReceiveAsync(connectionState.Buffer, SocketFlags.None);
                    _logger.LogTrace("Received {0} bytes.", bytesRead);
                    if (bytesRead == 0) break;
                    gameActionProtocolRec = connectionState.Buffer.AsSpan().Slice(0, bytesRead).ToArray().ToGameActionProtocol(bytesRead);

                    if (gameActionProtocolRec.Phase == Constants.PlayerReported
                        || gameActionProtocolRec.Phase == Constants.Ok)
                    {
                        //process message (player reported)
                        await OnMessageReceiveAsync(gameActionProtocolRec, null);
                        var waitingPlayers = Enumerable.Empty<Player>();
                        lock (Game)
                        {
                            waitingPlayers = Game.GetRemainingPickCardPlayers();
                        }
                        if (waitingPlayers.Any())
                        {
                            gameActionProtocolSend = GameActionProtocolFactory(Constants.WaitingPlayers);
                        }
                        else
                        {
                            GameAction[] gameActions;
                            lock (Game)
                            {
                                gameActions = Game.gameActions.Where(a => a.Round == Game.Round).ToArray();
                            }

                            //make sure Game.NextRound ist not called multiple times
                            if (connectionState.Player?.PlayerId == _FirstPlayerJoined && AllQueueEmpty())
                            {
                                //finally tell every player which cards they took
                                foreach (var gameAction in gameActions)
                                {
                                    var msg = GameActionProtocolFactory(Constants.PlayerScored, player: gameAction.Player, number: gameAction.Offered);
                                    await BroadcastMessageAsync(msg, null);
                                }
                                bool nextRound = Game.NextRound();
                                if (nextRound)
                                {
                                    await BroadcastMessageAsync(CreateNextRoundGameActionProtocol(), null);
                                }
                            }
                            else
                            {
                                gameActionProtocolSend = GameActionProtocolFactory(Constants.WaitingPlayers);
                            }
                        }
                    }
                    if (gameActionProtocolSend.Phase == Constants.WaitingPlayers)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }

                    //a messages awaits in the queue - send this message instead of the "regular" one
                    if (connectionState.MessageQueue.Count != 0)
                    {
                        gameActionProtocolSend = connectionState.MessageQueue.Dequeue();
                    }

                    bytes = gameActionProtocolSend.ToByteArray();
                    _logger.LogTrace("Send to {player}@{destination} Phase={Phase}", connectionState.Player?.Name ?? "unkown", connectionState.SocketClient.RemoteEndPoint, Constants.ToString(gameActionProtocolSend.Phase));
                    await connectionState.SocketClient.SendAsync(bytes, SocketFlags.None);
                    connectionState.LastPayload = gameActionProtocolRec;
                }


            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionReset)
            {
                _logger.LogInformation("Player {playername}@{ip} {playerguid} left", connectionState.Player?.Name ?? "unknown", connectionState.SocketClient.RemoteEndPoint, connectionState.Player?.PlayerId);
                if (connectionState.Player == null)
                {
                    return;
                }

                //remove player from game
                lock (Game)
                {
                    var player = Game.Players.First(a => a.PlayerId == connectionState.Player.PlayerId);
                    Game.Players.Remove(player);
                }

                bool successful = _playerConnections.TryRemove(connectionState.Player.PlayerId, out ConnectionState? removedConnectionState);
                if (!successful) throw new InvalidOperationException($"Could not remove player from {nameof(_playerConnections)}");
                //if our "main" player left we need to look for a new one
                if (connectionState.Player.PlayerId == _FirstPlayerJoined)
                {
                    _FirstPlayerJoined = _playerConnections.Keys.First();
                }

                //tell everyone that player left
                var msg = GameActionProtocolFactory(Constants.KickedPlayer, player: connectionState.Player, number: RequiredAmountOfPlayers);
                await BroadcastMessageAsync(msg, null);
            }
            catch (Exception ex)
            {
                _logger.LogError("exception", ex);
            }
            finally
            {
                connectionState.SocketClient.Dispose();
            }
        }

        private GameActionProtocol CreateNextRoundGameActionProtocol()
        {
            GameActionProtocol gameActionProtocol;
            var nextRoundEventArgs = new NextRoundEventArgs(Game.Round, Game.CurrentCard ?? throw new InvalidOperationException());
            gameActionProtocol = GameActionProtocolFactory(Constants.NextRound, nextRoundEventArgs: nextRoundEventArgs);
            return gameActionProtocol;
        }

        protected override Task BroadcastMessageAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            foreach (var key in _playerConnections.Keys)
            {
                bool tryGetValue = false;
                int counter = 3;
                while (tryGetValue == false && counter != 0)
                {
                    if (_playerConnections.TryGetValue(key, out var connectionState))
                    {
                        connectionState.MessageQueue.Enqueue(gameActionProtocol);
                        _logger.LogDebug("Broadcasting as {ServerId} to {ip} {PlayerName} Phase={Phase} QueueSize={size}", gameActionProtocol.PlayerId, connectionState.SocketClient.RemoteEndPoint, connectionState.Player?.Name ?? "", Constants.ToString(gameActionProtocol.Phase), connectionState.MessageQueue.Count);
                        tryGetValue = true;
                    }
                    else
                    {
                        _logger.LogError("Failed recieving item from {playerConnections} for {PlayerId}", nameof(_playerConnections), key);
                        counter--;
                    }
                }
            }
            return Task.CompletedTask;
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
    }
}