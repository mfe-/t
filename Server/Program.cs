// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using t.lib;

namespace t.Server
{
    public class Program
    {
        public static async Task<int> Main(String[] args)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            var configuration = configurationBuilder.Build();

            var serverPort = configuration.GetValue<int>("AppConfig:ServerPort");

            var builder = new HostBuilder().ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddConfiguration(configuration);

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            }).ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();
                //services.AddSingleton<ServerSocketListener>();

                services.AddHostedService(serviceProvider =>
                    new ServerSocketListener(serverPort, serviceProvider.GetService<ILogger<ServerSocketListener>>()));
            }).ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            }).UseConsoleLifetime();



            await builder.RunConsoleAsync();


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


