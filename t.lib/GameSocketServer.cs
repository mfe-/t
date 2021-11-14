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

                        _ = Task.Run(async () =>
                        {
                            var buffer = new byte[4096];
                            try
                            {
                                while (true)
                                {
                                    int read = await connection.ReceiveAsync(buffer, SocketFlags.None);
                                    if (read == 0)
                                    {
                                        break;
                                    }
                                    await connection.SendAsync(buffer[..read], SocketFlags.None);
                                }
                            }
                            finally
                            {
                                connection.Dispose();
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "exception while listing");
                }
            }
            cancellationTokenSource = null;
        }


        protected override Task BroadcastMessageAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            throw new NotImplementedException();
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