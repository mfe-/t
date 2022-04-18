using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.lib.Game;

namespace t.App.View
{
    public class DebugPageViewModel : BaseViewModel
    {
        public DebugPageViewModel(ILogger<DebugPageViewModel> logger) : base(logger)
        {
            CurrentCard = new Card(3);
        }

        public ObservableCollection<Card> Cards { get; set; } = new ObservableCollection<Card>()
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


        private Card _CurrentCard;
        public Card CurrentCard
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
    }
}
