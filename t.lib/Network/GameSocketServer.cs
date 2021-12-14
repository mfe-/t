using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using t.lib.Game.EventArgs;
using t.lib.Game;
using t.lib.Server;

[assembly: InternalsVisibleTo("t.TestProject1")]
namespace t.lib.Network
{
    public partial class GameSocketServer : GameSocketBase, IHostedService
    {
        private readonly int _RequiredAmountOfPlayers;
        private readonly int? _TotalPoints;
        private readonly int _GameRounds;
        private readonly IPAddress _ServerIpAddress;
        private readonly ConcurrentDictionary<Guid, PlayerConnection> _playerConnections = new();
        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, int udpPort, ILogger logger) : base(logger)
        {
            if (appConfig.GameRounds < 1) throw new ArgumentException("At least one game round should be played!");
            if (appConfig.RequiredAmountOfPlayers < 1) throw new ArgumentException("At least two players are required to play the game!");
            if (appConfig.TotalPoints != null && appConfig.TotalPoints < 10) throw new ArgumentException("At least ten points are expected!");

            ActionDictionary.Add(PhaseConstants.PlayerReported, OnPlayerReportedAsync);
            _RequiredAmountOfPlayers = appConfig.RequiredAmountOfPlayers;
            _TotalPoints = appConfig.TotalPoints;
            _GameRounds = appConfig.GameRounds;
            ServerPort = serverPort;
            UdpPort = udpPort;
            ServerIpAdress = serverIpAdress;
            _guid = Guid.NewGuid();
            if (String.IsNullOrEmpty(serverIpAdress))
            {
                _ServerIpAddress = GetLanIpAdress();
                _logger.LogTrace($"{nameof(ServerIpAdress)} not set!");
            }
            else
            {
                _ServerIpAddress = IPAddress.Parse(serverIpAdress);
            }
            _logger.LogTrace("Use {serverip} for tcp sockets", _ServerIpAddress);
        }
        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, int udpPort, ILogger logger, Guid guid)
            : this(appConfig, serverIpAdress, serverPort, udpPort, logger)
        {
            _guid = guid;
        }
        private Task OnStartAsync(GameActionProtocol gameActionProtocol)
        {
            var values = GetGameStartValues(gameActionProtocol);
            Game.Start(values.Totalpoints);
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
        private async Task BroadcastNewPlayerAsync()
        {
            //broadcast the "new player and all existing players" to all other players
            Player[] players = Game.Players.ToArray();
            foreach (var player in players)
            {
                var protocol = GameActionProtocolFactory(PhaseConstants.NewPlayer, player, number: _RequiredAmountOfPlayers);
                _logger.LogTrace($"Created {nameof(GameActionProtocol)} with {nameof(GameActionProtocol.Phase)}={{Phase}} for Player {{player}} {{PlayerId}} ", protocol.Phase, player.Name, player.PlayerId);
                await BroadcastMessageAsync(protocol, null);
            }
        }

        public int ServerPort { get; }
        public int UdpPort { get; }
        public string ServerIpAdress { get; }


        public bool AllQueueEmpty()
        {
            return _playerConnections.Select(a => a.Value).All(a => a.MessageQueue.Count == 0);
        }
        private Socket? _listener;
        public async Task StartListeningAsync(string serverName, CancellationToken cancellationToken)
        {
            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the
            // host running the application.  

            IPEndPoint localEndPoint = new IPEndPoint(_ServerIpAddress, ServerPort);

            // Create a TCP/IP socket.  
            _listener = new Socket(_ServerIpAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            var listenerTask = await Task.Factory.StartNew(() => ListeningAsync(serverName, localEndPoint, _listener),
                cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            await listenerTask;
        }

        public static IPAddress GetLanIpAdress()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            return ipHostInfo.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
        }


        public async Task StartBroadcastingServerAsync(string gameName, int requiredAmountOfPlayers, int gameRounds, CancellationToken cancellationToken)
        {
            var broadcastTask = await Task.Factory.StartNew(async () =>
            {
                using UdpClient client = new UdpClient();
                IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, UdpPort);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var msgbytes = GenerateBroadcastMessage(_ServerIpAddress, ServerPort, gameName, requiredAmountOfPlayers, _playerConnections.Count, gameRounds);
                    _logger.LogTrace("UDP Broadcast {bytes} bytes {ServerIpAdress}:{socketServerPort} Gamename:{Gamename} Players {currentPlayers}/{ofRequiredPlayers} GameRounds:{GameRounds}", msgbytes.Length,
                        _ServerIpAddress, ServerPort, gameName, _playerConnections.Count, requiredAmountOfPlayers, gameRounds);
                    await client.SendAsync(msgbytes, ip, cancellationToken);

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                _logger.LogInformation($"Stopped {nameof(StartBroadcastingServerAsync)}");
            });

            await broadcastTask;

        }
        public const int maxBroadcastCharacters = 60;
        internal byte[] GenerateBroadcastMessage(IPAddress iPAddress, int port, string gameName, int requiredAmountOfPlayers, int currentAmountOfPlayers, int gameRounds)
        {
            //4
            var ipBytes = iPAddress.GetAddressBytes();
            //4
            var portBytes = BitConverter.GetBytes(port);
            //4
            var requiredAmountOfPlayersBytes = BitConverter.GetBytes(requiredAmountOfPlayers);
            //4
            var gameRoundsBytes = BitConverter.GetBytes(gameRounds);
            //4
            var currentAmountPlayersBytes = BitConverter.GetBytes(currentAmountOfPlayers);
            //we only take the first 60 characters
            var serverNameBytes = Encoding.ASCII.GetBytes(gameName.Length < 60 ? gameName : gameName.Substring(0, maxBroadcastCharacters));

            var broadcastMsg = new byte[ipBytes.Length + portBytes.Length + requiredAmountOfPlayersBytes.Length + gameRoundsBytes.Length + maxBroadcastCharacters];

            ipBytes.CopyTo(broadcastMsg, 0);
            portBytes.CopyTo(broadcastMsg, 4);
            requiredAmountOfPlayersBytes.CopyTo(broadcastMsg, 8);
            gameRoundsBytes.CopyTo(broadcastMsg, 12);
            currentAmountPlayersBytes.CopyTo(broadcastMsg, 16);
            serverNameBytes.CopyTo(broadcastMsg, 20);

            return broadcastMsg;
        }


        private CancellationTokenSource? cancellationTokenSource;
        private CancellationTokenSource? _cancellationTokenBroadcasting;
        private async Task ListeningAsync(string serverName, IPEndPoint localEndPoint, Socket listener)
        {
            _logger.LogInformation($"{nameof(ListeningAsync)} on {localEndPoint.Address}:{ServerPort}", localEndPoint.Address, ServerPort);
            _logger.LogInformation("ServerId {ServerId} generated", _guid);
            // Bind the socket to the local endpoint and
            // listen for incoming connections.  
            using (cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(_RequiredAmountOfPlayers + 2);

                    Game.NewGame(_RequiredAmountOfPlayers, _GameRounds);

                    _logger.LogInformation("Waiting for a connection...");
                    // Start listening for connections.  
                    _cancellationTokenBroadcasting = new CancellationTokenSource();
                    _ = StartBroadcastingServerAsync(serverName, _RequiredAmountOfPlayers, _GameRounds, _cancellationTokenBroadcasting.Token);

                    while (true)
                    {
                        if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                            break;
                        var connection = await listener.AcceptAsync();
                        if (Game.Players.Count == _RequiredAmountOfPlayers)
                        {
                            var errmsg = "Game has already started";
                            var msg = GameActionProtocolFactory(PhaseConstants.ErrorOccoured, message: errmsg);
                            await connection.SendAsync(msg.ToByteArray(), SocketFlags.None);
                            connection.Close();
                        }
                        else
                        {
                            var connectionState = new PlayerConnection(connection);
                            _ = Task.Run(() => ClientHandler(connectionState));
                        }
                    }
                }
                catch (SocketException se)
                    //check if we closed the listener
                    when (se.ErrorCode == 995 && cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                {

                    _logger.LogInformation($"{nameof(_listener)} closed");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "exception while listing");
                }
            }
            cancellationTokenSource = null;
            if (_cancellationTokenBroadcasting != null && !_cancellationTokenBroadcasting.IsCancellationRequested)
            {
                _cancellationTokenBroadcasting.Cancel();
            }
        }
        /// <summary>
        /// Guid of first player which joined the game
        /// </summary>
        /// <remarks>
        /// To avoid calling <see cref="GameLogic.NextRound()"/> multiple times only the first player is allowed to do so
        /// </remarks>
        private Guid? _FirstPlayerJoined = null;
        private volatile byte GlobalGamePhase = PhaseConstants.StartGame;
        private async Task ClientHandler(PlayerConnection connectionState)
        {
            try
            {
                GameActionProtocol gameActionProtocolRec = new GameActionProtocol();
                gameActionProtocolRec.Phase = PhaseConstants.Ok;
                GameActionProtocol gameActionProtocolSend = new GameActionProtocol();
                gameActionProtocolSend.Phase = PhaseConstants.Ok;
                byte[] bytes;
                bool allPlayersAvailable = false;
                while (gameActionProtocolSend.Phase != PhaseConstants.NextRound)
                {
                    if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                        break;
                    int bytesRead = await connectionState.SocketClient.ReceiveAsync(connectionState.Buffer, SocketFlags.None);
                    _logger.LogTrace("Received {0} bytes.", bytesRead);
                    if (bytesRead == 0) break;

                    gameActionProtocolRec = connectionState.Buffer.AsSpan().Slice(0, bytesRead).ToArray().ToGameActionProtocol(bytesRead);

                    lock (Game)
                    {
                        allPlayersAvailable = Game.Players.Count == _RequiredAmountOfPlayers;
                    }

                    if (gameActionProtocolRec.Phase == PhaseConstants.RegisterPlayer && !_playerConnections.ContainsKey(gameActionProtocolRec.PlayerId))
                    {
                        await HandleRegisterPlayerAsync(connectionState, gameActionProtocolRec);
                    }
                    else if (gameActionProtocolRec.Phase == PhaseConstants.Ok)
                    {
                        gameActionProtocolSend = GameActionProtocolFactory(PhaseConstants.WaitingPlayers);
                        await Task.Delay(TimeSpan.FromSeconds(1));

                        if (allPlayersAvailable && AllQueueEmpty() && connectionState.Player?.PlayerId == _FirstPlayerJoined)
                        {
                            //start game all required players are here
                            await QueueStartGameAndNextRoundAsync();
                        }
                    }
                    //a messages awaits in the queue - send this message instead of the "regular" one
                    if (connectionState.MessageQueue.Count != 0)
                    {
                        gameActionProtocolSend = connectionState.MessageQueue.Dequeue();
                    }
                    bytes = gameActionProtocolSend.ToByteArray();
                    _logger.LogTrace("Send to {player}@{destination} Phase={Phase}", connectionState.Player?.Name ?? "unkown", connectionState.SocketClient.RemoteEndPoint, PhaseConstants.ToString(gameActionProtocolSend.Phase));
                    await connectionState.SocketClient.SendAsync(bytes, SocketFlags.None);
                    connectionState.LastRecPayload = gameActionProtocolRec;
                    connectionState.LastSendPayload = gameActionProtocolSend;
                }

                _cancellationTokenBroadcasting?.Cancel();
                GlobalGamePhase = PhaseConstants.NextRound;

                while (gameActionProtocolSend.Phase != PhaseConstants.PlayerWon)
                {
                    if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                        break;
                    int bytesRead = await connectionState.SocketClient.ReceiveAsync(connectionState.Buffer, SocketFlags.None);
                    _logger.LogTrace("Received {0} bytes.", bytesRead);
                    if (bytesRead == 0) break;
                    gameActionProtocolRec = connectionState.Buffer.AsSpan().Slice(0, bytesRead).ToArray().ToGameActionProtocol(bytesRead);

                    if (gameActionProtocolRec.Phase == PhaseConstants.PlayerReported
                        || gameActionProtocolRec.Phase == PhaseConstants.Ok)
                    {
                        //process message (player reported)
                        await OnMessageReceiveAsync(gameActionProtocolRec, null);
                        //create message
                        var waitingPlayers = GetWaitingPlayers();
                        if (waitingPlayers.Any())
                        {
                            gameActionProtocolSend = GameActionProtocolFactory(PhaseConstants.WaitingPlayers);
                        }
                        else
                        {
                            //make sure Game.NextRound ist not called multiple times
                            if (connectionState.Player?.PlayerId == _FirstPlayerJoined && AllQueueEmpty())
                            {
                                await QueuePlayerScoredAndNextRoundAsync();
                            }
                            else
                            {
                                gameActionProtocolSend = GameActionProtocolFactory(PhaseConstants.WaitingPlayers);
                            }
                        }
                    }

                    //a messages awaits in the queue - send this message instead of the "regular" one
                    if (connectionState.MessageQueue.Count != 0)
                    {
                        gameActionProtocolSend = connectionState.MessageQueue.Dequeue();
                    }
                    if (gameActionProtocolSend.Phase == PhaseConstants.WaitingPlayers)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    bytes = gameActionProtocolSend.ToByteArray();
                    _logger.LogTrace("Send to {player}@{destination} bytes={bytes} Phase={Phase}", connectionState.Player?.Name ?? "unkown", connectionState.SocketClient.RemoteEndPoint, bytes.Length, PhaseConstants.ToString(gameActionProtocolSend.Phase));
                    await connectionState.SocketClient.SendAsync(bytes, SocketFlags.None);
                    connectionState.LastRecPayload = gameActionProtocolRec;
                    connectionState.LastSendPayload = gameActionProtocolSend;

                }
                //game is finished
                await HandleConnectionResetAsync(connectionState);
                connectionState.SocketClient.Close();
                //the last thread should close the listener
                if (AllQueueEmpty())
                {
                    _listener?.Close();
                    cancellationTokenSource?.Cancel();
                }

            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionReset)
            {
                await HandleConnectionResetAsync(connectionState, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(Exception));
            }
            finally
            {
                connectionState.SocketClient.Dispose();
            }
        }

        private async Task HandleRegisterPlayerAsync(PlayerConnection connectionState, GameActionProtocol gameActionProtocolRec)
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
            await BroadcastNewPlayerAsync();
        }

        private IEnumerable<Player> GetWaitingPlayers()
        {
            IEnumerable<Player> waitingPlayers;
            lock (Game)
            {
                waitingPlayers = Game.GetRemainingPickCardPlayers();
            }
            return waitingPlayers;
        }
        private async Task QueueStartGameAndNextRoundAsync()
        {
            await QueueStartGameAsync();
            //first "next round"
            var gameActionProtocol = CreateNextRoundGameActionProtocol();
            await BroadcastMessageAsync(gameActionProtocol, null);
        }

        private async Task QueueStartGameAsync()
        {
            var gameActionProtocol = GameActionProtocolFactory(PhaseConstants.StartGame, number: _TotalPoints ?? 0, number2: _GameRounds);
            //start new game
            await OnStartAsync(gameActionProtocol);
            await BroadcastMessageAsync(gameActionProtocol, null);
        }

        private async Task QueuePlayerScoredAndNextRoundAsync()
        {
            GameAction[] gameActions;
            lock (Game)
            {
                gameActions = Game.gameActions.Where(a => a.Round == Game.Round && a.GameRound == Game.GetCurrentGameRound()).ToArray();
            }
            //finally tell every player which cards they took
            foreach (var gameAction in gameActions)
            {
                var msg = GameActionProtocolFactory(PhaseConstants.PlayerScored, player: gameAction.Player, number: gameAction.Offered);
                await BroadcastMessageAsync(msg, null);
            }
            bool nextRound = Game.NextRound();
            if (!nextRound)
            {
                if (Game.FinalGameRounds == Game.TotalRound)
                {
                    var players = Game.GetPlayerStats().OrderByDescending(a => a.Points);
                    var maxPoints = players.Max(a => a.Points);
                    var eventArgs = new PlayerWonEventArgs(players.Where(a => a.Points == maxPoints));

                    var msg = GameActionProtocolFactory(PhaseConstants.PlayerWon, playerWonEventArgs: eventArgs);
                    await BroadcastMessageAsync(msg, null);
                }
                else
                {
                    await QueueStartGameAndNextRoundAsync();
                }
            }
            else
            {
                var gameActionProtocol = CreateNextRoundGameActionProtocol();
                await BroadcastMessageAsync(gameActionProtocol, null);
            }
        }
        /// <summary>
        /// Looks up from the <paramref name="connectionState"/> the corresponding <see cref="Player"/>. Removes the player from the game and <see cref="_playerConnections"/>  
        /// </summary>
        /// <param name="connectionState">the connection which was reseted</param>
        /// <param name="broadcastleftPlayer">if true, broadcast which player left the game</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">if no player was found to connection it throws the exception</exception>
        private async Task HandleConnectionResetAsync(PlayerConnection connectionState, bool broadcastleftPlayer = false)
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
                Game.OnLeavePlayerEvent(player);
            }

            bool successful = _playerConnections.TryRemove(connectionState.Player.PlayerId, out PlayerConnection? removedConnectionState);
            if (!successful) throw new InvalidOperationException($"Could not remove player from {nameof(_playerConnections)}");
            //if our "main" player left we need to look for a new one
            if (connectionState.Player.PlayerId == _FirstPlayerJoined)
            {
                if (_playerConnections.Count > 0)
                {
                    _FirstPlayerJoined = _playerConnections.Keys.First();
                }
                else
                {
                    _logger.LogWarning($"{nameof(_playerConnections)} doesnt contain any players. Cannot set {nameof(_FirstPlayerJoined)}");
                }
            }
            if (broadcastleftPlayer)
            {
                //tell everyone that player left
                var msg = GameActionProtocolFactory(PhaseConstants.KickedPlayer, player: connectionState.Player, number: _RequiredAmountOfPlayers);
                await BroadcastMessageAsync(msg, null);
            }
            if (_playerConnections.Count == 1 && GlobalGamePhase == PhaseConstants.NextRound)
            {
                //if we are in the middle of the game and only one player is left - let the player win
                var lastPlayer = _playerConnections.First().Value.Player;
                if (lastPlayer != null)
                {
                    var msg = GameActionProtocolFactory(PhaseConstants.PlayerWon, player: lastPlayer, playerWonEventArgs: new PlayerWonEventArgs(new Player[] { lastPlayer }));
                    await BroadcastMessageAsync(msg, null);
                }

            }

        }

        private GameActionProtocol CreateNextRoundGameActionProtocol()
        {
            GameActionProtocol gameActionProtocol;
            var nextRoundEventArgs = new NextRoundEventArgs(Game.Round, Game.CurrentCard ?? throw new InvalidOperationException());
            gameActionProtocol = GameActionProtocolFactory(PhaseConstants.NextRound, nextRoundEventArgs: nextRoundEventArgs);
            return gameActionProtocol;
        }

        protected override Task BroadcastMessageAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            foreach (var key in _playerConnections.Keys)
            {
                bool tryGetValue = false;
                int counter = 3;
                while (!tryGetValue && counter != 0)
                {
                    if (_playerConnections.TryGetValue(key, out var connectionState))
                    {
                        connectionState.MessageQueue.Enqueue(gameActionProtocol);
                        _logger.LogDebug("Broadcasting as {ServerId} to {ip} {PlayerName} Phase={Phase} QueueSize={size}", gameActionProtocol.PlayerId, connectionState.SocketClient.RemoteEndPoint, connectionState.Player?.Name ?? "", PhaseConstants.ToString(gameActionProtocol.Phase), connectionState.MessageQueue.Count);
                        if (gameActionProtocol.Phase == PhaseConstants.PlayerScored)
                        {
                            var player = Game.Players.First(a => a.PlayerId == GetPlayer(gameActionProtocol).PlayerId);
                            var offeredCardNumber = GetNumber(gameActionProtocol);
                            _logger.LogDebug("Phase={Phase} {GameRound} Player {player} offered {offeredCardNumber}", PhaseConstants.ToString(gameActionProtocol.Phase), Game.TotalRound, player.Name, offeredCardNumber);
                        }

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
                //dispose exception dont care
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return StartListeningAsync("asdf", cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopListening();
            return Task.CompletedTask;
        }
    }
}