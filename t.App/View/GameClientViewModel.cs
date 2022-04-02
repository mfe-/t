using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Windows.Input;
using t.App.Models;
using t.App.Service;
using t.lib;
using t.lib.Game;
using t.lib.Game.EventArgs;
using t.lib.Network;

namespace t.App.View
{
    public class GameClientViewModel : GameClient, INotifyPropertyChanged
    {
        private readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
        public GameClientViewModel(ILogger logger, AppConfig appConfig)
            : base(logger, appConfig)
        {
            PickCardCommand = new Command<Card>(OnPickCard);
        }
        public override async Task OnJoinLanGameAsync(string ServerIpAdress, int port, string playerName)
        {
            ThrowException(ServerIpAdress, port, playerName);

            IPAddress iPAddress = IPAddress.Parse(ServerIpAdress);

            gameSocketClient = new(iPAddress, port, logger);
            //join game
            try
            {
                await gameSocketClient.JoinGameAsync(playerName, OnPlayerJoinedAsync);

                if (gameSocketClient.Player == null) throw new InvalidOperationException($"{nameof(gameSocketClient)}.{nameof(gameSocketClient.Player)} is expected to have a value!");
                CurrentPlayer = Game.Players.First(a => a.PlayerId == gameSocketClient.Player.PlayerId);
                Players.First(a => a.PlayerId == CurrentPlayer.PlayerId).IsCurrentPlayer = true;
                PlayerCards = new ObservableCollection<Card>(Game.PlayerCards[CurrentPlayer]);

                var messageReceiveArgs = new MessageReceiveArgs(OnNextRoundAsync, GetCardChoiceAsync,
                    ShowAvailableCardsAsync, ShowPlayerWon, ShowPlayerStats, ShowPlayerOffered, OnPlayerKickedAsync);

                await gameSocketClient.PlayGameAsync(messageReceiveArgs);
            }
            catch (SocketException e)
            {
                //todo show dialog or something with lost connection and return to overview
                logger.LogCritical(e, e.ToString());
            }
            gameSocketClient.ExitGame();
        }

        private t.lib.Game.Player? _CurrentPlayer;
        public t.lib.Game.Player? CurrentPlayer
        {
            get { return _CurrentPlayer; }
            set { SetProperty(ref _CurrentPlayer, value, nameof(CurrentPlayer)); }
        }
        private string _Title = "";
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value, nameof(Title)); }
        }

        private ObservableCollection<Models.Player> _Players = new();
        public ObservableCollection<Models.Player> Players
        {
            get { return _Players; }
            set { SetProperty(ref _Players, value, nameof(Players)); }
        }


        private ObservableCollection<PlayerOfferedCard> _PlayersOfferedCard = new();
        public ObservableCollection<PlayerOfferedCard> PlayersOfferedCard
        {
            get { return _PlayersOfferedCard; }
            set { SetProperty(ref _PlayersOfferedCard, value, nameof(PlayersOfferedCard)); }
        }

        public virtual Task OnPlayerJoinedAsync(t.lib.Game.Player player)
        {
            void AddJoinedPlayer(t.lib.Game.Player player)
            {
                if (!Players.Contains(player))
                {
                    var p = Mapper.ToPlayer(player);
                    Players.Add(p);
                }
            }
            if (SynchronizationContext.Current != synchronizationContext)
            {
                synchronizationContext?.Post((player) => AddJoinedPlayer(player as t.lib.Game.Player
                    ?? throw new ArgumentNullException(nameof(player), $"Expected type of {nameof(t.lib.Game.Player)}")), player);
            }
            else
            {
                AddJoinedPlayer(player);
            }
            return Task.CompletedTask;
        }

        public override Task OnFoundLanGames(IEnumerable<PublicGame> publicGames)
        {
            return Task.CompletedTask;
        }


        private Card? _CurrentCard;
        public Card? CurrentCard
        {
            get { return _CurrentCard; }
            set { SetProperty(ref _CurrentCard, value, nameof(CurrentCard)); }
        }

        private ObservableCollection<Card> _PlayerCards = new();
        public ObservableCollection<Card> PlayerCards
        {
            get { return _PlayerCards; }
            set { SetProperty(ref _PlayerCards, value, nameof(PlayerCards)); }
        }

        public override Task OnNextRoundAsync(NextRoundEventArgs e)
        {
            Title = $"Round {e.Round}";
            CurrentCard = e.Card;
            CanPlayerChooseCard = true;
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                PlayersOfferedCard.Clear();
            });

            return Task.CompletedTask;
        }

        public override Task OnShowMenueAsync()
        {
            return Task.CompletedTask;
        }

        public override Task ParseStartArgumentsAsync(string[] args)
        {
            throw new NotImplementedException();
        }

        public override Task ShowAvailableCardsAsync(IEnumerable<Card> availableCards)
        {
            //maybe update available cards if ncecessary
            return Task.CompletedTask;
        }



        public ICommand PickCardCommand { get; }

        private void OnPickCard(Card param)
        {
            if (param == null) return;
            if (!CanPlayerChooseCard) return;
            PickCardTaskCompletionSource?.SetResult(param);
        }


        private bool _CanPlayerChooseCard = false;
        public bool CanPlayerChooseCard
        {
            get { return _CanPlayerChooseCard; }
            set { SetProperty(ref _CanPlayerChooseCard, value, nameof(CanPlayerChooseCard)); }
        }


        private Card? _SelectedCard = null;
        public Card? SelectedCard
        {
            get { return _SelectedCard; }
            set { SetProperty(ref _SelectedCard, value, nameof(SelectedCard)); }
        }

        private TaskCompletionSource<Card>? PickCardTaskCompletionSource = null;
        public override async Task<string> GetCardChoiceAsync()
        {
            SelectedCard = null;
            CanPlayerChooseCard = true;
            PickCardTaskCompletionSource = new TaskCompletionSource<Card>();
            var pickedCard = await PickCardTaskCompletionSource.Task;
            CanPlayerChooseCard = false;
            //clear players offered card if they are still displayed
            PlayersOfferedCard.Clear();
            return pickedCard.Value.ToString();
        }
        public override Task ShowPlayerOffered(t.lib.Game.Player player, int offered, int forCard)
        {
            //remove the offered card from our player cards list so can't choose the same card again
            if (CurrentPlayer == null) throw new InvalidOperationException($"{nameof(gameSocketClient)}.{nameof(gameSocketClient.Player)} is expected to have a value!");
            if (CurrentPlayer.PlayerId == player.PlayerId)
            {
                var card = PlayerCards.First(a => a.Value == offered);
                PlayerCards.Remove(card);
            }
            PlayersOfferedCard.Add(new PlayerOfferedCard(player, new Card(offered)));
            //display what the other players played
            return Task.CompletedTask;
        }

        public override Task ShowPlayerStats(IEnumerable<t.lib.Game.Player> playerStats)
        {
            //playerstats contains player lsit with points
            Players = new ObservableCollection<Models.Player>(playerStats.Select(a => Mapper.ToPlayer(a)));
            Players.First(a => a.PlayerId == CurrentPlayer?.PlayerId).IsCurrentPlayer = true;
            return Task.CompletedTask;
        }

        public override Task ShowPlayerWon(IEnumerable<t.lib.Game.Player> playerStats)
        {
            return Task.CompletedTask;
        }
        public override Task OnPlayerKickedAsync(PlayerLeftEventArgs playerLeftEventArgs)
        {
            return Task.CompletedTask;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }


    }
}
