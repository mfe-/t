using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using t.lib.Game.EventArgs;
using t.lib.Game;

namespace t.lib.Network
{
    public abstract class GameClient : IHostedService
    {
        protected readonly ILogger logger;
        protected readonly AppConfig AppConfig;
        protected readonly string[] args;
        protected readonly Func<Task<string>> onChoiceCommandFunc;
        protected GameClient(ILogger logger, AppConfig appConfig, Func<Task<string>> onChoiceCommandFunc)
        {
            this.logger = logger;
            this.AppConfig = appConfig;
            this.args = Environment.GetCommandLineArgs() ?? new string[0];
            this.onChoiceCommandFunc = onChoiceCommandFunc;
        }
        public virtual async Task OnJoinLanGameAsync(string ServerIpAdress, int port, string playerName)
        {
            ThrowException(ServerIpAdress, port, playerName);

            IPAddress iPAddress = IPAddress.Parse(ServerIpAdress);

            GameSocketClient gameSocketClient = new GameSocketClient(iPAddress, port, logger);
            await gameSocketClient.JoinGameAsync(playerName);

            MessageReceiveArgs messageReceiveArgs = new MessageReceiveArgs(OnNextRoundAsync, onChoiceCommandFunc,
                ShowAvailableCardsAsync, ShowPlayerWon, ShowPlayerStats, ShowPlayerOffered);

            await gameSocketClient.PlayGameAsync(messageReceiveArgs);

            gameSocketClient.ExitGame();

        }

        private static void ThrowException(string ServerIpAdress, int port, string playerName)
        {
            if (String.IsNullOrEmpty(ServerIpAdress)) throw new ArgumentException(nameof(ServerIpAdress));
            if (String.IsNullOrEmpty(playerName)) throw new ArgumentException(nameof(playerName));
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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (args.Any(a => !a.Contains("t.Client.dll")))
            {
                await ParseStartArgumentsAsync(args);
            }
            else
            {
                await OnShowMenueAsync();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
