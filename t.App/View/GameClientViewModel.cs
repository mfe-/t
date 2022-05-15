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
        private const int ShowAllOfferedPlayerCardsSeconds = 2;
        private readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
        public GameClientViewModel(ILogger logger, AppConfig appConfig)
            : base(logger, appConfig)
        {
            PickCardCommand = new Command<Card>(OnPickCard);
        }
        private string _Title = "";
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value, nameof(Title)); }
        }
        public override async Task OnJoinLanGameAsync(string ServerIpAdress, int port, string playerName)
        {
            ThrowException(ServerIpAdress, port, playerName);

            IPAddress iPAddress = IPAddress.Parse(ServerIpAdress);

            gameSocketClient = new(iPAddress, port, logger);
            //join game
            try
            {
                //waits until all players joined
                await gameSocketClient.JoinGameAsync(playerName, OnPlayerJoinedAsync);

                if (gameSocketClient.Player == null) throw new InvalidOperationException($"{nameof(gameSocketClient)}.{nameof(gameSocketClient.Player)} is expected to have a value!");

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
        private ObservableCollection<PlayerCardContainer> _Players = new();
        public ObservableCollection<PlayerCardContainer> Players
        {
            get { return _Players; }
            set { SetProperty(ref _Players, value, nameof(Players)); }
        }
        private PlayerCardContainer? _Player1Container;
        public PlayerCardContainer? Player1Container
        {
            get { return _Player1Container; }
            set { SetProperty(ref _Player1Container, value, nameof(Player1Container)); }
        }

        private PlayerCardContainer? _Player2Container;
        public PlayerCardContainer? Player2Container
        {
            get { return _Player2Container; }
            set { SetProperty(ref _Player2Container, value, nameof(Player2Container)); }
        }


        private PlayerCardContainer? _Player3Container;
        public PlayerCardContainer? Player3Container
        {
            get { return _Player3Container; }
            set { SetProperty(ref _Player3Container, value, nameof(Player3Container)); }
        }


        private PlayerCardContainer? _Player4Container;
        public PlayerCardContainer? Player4Container
        {
            get { return _Player4Container; }
            set { SetProperty(ref _Player4Container, value, nameof(Player4Container)); }
        }
        /// <summary>
        /// Adds the joined player to the current viewmodel instance
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual Task OnPlayerJoinedAsync(t.lib.Game.Player player)
        {
            void AddJoinedPlayer(t.lib.Game.Player player)
            {
                if (!Players.Any(a => Mapper.ToPlayer(player).PlayerId == a.Player.PlayerId))
                {
                    var p = Mapper.ToPlayer(player);
                    var playerCardContainer = new PlayerCardContainer(p);
                    if (Players.Count == 0)
                    {
                        p.IsCurrentPlayer = true;
                        Player1Container = playerCardContainer;
                    }
                    else if (Players.Count == 1)
                    {
                        Player2Container = playerCardContainer;
                    }
                    else if (Players.Count == 2)
                    {
                        Player3Container = playerCardContainer;
                    }
                    else if (Players.Count == 3)
                    {
                        Player4Container = playerCardContainer;
                    }
                    Players.Add(playerCardContainer);
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
        private bool? _StartAnimationNextRound;
        public bool? StartAnimationNextRound
        {
            get { return _StartAnimationNextRound; }
            set { SetProperty(ref _StartAnimationNextRound, value, nameof(StartAnimationNextRound)); }
        }

        private string? _NextRound;
        public string? NextRound
        {
            get { return _NextRound; }
            set { SetProperty(ref _NextRound, value, nameof(NextRound)); }
        }

        private string? _WinnerText;
        public string? WinnerText
        {
            get { return _WinnerText; }
            set { SetProperty(ref _WinnerText, value, nameof(WinnerText)); }
        }
        public override async Task OnNextRoundAsync(NextRoundEventArgs e)
        {
            Title = $"Round {e.Round}";
            if(Players.Any(a=>a.PlayerCards.Count==0))
            {
                //make sure player got cards to play
                foreach (var playerCardContainer in Players)
                {
                    var player = Game.Players.First(a => playerCardContainer.Player.PlayerId == a.PlayerId);
                    playerCardContainer.PlayerCards = new ObservableCollection<Card>(Game.PlayerCards[player]);
                }
            }
            if (Game.Round > 1)
            {
                foreach (var container in Players)
                {
                    container.IsBackCardVisible = false;
                }
                await Task.Delay(TimeSpan.FromSeconds(ShowAllOfferedPlayerCardsSeconds));
                foreach (var container in Players)
                {
                    container.IsBackCardVisible = true;
                }
            }

            CurrentCard = e.Card;
            CardsEnabledPlayer1 = true;

            foreach (var container in Players)
            {
                container.SelectedCardPlayer = null;
            }
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
            if (!CardsEnabledPlayer1) return;
            PickCardTaskCompletionSource?.SetResult(param);
        }


        private bool _CardsEnabledPlayer1 = true;
        public bool CardsEnabledPlayer1
        {
            get { return _CardsEnabledPlayer1; }
            set { SetProperty(ref _CardsEnabledPlayer1, value, nameof(CardsEnabledPlayer1)); }
        }

        private TaskCompletionSource<Card>? PickCardTaskCompletionSource = null;
        /// <summary>
        /// Returns the value of the picked card
        /// </summary>
        /// <returns>The cards value as string</returns>
        public override async Task<string> GetCardChoiceAsync()
        {
            CardsEnabledPlayer1 = true;
            PickCardTaskCompletionSource = new TaskCompletionSource<Card>();
            //wait until the player selected a card
            var pickedCard = await PickCardTaskCompletionSource.Task;
            CardsEnabledPlayer1 = false;
            return pickedCard.Value.ToString();
        }
        private PlayerCardContainer GetPlayerCardContainer(t.lib.Game.Player p)
            => Players.First(a => a.Player.PlayerId == p.PlayerId);

        public override Task ShowPlayerOffered(t.lib.Game.Player player, int offered, int forCard)
        {
            //remove the offered card from our player cards list so can't choose the same card again
            if (Player1Container?.Player == null) throw new InvalidOperationException($"{nameof(gameSocketClient)}.{nameof(gameSocketClient.Player)} is expected to have a value!");

            var playeContainer = GetPlayerCardContainer(player);
            //set the selected card of the players
            playeContainer.SelectedCardPlayer = playeContainer.PlayerCards.First(a => a.Value == offered);

            //remove the offered card from the available card set
            var card = playeContainer.PlayerCards.First(a => a.Value == offered);

            if (SynchronizationContext.Current != synchronizationContext)
            {
                synchronizationContext?.Post((c) => playeContainer.PlayerCards.Remove(card), null);
            }
            else
            {
                playeContainer.PlayerCards.Remove(card);
            }

            //display what the other players played
            return Task.CompletedTask;
        }

        public override Task ShowPlayerStats(IEnumerable<t.lib.Game.Player> playerStats)
        {
            void UpdatePlayerStats()
            {
                StartAnimationNextRound = true;
                //playerstats contains player list with points
                foreach (var player in playerStats)
                {
                    var container = GetPlayerCardContainer(player);
                    container.Player.Points = player.Points;
                }
                NextRound = $"Next round {Game.Round}";
                StartAnimationNextRound = false;
            }

            if (SynchronizationContext.Current != synchronizationContext)
            {
                synchronizationContext?.Post((c) => UpdatePlayerStats(), null);
            }
            else
            {
                UpdatePlayerStats();
            }

            return Task.CompletedTask;

        }

        public override async Task ShowPlayerWon(IEnumerable<t.lib.Game.Player> playerStats)
        {
            WinnerText = "Martin won!";
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
