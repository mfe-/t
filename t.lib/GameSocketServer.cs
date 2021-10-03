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
using t.lib.Messaging;

namespace t.lib.Server
{
    public partial class GameSocketServer : IHostedService
    {
        protected readonly GameLogic _game;
        protected readonly ILogger _logger;
        protected Guid _guid;
        private readonly int RequiredAmountOfPlayers;
        private readonly int TotalPoints;
        private readonly EventAggregator _eventAggregator;
        private readonly Thread _thread;

        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, ILogger logger)
        {
            if (appConfig.RequiredAmountOfPlayers < 1) throw new ArgumentException("At least two players are required to play the game!");
            if (appConfig.TotalPoints < 10) throw new ArgumentException("At least ten points are expected!");
            //ActionDictionary.Add(Constants.PlayerReported, OnPlayerReportedAsync);
            RequiredAmountOfPlayers = appConfig.RequiredAmountOfPlayers;
            TotalPoints = appConfig.TotalPoints;
            ServerPort = serverPort;
            ServerIpAdress = serverIpAdress;
            _game = new GameLogic();
            Game.NewPlayerRegisteredEvent += Game_NewPlayerRegisteredEvent;
            Game.PlayerWonEvent += Game_EndPlayerWonEvent;
            _guid = Guid.NewGuid();
            _thread = Thread.CurrentThread;
            _logger = logger;
            _eventAggregator = new EventAggregator();
            _eventAggregator.Subscribe<PlayerRegisterEventArgs>(OnPlayerRegisterAsync);
            _eventAggregator.Subscribe<PlayerReportEventArgs>(OnPlayerReportedAsync);
            _logger.LogTrace("GameSocketServer ManagedThreadId {ManagedThreadId}", _thread.ManagedThreadId);
        }

        public GameSocketServer(AppConfig appConfig, string serverIpAdress, int serverPort, ILogger logger, Guid guid)
            : this(appConfig, serverIpAdress, serverPort, logger)
        {
            _guid = guid;
        }

        protected virtual GameLogic Game => _game;

        protected virtual Task OnPlayerRegisterAsync(PlayerRegisterEventArgs playerRegisterArgs)
        {
            Player player = playerRegisterArgs.Player;
            if (!Game.Players.Any(a => a.PlayerId == player.PlayerId))
            {
                Game.RegisterPlayer(player);
            }
            return Task.CompletedTask;
        }
        private async void Game_NextRoundEvent(object? sender, NextRoundEventArgs e)
        {
            //broadcast the "new player and all existing players" to all other players
            foreach (var player in Game.Players.ToArray())
            {
                //get the proper worker for the player
                var socketWorkerServer = _playerConnections.First(a => a.Player?.PlayerId == player.PlayerId);
                var protocol = socketWorkerServer.Protocol.GenerateNextRound(e);
                await BroadcastMessageAsync(protocol, null);
            }
        }
        private void Game_EndPlayerWonEvent(object? sender, EventArgs<IEnumerable<Player>> e)
        {
            //Task GenerateGameEndWinner()
            //{
            //    var gameActionProtocol = GameActionProtocolFactory(Constants.PlayerWon, e.Data.First());
            //    return BroadcastMessageAsync(gameActionProtocol, null);
            //}
            //_EventQueue.Enqueue(GenerateGameEndWinner);
        }
        private async Task OnPlayerReportedAsync(PlayerReportEventArgs arg)
        {
            var player = Game.Players.First(a => a.PlayerId == arg.Player.PlayerId);
            Card pickedCard = arg.Card;
            if (!Game.GetRemainingPickCardPlayers().Any())
            {
                //finally tell every player which cards they took
                foreach (var gameAction in Game.gameActions.Where(a => a.Round == Game.Round).ToArray())
                {
                    var gameActionPro = _playerConnections.First().Protocol.GeneratePlayerScored(new PlayerReportEventArgs(gameAction.Player, new Card(gameAction.Offered)));
                    await BroadcastMessageAsync(gameActionPro, null);
                }
                Game.NextRound();
            }
        }

        private async void Game_NewPlayerRegisteredEvent(object? sender, PlayerRegisterEventArgs e)
        {
            //broadcast the "new player and all existing players" to all other players
            foreach (var player in Game.Players.ToArray())
            {
                //get the proper worker for the player
                var socketWorkerServer = _playerConnections.First(a => a.Player?.PlayerId == player.PlayerId);
                var protocol = socketWorkerServer.Protocol.GeneratePlayerJoined(new PlayerJoinedEventArgs(player, RequiredAmountOfPlayers));
                _logger.LogTrace($"Created {nameof(GameActionProtocol)} with {nameof(GameActionProtocol.Phase)}={{Phase}} for Player {{player}} {{PlayerId}} ", protocol.Phase, player.Name, player.PlayerId);
                await BroadcastMessageAsync(protocol, null);
            }
            if (Game.Players.Count == RequiredAmountOfPlayers && !Game.GameStarted)
            {
                await OnStartAsync();
                var gameActionProtocol = _playerConnections.First().Protocol.GenerateStartGame(new GameStartedEventArgs(TotalPoints));
                await BroadcastMessageAsync(gameActionProtocol, null);

                gameActionProtocol = _playerConnections.First().Protocol.GenerateNextRound(new NextRoundEventArgs(1, Game.CurrentCard ?? throw new ArgumentNullException(nameof(Game.CurrentCard))));
                await BroadcastMessageAsync(gameActionProtocol, null);

                Game.NextRoundEvent += Game_NextRoundEvent;
            }
        }
        protected virtual Task OnStartAsync()
        {
            Game.Start(TotalPoints);
            return Task.CompletedTask;
        }
        internal int GetTotalPoints(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.StartGame) throw new ArgumentException($"{nameof(Constants.StartGame)} required for argument {nameof(gameActionProtocol.Phase)}");
            int a = BitConverter.ToInt32(gameActionProtocol.Payload.AsSpan().Slice(0, gameActionProtocol.PayloadSize));
            return a;
        }

        public int ServerPort { get; }
        public string ServerIpAdress { get; }


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
                    //start a new game
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
        private List<SocketWorkerServer> _playerConnections = new List<SocketWorkerServer>();
        CancellationToken CancellationToken = new CancellationToken();
        private void AcceptClientConnection(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            if (ar.AsyncState is ISocket socket)
            {
                ISocket listener = socket;
                ISocket handler = listener.EndAccept(ar);
                SocketWorkerServer socketWorkerServer = new SocketWorkerServer(handler, GameActionServerProtocolFactory(), _logger);
                _playerConnections.Add(socketWorkerServer);
                socketWorkerServer.RunAsyncTask = Task.Factory.StartNew(async () => await socketWorkerServer.RunAsync(CancellationToken), CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
        TaskCompletionSource TaskCompletionWaitNewPlayer = new TaskCompletionSource();
        private GameActionServerProtocol GameActionServerProtocolFactory()
        {
            return new GameActionServerProtocol(_eventAggregator, new(), _guid, _logger);
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
        //SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        protected async Task BroadcastMessageAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            //await semaphoreSlim.WaitAsync();
            foreach (var socketWorker in _playerConnections.ToArray())
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() =>
                {
                    _logger.LogDebug("Broadcasting as {ServerId} to {ip} {PlayerName} Phase={Phase}", gameActionProtocol.PlayerId, socketWorker.Socket.RemoteEndPoint, socketWorker.Player?.Name ?? "", Constants.ToString(gameActionProtocol.Phase));
                    socketWorker.SetOverrideMessage(new GameActionProtocol[] { gameActionProtocol });
                    //"release" WaitingPlayer 
                    if (socketWorker.Protocol.TaskCompletionWaitPlayer is TaskCompletionSource taskCompletionWait
                        && !taskCompletionWait.Task.IsCompleted)
                    {
                        taskCompletionWait.SetResult();
                    }
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            //semaphoreSlim.Release();
            //return Task.CompletedTask;
        }
    }
}