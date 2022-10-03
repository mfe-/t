using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App
{
    //
    // Summary:
    //     Extension methods for the Mobile.Logger class.
    public static class LoggerFactoryExtensions
    {
        //
        // Summary:
        //     Adds a output logger named 'Output' to the factory.
        //
        // Parameters:
        //   builder:
        //     The extension method argument.
        public static ILoggingBuilder AddOutput(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, OutputLoggerProvider>());
            return builder;
        }
    }
}
