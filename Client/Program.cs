// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using t.lib;
using t.lib.Client;

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
                    config.AddCommandLine(args);
                }
            }).ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();

                services.AddHostedService(serviceProvider =>
                    new GameSocketClient(
                        IPAddress.Parse(hostContext.Configuration.GetValue<string>("AppConfig:ServerIpAdress")),
                        hostContext.Configuration.GetValue<int>("AppConfig:ServerPort"),
                        serviceProvider.GetService<ILogger<GameSocketClient>>() ?? throw new ArgumentNullException(),
                        hostContext.Configuration.GetValue<string>("AppConfig:DefaultPlayerName")));
            }).ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            }).UseConsoleLifetime();

            await builder.RunConsoleAsync();

            return 0;
        }
    }
}


