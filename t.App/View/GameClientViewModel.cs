using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using t.App.Models;
using t.lib;
using t.lib.Game;
using t.lib.Game.EventArgs;
using t.lib.Network;

namespace t.App.View
{
    public class GameClientViewModel : GameClient, INotifyPropertyChanged
    {
        private readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
        public GameClientViewModel(ILogger logger, AppConfig appConfig, Func<Task<string>> onChoiceCommandFunc)
            : base(logger, appConfig, onChoiceCommandFunc)
        {

        }
        public override Task OnJoinLanGameAsync(string ServerIpAdress, int port, string playerName)
        {
            ThrowException(ServerIpAdress, port, playerName);

            IPAddress iPAddress = IPAddress.Parse(ServerIpAdress);

            gameSocketClient = new(iPAddress, port, logger);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            gameSocketClient.JoinGameAsync(playerName, OnPlayerJoinedAsync)
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        logger.LogCritical(t.Exception, nameof(OnJoinLanGameAsync), playerName);
                    }
                }).ConfigureAwait(false);

            var messageReceiveArgs = new MessageReceiveArgs(OnNextRoundAsync, onChoiceCommandFunc,
                ShowAvailableCardsAsync, ShowPlayerWon, ShowPlayerStats, ShowPlayerOffered);

            //await gameSocketClient.PlayGameAsync(messageReceiveArgs);

            //gameSocketClient.ExitGame();

            Game.NewPlayerRegisteredEvent += Game_NewPlayerRegisteredEvent;

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            return Task.CompletedTask;
        }

        private void Game_NewPlayerRegisteredEvent(object? sender, NewPlayerRegisteredEventArgs e)
        {

        }

        private ObservableCollection<Player> _Players = new();
        public ObservableCollection<Player> Players
        {
            get { return _Players; }
            set { SetProperty(ref _Players, value, nameof(Players)); }
        }
        private void AddJoinedPlayer(Player player)
        {
            if (!Players.Contains(player))
            {
                Players.Add(player);
            }
        }
        public virtual Task OnPlayerJoinedAsync(Player player)
        {
            if (SynchronizationContext.Current != synchronizationContext)
            {
                synchronizationContext?.Post((player) => AddJoinedPlayer(player as Player
                    ?? throw new ArgumentNullException(nameof(player), $"Expected type of {nameof(Player)}")), player);
            }
            else
            {
                AddJoinedPlayer(player);
            }
            return Task.CompletedTask;
        }
        public override Task OnFoundLanGames(IEnumerable<PublicGame> publicGames)
        {
            throw new NotImplementedException();
        }

        public override Task OnNextRoundAsync(NextRoundEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override Task OnShowMenueAsync()
        {
            throw new NotImplementedException();
        }

        public override Task ParseStartArgumentsAsync(string[] args)
        {
            throw new NotImplementedException();
        }

        public override Task ShowAvailableCardsAsync(IEnumerable<Card> availableCards)
        {
            throw new NotImplementedException();
        }

        public override Task ShowPlayerOffered(Player player, int offered, int forCard)
        {
            throw new NotImplementedException();
        }

        public override Task ShowPlayerStats(IEnumerable<Player> playerStats)
        {
            throw new NotImplementedException();
        }

        public override Task ShowPlayerWon(IEnumerable<Player> playerStats)
        {
            throw new NotImplementedException();
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
