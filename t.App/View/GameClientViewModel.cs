using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using t.App.Models;
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
            Game.NewPlayerRegisteredEvent += Game_NewPlayerRegisteredEvent;
            await gameSocketClient.JoinGameAsync(playerName, OnPlayerJoinedAsync);

            if (gameSocketClient.Player == null) throw new InvalidOperationException($"{nameof(gameSocketClient)}.{nameof(gameSocketClient.Player)} is expected to have a value!");
            var player = Game.Players.First(a => a.PlayerId == gameSocketClient.Player.PlayerId);
            PlayerCards = new ObservableCollection<Card>(Game.PlayerCards[player]);

            var messageReceiveArgs = new MessageReceiveArgs(OnNextRoundAsync, GetCardChoiceAsync,
                ShowAvailableCardsAsync, ShowPlayerWon, ShowPlayerStats, ShowPlayerOffered);

            await gameSocketClient.PlayGameAsync(messageReceiveArgs);

            gameSocketClient.ExitGame();
        }

        private void Game_NewPlayerRegisteredEvent(object? sender, NewPlayerRegisteredEventArgs e)
        {

        }
        private string _Title = "";
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value, nameof(Title)); }
        }

        private ObservableCollection<Player> _Players = new();
        public ObservableCollection<Player> Players
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

        public virtual Task OnPlayerJoinedAsync(Player player)
        {
            void AddJoinedPlayer(Player player)
            {
                if (!Players.Contains(player))
                {
                    Players.Add(player);
                }
            }
            if (SynchronizationContext.Current != synchronizationContext)
            {
                synchronizationContext?.Post((player) => AddJoinedPlayer(player as Player
                    ?? throw new ArgumentNullException(nameof(player), $"Expected type of {nameof(Player)}")), player);
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
        public override Task ShowPlayerOffered(Player player, int offered, int forCard)
        {
            //remove the offered card from our player cards list so can't choose the same card again
            if (gameSocketClient?.Player == null) throw new InvalidOperationException($"{nameof(gameSocketClient)}.{nameof(gameSocketClient.Player)} is expected to have a value!");
            if (gameSocketClient?.Player.PlayerId == player.PlayerId)
            {
                var card = PlayerCards.First(a => a.Value == offered);
                PlayerCards.Remove(card);
            }
            PlayersOfferedCard.Add(new PlayerOfferedCard(player, new Card(offered)));
            //display what the other players played
            return Task.CompletedTask;
        }

        public override Task ShowPlayerStats(IEnumerable<Player> playerStats)
        {
            //playerstats contains player lsit with points
            Players = new ObservableCollection<Player>(playerStats);
            return Task.CompletedTask;
        }

        public override Task ShowPlayerWon(IEnumerable<Player> playerStats)
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
