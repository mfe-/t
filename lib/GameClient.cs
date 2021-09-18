using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib
{
    public abstract class GameClient : IHostedService
    {
        protected readonly ILogger logger;
        private readonly IConfiguration configuration;
        protected readonly string[] args;
        protected readonly Func<Task<string>> onChoiceCommandFunc;
        public GameClient(ILogger logger, IConfiguration configuration, Func<Task<string>> onChoiceCommandFunc)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.args = Environment.GetCommandLineArgs() ?? new string[0];
            this.onChoiceCommandFunc = onChoiceCommandFunc;
        }
        public virtual Task OnJoinLanGameAsync(string ServerIpAdress, int port)
        {
            if (String.IsNullOrEmpty(ServerIpAdress)) throw new ArgumentException($"{nameof(ServerIpAdress)} is required");
            if (port == 0) throw new ArgumentException("port 0 not allowed");
            return Task.CompletedTask;
        }
        protected TaskCompletionSource? TaskCompletionSource;

        public abstract Task OnShowMenueAsync();

        public abstract Task ParseAsync(string[] args);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource = new TaskCompletionSource();
            if (args.Any())
            {
                await ParseAsync(args);
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
