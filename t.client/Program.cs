// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using t.lib;
using t.lib.Console;
using t.lib.Network;

namespace t.Client
{
    public static class Program
    {
        public static async Task<int> Main(String[] args)
        {

            var builder = new HostBuilder().ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json");
                if (args != null)
                {
                    //https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.commandlineconfigurationextensions.addcommandline?view=dotnet-plat-ext-5.0
                    var switchMappings = new Dictionary<string, string>()
                    {
                        { "-ip", "ServerIpAdress" }
                       ,{ "-port", "Port" }
                       ,{ "-join", "join" }
                       ,{"-name", "name" }
                        ,{ "-start","start"}
                        ,{"-gamerounds","gamerounds"}
                        ,{"-players","players"}
                        ,{"-gamename","gamename"},
                        {"-playername","playername" }

                    };
                    config.AddCommandLine(args, switchMappings);
                }
            }).ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();

                var appConfig = hostContext.Configuration.GetSection("AppConfig").Get<AppConfig>();
                Func<IServiceProvider, GameSocketServer> GameSocketServerFactory = serviceProvider =>
                {
                    var identifier = hostContext.Configuration.GetValue<Guid>("AppConfig:Identifier");
                    if (identifier == Guid.Empty)
                    {
                        return new GameSocketServer(
                            hostContext.Configuration.GetSection("AppConfig").Get<AppConfig>(),
                            hostContext.Configuration.GetValue<string>("AppConfig:ServerIpAdress"),
                            hostContext.Configuration.GetValue<int>("AppConfig:ServerPort"),
                            hostContext.Configuration.GetValue<int>("AppConfig:BroadcastPort"),
                            serviceProvider.GetService<ILogger<GameSocketServer>>() ?? throw new InvalidOperationException($"{nameof(ILogger<GameSocketServer>)} is null. Could not resolve service!"));
                    }
                    else
                    {
                        return new GameSocketServer(
                            hostContext.Configuration.GetSection("AppConfig").Get<AppConfig>(),
                            hostContext.Configuration.GetValue<string>("AppConfig:ServerIpAdress"),
                            hostContext.Configuration.GetValue<int>("AppConfig:ServerPort"),
                            hostContext.Configuration.GetValue<int>("AppConfig:BroadcastPort"),
                            serviceProvider.GetService<ILogger<GameSocketServer>>() ?? throw new InvalidOperationException($"{nameof(ILogger<GameSocketServer>)} is null. Could not resolve service!"),
                            identifier);
                    }
                };

                Func<IServiceProvider, GameClientConsole> GameClientConsoleFactory = serviceProvider => new GameClientConsole(serviceProvider.GetRequiredService<IServiceProvider>(),
                                        serviceProvider.GetService<ILogger<GameClientConsole>>() ?? throw new InvalidOperationException($"{nameof(ILogger<GameClientConsole>)} is null. Could not resolve service!"),
                                        appConfig ?? throw new InvalidOperationException($"{nameof(AppConfig)} is null which was not expected!"), ReadLineAsync);

                services.AddScoped(GameClientConsoleFactory);
                services.AddHostedService(GameClientConsoleFactory);
                services.AddScoped(GameSocketServerFactory);
                services.AddHostedService(GameSocketServerFactory);

            }).ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            }).UseConsoleLifetime();

            var host = builder.Build();

            var gameClientConsole = host.Services.GetService<GameClientConsole>();
            if (gameClientConsole == null) throw new InvalidOperationException($"{nameof(gameClientConsole)} is null!");
            await gameClientConsole.ParseStartArgumentsAsync(args);


            return 0;
        }
        public static Task<string> ReadLineAsync()
        {
            string? s = Console.ReadLine();
            return Task.FromResult(s ?? String.Empty);
        }
    }
}


