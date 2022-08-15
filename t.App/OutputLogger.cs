using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace t.App
{
    [DebuggerDisplay("OutputLogger.{_name}")]
    public class OutputLogger : ILogger
    {
        private readonly string _name;
        private readonly LoggerFilterOptions _loggerFilterOptions;

        public OutputLogger(string name, LoggerFilterOptions loggerFilterOptions)
        {
            this._name = name;
            this._loggerFilterOptions = loggerFilterOptions;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            var s = state as IDisposable;

            //_loggingContext = JsonConvert.SerializeObject(s);

            return s;
        }
        /// <summary>
        /// Checks if the given logLevel is enabled.
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException("formatter");
            }

            string text = formatter(state, exception);
            if (!string.IsNullOrEmpty(text))
            {
                text = $"{logLevel}: {text}";
                if (exception != null)
                {
                    text = text + Environment.NewLine + Environment.NewLine + exception;
                }
#if ANDROID
                if (logLevel == LogLevel.Trace)
                    Android.Util.Log.Verbose(text, _name);
                else if (logLevel == LogLevel.Debug)
                    Android.Util.Log.Debug(text, _name);
                else if (logLevel == LogLevel.Information)
                    Android.Util.Log.Info(text, _name);
                else if (logLevel == LogLevel.Warning)
                    Android.Util.Log.Warn(text, _name);
                else if (logLevel == LogLevel.Error)
                    Android.Util.Log.Error(text, _name);
                else if (logLevel == LogLevel.Critical)
                    Android.Util.Log.Wtf(text, _name);
#else
                        OutputWriteLine(text, _name);
#endif
            }
        }

        private void OutputWriteLine(string message, string name)
        {
            System.Diagnostics.Debug.WriteLine(message, name);
        }
    }
}