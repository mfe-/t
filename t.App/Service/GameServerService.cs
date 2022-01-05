using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.lib;
using t.lib.Network;

namespace t.App.Service;

public class GameService
{
    private readonly ILogger<GameService> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly AppConfig appConfig;

    public GameService(ILogger<GameService> logger, IServiceProvider serviceProvider, AppConfig appConfig)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.appConfig = appConfig;
    }
    public void StartGameServer(int gameRounds, int RequiredAmountOfPlayers)
    {
        var ServerIpAdress = GameSocketServer.GetLanIpAdress().ToString();
        var config = new AppConfig()
        {
            BroadcastPort = appConfig.BroadcastPort,
            ServerPort = appConfig.ServerPort,
        };
        config.GameRounds = gameRounds;
        config.RequiredAmountOfPlayers = RequiredAmountOfPlayers;
        GameSocketServer gameSocketServer = new GameSocketServer(config, ServerIpAdress, config.ServerPort, config.BroadcastPort, serviceProvider.GetService<ILogger<GameSocketServer>>() ?? throw new InvalidOperationException($"Could not resolve {nameof(ILogger<GameSocketServer>)}"));

    }
}

