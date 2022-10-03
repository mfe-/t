using System.Collections.ObjectModel;
using System.ComponentModel;
using t.lib.Game;

namespace t.App.Models;

public class PlayerCardContainer : INotifyPropertyChanged
{
    public PlayerCardContainer(Player player)
    {
        _Player = player;
    }

    private ObservableCollection<Card> _PlayerCards = new();
    public ObservableCollection<Card> PlayerCards
    {
        get { return _PlayerCards; }
        set { SetProperty(ref _PlayerCards, value, nameof(PlayerCards)); }
    }

    private Card? _SelectedCardPlayer;
    public Card? SelectedCardPlayer
    {
        get { return _SelectedCardPlayer; }
        set { SetProperty(ref _SelectedCardPlayer, value, nameof(SelectedCardPlayer)); }
    }


    private Player _Player;
    public Player Player
    {
        get { return _Player; }
        set { SetProperty(ref _Player, value, nameof(Player)); }
    }


    private bool _IsBackCardVisible = true;
    public bool IsBackCardVisible
    {
        get { return _IsBackCardVisible; }
        set { SetProperty(ref _IsBackCardVisible, value, nameof(IsBackCardVisible)); }
    }


    private bool _IsVisible;
    public bool IsVisible
    {
        get { return _IsVisible; }
        set { SetProperty(ref _IsVisible, value, nameof(IsVisible)); }
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
