using System.ComponentModel;

namespace t.App.Models;

public class Player : lib.Game.Player, INotifyPropertyChanged
{
    public Player(string name, Guid playerid) : base(name,playerid)
    {
    }


    private bool _IsCurrentPlayer;
    public bool IsCurrentPlayer
    {
        get { return _IsCurrentPlayer; }
        set { SetProperty(ref _IsCurrentPlayer, value, nameof(IsCurrentPlayer)); }
    }

    private int _Points;
    public override int Points
    {
        get { return _Points; }
        set { SetProperty(ref _Points, value, nameof(Points)); }
    }

    public override bool Equals(object? obj)
    {
        if (obj is Player player)
        {
            return player.PlayerId == this.PlayerId;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return PlayerId.GetHashCode() + Name.GetHashCode();
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
