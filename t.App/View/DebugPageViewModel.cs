using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using t.App.Models;
using t.App.Service;
using t.lib.Game;
using Player = t.App.Models.Player;

namespace t.App.View
{
    public class DebugPageViewModel : BaseViewModel
    {
        private readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
        private readonly NavigationService navigationService;

        public DebugPageViewModel(ILogger<DebugPageViewModel> logger, NavigationService navigationService) : base(logger)
        {
            CurrentCard = new Card(3);
            PickCardCommand = new Command<object>(OnPickCardCommand);
            NextRoundCommand = new Command(OnNextRound);
            StartPlayerWonAnimationCommand = new Command(async () => await ShowPlayerWon(new List<t.lib.Game.Player>()
            {
                new lib.Game.Player("asdf",Guid.Empty) {Points=5 },
                new lib.Game.Player("ma",Guid.Empty) {Points=3 },
            }));
            Task.Factory.StartNew(async () =>
            {
                await InitAsync();
            }, TaskCreationOptions.DenyChildAttach);

            //Player1Container.SelectedCardPlayer = Player1Container.PlayerCards[0];

            this.navigationService = navigationService;
        }

        private async Task InitAsync()
        {
            Player1Container = new(new Player("martin", Guid.Empty));
            Player2Container = new(new Player("stefan", Guid.Empty));
            Player3Container = new(new Player("katharina", Guid.Empty));
            Player4Container = new(new Player("simon", Guid.Empty));

            await Task.Delay(TimeSpan.FromSeconds(1));

            _Players = new();
            Players.Add(Player1Container);
            Players.Add(Player2Container);
            Players.Add(Player3Container);
            Players.Add(Player4Container);

            await Task.Delay(TimeSpan.FromSeconds(2));

            foreach (var container in Players)
            {
                container.PlayerCards = new ObservableCollection<Card>()
                {
                    new Card(1),
                    new Card(2),
                    new Card(3),
                    new Card(4),
                    new Card(5),
                    new Card(6),
                    new Card(7),
                    new Card(8),
                    new Card(9),
                    new Card(10)
                };
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private ObservableCollection<PlayerCardContainer> _Players;
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

        private Card? _CurrentCard;
        public Card? CurrentCard
        {
            get { return _CurrentCard; }
            set { SetProperty(ref _CurrentCard, value, nameof(CurrentCard)); }
        }




        private bool _CardsEnabledPlayer1 = true;
        public bool CardsEnabledPlayer1
        {
            get { return _CardsEnabledPlayer1; }
            set { SetProperty(ref _CardsEnabledPlayer1, value, nameof(CardsEnabledPlayer1)); }
        }

        public ICommand PickCardCommand { get; }

        private void OnPickCardCommand(object param)
        {
            if (Player1Container?.SelectedCardPlayer != null)
            {
                Player1Container.PlayerCards.Remove(Player1Container.SelectedCardPlayer);
            }
            CardsEnabledPlayer1 = false;
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

        public ICommand NextRoundCommand { get; }
        int i = 1;
        private async void OnNextRound()
        {

            foreach (var container in Players)
            {
                container.IsBackCardVisible = false;
            }
            await Task.Delay(TimeSpan.FromSeconds(7));
            foreach (var container in Players)
            {
                container.IsBackCardVisible = true;
            }

            StartAnimationNextRound = true;
            i++;
            WinnerText = "Martin won!";
            NextRound = $"Next round {i}";
            CardsEnabledPlayer1 = true;

            foreach (var container in Players)
            {
                container.SelectedCardPlayer = null;
            }
            StartAnimationNextRound = false;
        }

        private bool _StartAnimationWinnerText;
        public bool StartAnimationWinnerText
        {
            get { return _StartAnimationWinnerText; }
            set { SetProperty(ref _StartAnimationWinnerText, value, nameof(StartAnimationWinnerText)); }
        }

        public ICommand StartPlayerWonAnimationCommand { get; }
        /// <summary>
        /// Start 
        /// </summary>
        /// <param name="playerStats"></param>
        /// <returns></returns>
        public async Task ShowPlayerWon(IEnumerable<t.lib.Game.Player> playerStats)
        {
            async Task StartWinnerAnimationAsync()
            {
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
            StartAnimationWinnerText = false;
        }
    }
}
