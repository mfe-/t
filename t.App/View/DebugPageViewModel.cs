using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using t.App.Models;
using t.lib.Game;
using Player = t.App.Models.Player;

namespace t.App.View
{
    public class DebugPageViewModel : BaseViewModel
    {
        public DebugPageViewModel(ILogger<DebugPageViewModel> logger) : base(logger)
        {
            CurrentCard = new Card(3);
            SelectCommand = new Command<object>(OnSelect);
            NextRoundCommand = new Command(OnNextRound);
            Player1Container = new() { Player = new Player("martin", Guid.Empty) };
            Player2Container = new() { Player = new Player("stefan", Guid.Empty) };

            PlayerContainers = new();
            PlayerContainers.Add(Player1Container);
            PlayerContainers.Add(Player2Container);
        }

        private ObservableCollection<PlayerCardContainer> _PlayerContainers;
        public ObservableCollection<PlayerCardContainer> PlayerContainers
        {
            get { return _PlayerContainers; }
            set { SetProperty(ref _PlayerContainers, value, nameof(PlayerContainers)); }
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

        public ICommand SelectCommand { get; }

        private void OnSelect(object param)
        {
            if (Player1Container?.SelectedCardPlayer != null)
            {
                Player1Container.PlayerCards.Remove(Player1Container.SelectedCardPlayer);
            }
            CardsEnabledPlayer1 = false;
        }


        private bool? _StartAnimation;
        public bool? StartAnimation
        {
            get { return _StartAnimation; }
            set { SetProperty(ref _StartAnimation, value, nameof(StartAnimation)); }
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

            foreach (var container in PlayerContainers)
            {
                container.IsBackCardVisible = false;
            }
            await Task.Delay(TimeSpan.FromSeconds(7));
            foreach (var container in PlayerContainers)
            {
                container.IsBackCardVisible = true;
            }

            StartAnimation = true;
            i++;
            WinnerText = "Martin won!";
            NextRound = $"Next round {i}";
            CardsEnabledPlayer1 = true;

            foreach (var container in PlayerContainers)
            {
                container.SelectedCardPlayer = null;
            }
            StartAnimation = false;
        }
    }
}
