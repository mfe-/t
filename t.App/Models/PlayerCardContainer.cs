using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.lib.Game;

namespace t.App.Models;

public class PlayerCardContainer : INotifyPropertyChanged
{
    public ObservableCollection<Card> PlayerCards { get; set; } = new()
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
    private Card? _SelectedCardPlayer;
    public Card? SelectedCardPlayer
    {
        get { return _SelectedCardPlayer; }
        set { SetProperty(ref _SelectedCardPlayer, value, nameof(SelectedCardPlayer)); }
    }


    private Player? _Player;
    public Player? Player
    {
        get { return _Player; }
        set { SetProperty(ref _Player, value, nameof(Player)); }
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
