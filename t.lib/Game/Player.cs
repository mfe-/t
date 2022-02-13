using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace t.lib.Game
{
    [DebuggerDisplay("Name={Name}, Points={Points}")]
    public class Player : INotifyPropertyChanged
    {
        public Player(string name, Guid playerid)
        {
            Name = name;
            PlayerId = playerid;
        }
        public Guid PlayerId { get; }
        public string Name { get; set; }


        private int _Points;
        public int Points
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
}