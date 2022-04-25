using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using t.lib.Game;

namespace t.App.View
{
    public class DebugPageViewModel : BaseViewModel
    {
        public DebugPageViewModel(ILogger<DebugPageViewModel> logger) : base(logger)
        {
            CurrentCard = new Card(3);
            SelectCommand = new Command<object>(OnSelect);
            NextRoundCommand = new Command(OnNextRound);
        }

        public ObservableCollection<Card> Player1Cards { get; set; } = new ObservableCollection<Card>()
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

        public ObservableCollection<Card> Player2Cards { get; set; } = new ObservableCollection<Card>()
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


        private Card? _CurrentCard;
        public Card? CurrentCard
        {
            get { return _CurrentCard; }
            set { SetProperty(ref _CurrentCard, value, nameof(CurrentCard)); }
        }

        private Card? _SelectedCardPlayer1;
        public Card? SelectedCardPlayer1
        {
            get { return _SelectedCardPlayer1; }
            set { SetProperty(ref _SelectedCardPlayer1, value, nameof(SelectedCardPlayer1)); }
        }


        private Card? _SelectedCardPlayer2;
        public Card? SelectedCardPlayer2
        {
            get { return _SelectedCardPlayer2; }
            set { SetProperty(ref _SelectedCardPlayer2, value, nameof(SelectedCardPlayer2)); }
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
            if (SelectedCardPlayer1 != null)
            {
                Player1Cards.Remove(SelectedCardPlayer1);
            }
            CardsEnabledPlayer1 = false;
        }


        private string? _NextRound;
        public string? NextRound
        {
            get { return _NextRound; }
            set { SetProperty(ref _NextRound, value, nameof(NextRound)); }
        }

        public ICommand NextRoundCommand { get; }
        int i = 1;
        private void OnNextRound()
        {
            i++;
            NextRound = $"Next round {i}";
            CardsEnabledPlayer1 = true;
        }
    }
}
