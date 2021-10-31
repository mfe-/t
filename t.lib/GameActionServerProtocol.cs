using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using t.lib.EventArgs;
using t.lib.Game;
using t.lib.Messaging;

namespace t.lib
{
    public class GameActionServerProtocol : GameActionBaseProtocol, IProtocol<GameActionProtocol>
    {
        protected Dictionary<byte, Func<GameActionProtocol, GameActionProtocol, GameActionProtocol, object?, Task>> ActionOnMessageReceivedDictionary = new();
        private readonly ILogger _logger;
        private readonly Func<Task<PlayerJoinedEventArgs>> _playerJoinedfunc;
        private readonly EventAggregator _eventAggregator;
        private readonly Guid guid;

        internal Player? Player { get; private set; }
        public GameActionServerProtocol(EventAggregator eventAggregator, Guid guid, ILogger logger, Func<Task<PlayerJoinedEventArgs>> playerJoinedfunc) : base(guid)
        {

            //http://www.albahari.com/threading/part4.aspx#_Wait_and_Pulse
            //queue thread bauen der in while loop schaut ob es ein item (publish) gibt, wenn ja spezifische methode ausführen z.b -> Game.PlayerJoined() -> löst event aus -> selber publish ausführen und auf queue legen die von externen threads gelesen wird
            //2ter thread (worker?) schaut ob es auf externe queue was gibt und führt dann je nach dem ein 
            //possible impementation https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview?redirectedfrom=MSDN
            _logger = logger;
            _playerJoinedfunc = playerJoinedfunc;
            _eventAggregator = eventAggregator;
            this.guid = guid;
            ActionOnMessageReceivedDictionary.Add(Constants.RegisterPlayer, OnRegisterPlayerAsync);
            ActionOnMessageReceivedDictionary.Add(Constants.PlayerReported, OnPlayerReportedAsync);
            ActionOnMessageReceivedDictionary.Add(Constants.Ok, OnOkAsync);
        }

        private async Task OnPlayerReportedAsync(GameActionProtocol gameActionProtocolReceived, GameActionProtocol gameActionProtocolReceivedLast, GameActionProtocol gameActionProtocolLastSent, object? arg4)
        {
            int pickedNumber = GetNumber(gameActionProtocolReceived);
            Card pickedCard = new Card(pickedNumber);
            Player player = new Player("", gameActionProtocolReceived.PlayerId);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => _eventAggregator.PublishAsync(new PlayerReportEventArgs(player, pickedCard)));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            //wait until all player reported status
            TaskCompletionWaitPlayer = new TaskCompletionSource();
            await TaskCompletionWaitPlayer.Task;
        }

        private async Task OnOkAsync(GameActionProtocol gameActionProtocolReceived, GameActionProtocol gameActionProtocolReceivedLast, GameActionProtocol gameActionProtocolLastSent, object? context)
        {
            if (gameActionProtocolReceived.Phase != Constants.Ok) throw new InvalidOperationException($"{nameof(gameActionProtocolReceived)} should have for {nameof(GameActionProtocol.Phase)} {Constants.Ok}");
            byte[] PhaseToWait = new byte[] { Constants.NewPlayer, Constants.StartGame, Constants.PlayerReported };
            if (PhaseToWait.Contains(gameActionProtocolLastSent.Phase))
            {
                //wait until we recieve a event
                TaskCompletionWaitPlayer = new TaskCompletionSource();
                await TaskCompletionWaitPlayer.Task;
            }
        }

        public TaskCompletionSource TaskCompletionWaitPlayer { get; set; } = new TaskCompletionSource();
        protected async Task OnRegisterPlayerAsync(GameActionProtocol gameActionProtocolReceived, GameActionProtocol gameActionProtocolReceivedLast, GameActionProtocol gameActionProtocolLastSent, object? context)
        {
            Player = GetPlayer(gameActionProtocolReceived);
            _logger.LogInformation("Client {connectionWithPlayerId} {PlayerName} connected", gameActionProtocolReceived.PlayerId, Player?.Name ?? "");

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => _eventAggregator.PublishAsync(new PlayerRegisterEventArgs(Player)));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
        public Task<GameActionProtocol> ByteToProtocolTypeAsync(Span<byte> buffer, int receivedBytes)
        {
            return Task.FromResult(buffer.Slice(0, receivedBytes).ToArray().ToGameActionProtocol(receivedBytes));
        }

        public GameActionProtocol DefaultFactory()
        {
            return new GameActionProtocol() { Payload = new byte[0], PayloadSize = 0 };
        }

        public async Task<GameActionProtocol> GenerateMessageAsync(GameActionProtocol protocolTypeReceived, GameActionProtocol lastProtocolTypeReceived, object? obj)
        {
            GameActionProtocol protocol = DefaultFactory();

            if (protocolTypeReceived.Phase == Constants.RegisterPlayer)
            {
                //wait until new player event releases the wait operation
                var playerJoined = await _playerJoinedfunc.Invoke();

                GameActionProtocolFactory(Constants.NewPlayer, playerJoined.JoinedPlayer, number: playerJoined.RequiredAmountOfPlayers);
            }
            _logger.LogTrace($"Created {nameof(GameActionProtocol)} with {nameof(GameActionProtocol.Phase)}={{Phase}}", protocol.Phase);
            return protocolTypeReceived;
        }

        public async Task OnMessageReceivedAsync(GameActionProtocol protocolTypeReceived, GameActionProtocol lastProtocolTypeReceived, GameActionProtocol gameActionProtocolLastSent, object? obj)
        {
            if (ActionOnMessageReceivedDictionary.ContainsKey(protocolTypeReceived.Phase))
            {
                await ActionOnMessageReceivedDictionary[protocolTypeReceived.Phase](protocolTypeReceived, lastProtocolTypeReceived, gameActionProtocolLastSent, obj);
            }
        }
        public GameActionProtocol GeneratePlayerJoined(PlayerJoinedEventArgs e)
        {
            return GameActionProtocolFactory(Constants.NewPlayer, e.JoinedPlayer, number: e.RequiredAmountOfPlayers);
        }
        public GameActionProtocol GenerateStartGame(GameStartedEventArgs e)
        {
            return GameActionProtocolFactory(Constants.StartGame, number: e.TotalPoints);
        }
        public GameActionProtocol GenerateNextRound(NextRoundEventArgs e)
        {
            return GameActionProtocolFactory(Constants.NextRound, nextRoundEventArgs: e);
        }
        public GameActionProtocol GeneratePlayerScored(PlayerReportEventArgs playerReportEventArgs)
        {
            return GameActionProtocolFactory(Constants.PlayerScored, player: playerReportEventArgs.Player, number: playerReportEventArgs.Card.Value);
        }
        public Task<byte[]> ProtocolToByteArrayAsync(GameActionProtocol protocolType)
        {
            return Task.FromResult(ToByteArray(protocolType));
        }
    }
}
