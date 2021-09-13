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
        protected readonly string[] args;
        protected readonly Func<Task> onCommandFunc;
        public GameClient(ILogger logger, string[] args, Func<Task> onCommandFunc)
        {
            this.logger = logger;
            this.args = args;
            this.onCommandFunc = onCommandFunc;
        }
        protected virtual void OnSocketClient()
        {

        }
        protected TaskCompletionSource? TaskCompletionSource;

        public abstract void OnShowMenue();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource = new TaskCompletionSource();
            OnShowMenue();
            await onCommandFunc.Invoke();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource?.TrySetCanceled();
            return Task.CompletedTask;
        }
    }
}
