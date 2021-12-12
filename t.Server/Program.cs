// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using t.lib;
using t.lib.Network;
using t.lib.Server;

namespace t.Server
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
                    config.AddCommandLine(args);
                }
            }).ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();

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
                            serviceProvider.GetService<ILogger<GameSocketServer>>() ?? throw new ArgumentNullException());
                    }
                    else
                    {
                        return new GameSocketServer(
                            hostContext.Configuration.GetSection("AppConfig").Get<AppConfig>(),
                            hostContext.Configuration.GetValue<string>("AppConfig:ServerIpAdress"),
                            hostContext.Configuration.GetValue<int>("AppConfig:ServerPort"),
                            hostContext.Configuration.GetValue<int>("AppConfig:BroadcastPort"),
                            serviceProvider.GetService<ILogger<GameSocketServer>>() ?? throw new ArgumentNullException(),
                            identifier);
                    }
                };
                services.AddScoped(GameSocketServerFactory);
                services.AddHostedService(GameSocketServerFactory);
            }).ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            }).UseConsoleLifetime();

            var host = builder.Build();

            var gameSocketServer = host.Services.GetService<GameSocketServer>();
            if (gameSocketServer == null) throw new InvalidOperationException($"{nameof(gameSocketServer)} is null!");
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            await gameSocketServer.StartAsync(cancellationTokenSource.Token);

            //GameActionProtocol gameActionProtocol = new GameActionProtocol();
            //gameActionProtocol.Version = 0b0000100;
            //System.Console.WriteLine(gameActionProtocol.Version);

            //Player player1 = new Player("martin");
            //Player player2 = new Player("simon");
            //Player player3 = new Player("katharina");

            //game.RegisterPlayer(player2);
            //game.RegisterPlayer(player1);

            //game.Start(10);

            //game.PlayerReport(player1, new Card(1));
            //game.PlayerReport(player2, new Card(2));
            //game.PlayerReport(player3, new Card(2));

            //game.NextRound();

            //game.PlayerReport(player1, new Card(2));
            //game.PlayerReport(player2, new Card(1));

            //game.NextRound();

            //game.PlayerReport(player1, new Card(3));
            //game.PlayerReport(player2, new Card(3));


            //socketListener = new SocketListener();
            //thread = new Thread((obj) =>
            //{
            //    socketListener.StartListening();
            //});
            //thread.Start();
            //while (true)
            //{
            //    string? enter = Console.ReadLine();
            //    if (enter == "exit") break;
            //}
            return 0;
        }
    }
}


