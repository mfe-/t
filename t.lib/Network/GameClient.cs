using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using t.lib.Game.EventArgs;
using t.lib.Game;

namespace t.lib.Network
{
    public abstract class GameClient : IDisposable
    {
        protected readonly ILogger logger;
        protected readonly AppConfig AppConfig;
        protected readonly string[] args;
        protected GameClient(ILogger logger, AppConfig appConfig)
        {
            this.logger = logger;
            this.AppConfig = appConfig;
            this.args = Environment.GetCommandLineArgs() ?? new string[0];
        }
        protected GameSocketClient? gameSocketClient;
        public virtual async Task OnJoinLanGameAsync(string ServerIpAdress, int port, string playerName)
        {
            ThrowException(ServerIpAdress, port, playerName);

            IPAddress iPAddress = IPAddress.Parse(ServerIpAdress);

            gameSocketClient = new(iPAddress, port, logger);
            await gameSocketClient.JoinGameAsync(playerName);

            var messageReceiveArgs = new MessageReceiveArgs(OnNextRoundAsync, GetCardChoiceAsync,
                ShowAvailableCardsAsync, ShowPlayerWon, ShowPlayerStats, ShowPlayerOffered, OnPlayerKickedAsync);

            await gameSocketClient.PlayGameAsync(messageReceiveArgs);

            gameSocketClient.ExitGame();

        }
        public GameLogic Game
        {
            get
            {
                if (gameSocketClient == null) throw new InvalidOperationException($"Before accessing you need to call {nameof(OnJoinLanGameAsync)}");
                return gameSocketClient.Game;
            }
        }

        protected void ThrowException(string ServerIpAdress, int port, string playerName)
        {
            if (string.IsNullOrEmpty(ServerIpAdress)) throw new ArgumentException(nameof(ServerIpAdress));
            if (string.IsNullOrEmpty(playerName)) throw new ArgumentException(nameof(playerName));
            if (port == 0) throw new ArgumentException("port 0 not allowed");
        }
        public abstract Task OnNextRoundAsync(NextRoundEventArgs e);
        public abstract Task ShowAvailableCardsAsync(IEnumerable<Card> availableCards);
        public abstract Task OnShowMenueAsync();
        public abstract Task ParseStartArgumentsAsync(string[] args);
        public abstract Task ShowPlayerOffered(Player player, int offered, int forCard);
        public abstract Task ShowPlayerWon(IEnumerable<Player> playerStats);
        public abstract Task ShowPlayerStats(IEnumerable<Player> playerStats);
        public abstract Task OnFoundLanGames(IEnumerable<PublicGame> publicGames);
        public abstract Task OnPlayerKickedAsync(PlayerLeftEventArgs playerLeftEventArgs);

        public abstract Task<string> GetCardChoiceAsync();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            // Cleanup
            gameSocketClient?.ExitGame();
            gameSocketClient = null;
        }
        ~GameClient()
        {
            Dispose(false);
        }
    }
}
