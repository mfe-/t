using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App
{
    public class OutputLoggerProvider : ILoggerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<LoggerFilterOptions> _optionsLoggerFilterOptions;
        private readonly List<ILogger> loggers = new List<ILogger>();

        public OutputLoggerProvider(IServiceProvider serviceProvider, IOptions<LoggerFilterOptions> optionsLoggerFilterOptions)
        {
            this._serviceProvider = serviceProvider;
            this._optionsLoggerFilterOptions = optionsLoggerFilterOptions;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new OutputLogger(categoryName, _optionsLoggerFilterOptions.Value);
            loggers.Add(logger);

            return logger;
        }

        public void Dispose() => loggers.Clear();
    }
}
