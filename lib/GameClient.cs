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
using t.lib.EventArgs;

namespace t.lib
{
    public abstract class GameClient : IHostedService
    {
        protected readonly ILogger logger;
        protected readonly IConfiguration configuration;
        protected readonly string[] args;
        protected readonly Func<Task<string>> onChoiceCommandFunc;
        public GameClient(ILogger logger, IConfiguration configuration, Func<Task<string>> onChoiceCommandFunc)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.args = Environment.GetCommandLineArgs() ?? new string[0];
            this.onChoiceCommandFunc = onChoiceCommandFunc;
        }
        public virtual async Task OnJoinLanGameAsync(string ServerIpAdress, int port, string playerName)
        {
            if (String.IsNullOrEmpty(ServerIpAdress)) throw new ArgumentException(nameof(ServerIpAdress));
            if (String.IsNullOrEmpty(playerName)) throw new ArgumentException(nameof(playerName));
            if (port == 0) throw new ArgumentException("port 0 not allowed");

            IPAddress iPAddress = IPAddress.Parse(ServerIpAdress);

            GameSocketClient gameSocketClient = new GameSocketClient(iPAddress, port, logger);
            await gameSocketClient.JoinGameAsync(playerName);

            MessageReceiveArgs messageReceiveArgs = new MessageReceiveArgs(OnNextRoundAsync, onChoiceCommandFunc, ShowAvailableCardsAsync, ShowPlayerWon, ShowPlayerStats);

            await gameSocketClient.PlayGameAsync(messageReceiveArgs);
        }
        protected TaskCompletionSource? TaskCompletionSource;
        public abstract Task OnNextRoundAsync(NextRoundEventArgs e);
        public abstract Task ShowAvailableCardsAsync(IEnumerable<Card> cards);
        public abstract Task OnShowMenueAsync();
        public abstract Task ParseStartArgumentsAsync(string[] args);
        public abstract Task ShowPlayerWon(IEnumerable<Player> playerStats);
        public abstract Task ShowPlayerStats(IEnumerable<Player> playerStats);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource = new TaskCompletionSource();
            if (args.Where(a => !a.Contains("t.Client.dll")).Any())
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
            TaskCompletionSource?.TrySetCanceled();
            return Task.CompletedTask;
        }
    }
}
