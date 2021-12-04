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

namespace t.Client
{
    public class Program
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
                //services.Configure<AppConfig>(options => Configuration.GetSection("AppConfig").Bind(options));
            }).ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();

                //services.AddHostedService(serviceProvider =>
                //    new GameSocketClient(
                //        IPAddress.Parse(hostContext.Configuration.GetValue<string>("AppConfig:ServerIpAdress")),
                //        hostContext.Configuration.GetValue<int>("AppConfig:ServerPort"),
                //        serviceProvider.GetService<ILogger<GameSocketClient>>() ?? throw new ArgumentNullException(),
                //        hostContext.Configuration.GetValue<string>("AppConfig:DefaultPlayerName")));

                var appConfig = hostContext.Configuration.GetSection("AppConfig").Get<AppConfig>();

                Func<IServiceProvider, GameClientConsole> GameClientConsoleFactory = serviceProvider => new GameClientConsole(serviceProvider.GetRequiredService<IServiceProvider>(),
                                        serviceProvider.GetService<ILogger<GameClientConsole>>() ?? throw new ArgumentNullException(nameof(ILogger<GameClientConsole>)),
                                        appConfig ?? throw new ArgumentNullException(nameof(AppConfig)), ReadLineAsync);

                services.AddScoped(GameClientConsoleFactory);
                services.AddHostedService(GameClientConsoleFactory);

            }).ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            }).UseConsoleLifetime();

            var host = builder.Build();

            var gameClientConsole = host.Services.GetService<GameClientConsole>();
            if (gameClientConsole == null) throw new NullReferenceException($"{nameof(gameClientConsole)} is null!");
            await gameClientConsole.ParseStartArgumentsAsync(args);


            return 0;
        }
        public static Task<string> ReadLineAsync()
        {
            string? s = Console.ReadLine();
            return Task.FromResult(s ?? String.Empty);
        }

        //Parser parser = new Parser(with =>
        //{
        //    with.HelpWriter = System.Console.Out;
        //    with.AutoVersion = false;
        //    with.AutoHelp = true;
        //});
        //parser.ParseArguments(
        //    s.Split(" "),
        //        typeof(JoinServerParam).Assembly.GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray())
        //    .MapResult(
        //        (JoinServerParam param) => gameClient.OnJoinLanGameAsync(param.ServerIpAdress, param.ServerPort),
        //        errs => Task.CompletedTask);
    }
}


