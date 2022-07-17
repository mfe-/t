using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        private readonly NavigationService navigationService;
        private readonly DialogService dialogService;

        public GameClientViewModel(ILogger logger, NavigationService navigationService, AppConfig appConfig, DialogService dialogService)
            : base(logger, appConfig)
        {
            PickCardCommand = new Command<Card>(OnPickCard);
            this.navigationService = navigationService;
            this.dialogService = dialogService;
        }
        private string _Title = "";
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value, nameof(Title)); }
        }
        /// <summary>
        /// Join a network game
        /// </summary>
        /// <param name="ServerIpAdress"></param>
        /// <param name="port"></param>
        /// <param name="playerName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public override async Task OnJoinLanGameAsync(string ServerIpAdress, int port, string playerName)
        {
            ThrowException(ServerIpAdress, port, playerName);

            var iPAddress = IPAddress.Parse(ServerIpAdress);

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
                await dialogService.DisplayAsync("Error", $"Connection lost. {e.ToString}", "Ok");
                logger.LogCritical(e, e.ToString());
            }
            gameSocketClient?.ExitGame();
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
#pragma warning disable S2971 // surpress "IEnumerable" LINQs should be simplified as we need to call ToArray() since the collection can change
                var currentPlayer = Game.Players.ToArray().FirstOrDefault(a => a.PlayerId == gameSocketClient?.Player?.PlayerId);
#pragma warning restore S2971 // "IEnumerable" LINQs should be simplified

                if (!Players.Any(a => Mapper.ToPlayer(player).PlayerId == a.Player.PlayerId))
                {
                    var p = Mapper.ToPlayer(player);
                    var playerCardContainer = new PlayerCardContainer(p);
                    if (currentPlayer != null && currentPlayer.PlayerId == p.PlayerId)
                    {
                        p.IsCurrentPlayer = true;
                        Player1Container = playerCardContainer;
                    }
                    else if (Player2Container == null)
                    {
                        Player2Container = playerCardContainer;
                    }
                    else if (Player3Container == null)
                    {
                        Player3Container = playerCardContainer;
                    }
                    else if (Player4Container == null)
                    {
                        Player4Container = playerCardContainer;
                    }
                    else
                    {
                        throw new NotImplementedException("More than four players are currently not implemented!");
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

        private bool _StartAnimationWinnerText;
        public bool StartAnimationWinnerText
        {
            get { return _StartAnimationWinnerText; }
            set { SetProperty(ref _StartAnimationWinnerText, value, nameof(StartAnimationWinnerText)); }
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
            if (Game.Round == 1)
            {
                //make sure the player got some cards to play
                foreach (var playerCardContainer in Players)
                {
                    var player = Game.Players.First(a => playerCardContainer.Player.PlayerId == a.PlayerId);
                    playerCardContainer.PlayerCards = new ObservableCollection<Card>(Game.PlayerCards[player]);
                    playerCardContainer.IsVisible = true;
                }
            }
            if (Game.Round > 1)
            {
                await ShowCardsBySettingIsBackCardVisible();
            }

            CurrentCard = e.Card;
            CardsEnabledPlayer1 = true;

            foreach (var container in Players)
            {
                container.SelectedCardPlayer = null;
            }
        }

        private async Task ShowCardsBySettingIsBackCardVisible()
        {
            foreach (var container in Players)
            {
                container.IsBackCardVisible = true;
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
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

        /// <summary>
        /// Applies the selected offered card of the player
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="offered"></param>
        /// <param name="forCard"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public override Task ShowPlayerOffered(t.lib.Game.Player player, int offered, int forCard)
        {
            //remove the offered card from our player cards list so can't choose the same card again
            if (Player1Container?.Player == null) throw new InvalidOperationException($"{nameof(gameSocketClient)}.{nameof(gameSocketClient.Player)} is expected to have a value!");

            var playeContainer = GetPlayerCardContainer(player);
            //set the selected card of the players
            playeContainer.SelectedCardPlayer = playeContainer.PlayerCards.First(a => a.Value == offered);

            //remove the offered card from the available card set
            var card = playeContainer.PlayerCards.First(a => a.Value == offered);

            void RemoveCard()
            {
                try
                {
                    playeContainer.PlayerCards.Remove(card);
                }
                catch (Exception e)
                {
                    logger.LogError(e, nameof(RemoveCard));
                }
            }

            if (SynchronizationContext.Current != synchronizationContext)
            {
                synchronizationContext?.Post((c) => RemoveCard(), null);
            }
            else
            {
                RemoveCard();
            }

            //display what the other players played
            return Task.CompletedTask;
        }
        /// <summary>
        /// Records the cureent player points and announces the next game round
        /// </summary>
        /// <param name="playerStats"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Displays whether the player won or lost 
        /// </summary>
        /// <param name="playerStats">score result of the game</param>
        /// <returns></returns>
        public override async Task ShowPlayerWon(IEnumerable<t.lib.Game.Player> playerStats)
        {
            async Task StartWinnerAnimationAsync()
            {
                await ShowCardsBySettingIsBackCardVisible();

                if (playerStats.First().PlayerId == Player1Container?.Player?.PlayerId)
                {
                    WinnerText = "You Won";
                }
                else
                {
                    WinnerText = "You Lost";
                }
                StartAnimationWinnerText = true;
                await Task.Delay(TimeSpan.FromSeconds(2));
                StartAnimationWinnerText = false;
                await navigationService.NavigateToAsync(typeof(MainPageViewModel));
            }
            if (SynchronizationContext.Current != synchronizationContext)
            {
                synchronizationContext?.Post(async (c) => await StartWinnerAnimationAsync(), null);
            }
            else
            {
                await StartWinnerAnimationAsync();
            }
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
