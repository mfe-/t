using Microsoft.Extensions.Logging;
using t.App.Models;
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

    public GameConfig? Current { get; set; }

    public Task StartGameServerAsync(string gamename, string playername, int gameRounds, int requiredAmountOfPlayers)
    {
        var ServerIpAdress = GameSocketServer.GetLanIpAdress().First().Address.ToString();
        var config = new AppConfig()
        {
            BroadcastPort = appConfig.BroadcastPort,
            ServerPort = appConfig.ServerPort,
        };
        config.GameRounds = gameRounds;
        config.RequiredAmountOfPlayers = requiredAmountOfPlayers;
        var cancellationTokenServer = new CancellationTokenSource();
        var gameConfig = new GameConfig(gamename, playername, gameRounds, requiredAmountOfPlayers, ServerIpAdress, config.ServerPort, config.BroadcastPort, cancellationTokenServer);

        var gameSocketServer = new GameSocketServer(config, gameConfig.ServerIpAdress, gameConfig.ServerPort, gameConfig.BroadcastPort,
            serviceProvider.GetService<ILogger<GameSocketServer>>() ?? throw new InvalidOperationException($"Could not resolve {nameof(ILogger<GameSocketServer>)}"));

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        gameSocketServer.StartListeningAsync(gamename, gameConfig.CancellationTokenServer.Token)
            .ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    this.logger.LogCritical(t.Exception, nameof(StartGameServerAsync), gameConfig);
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Current = gameConfig;
        return Task.CompletedTask;
    }
    public Task JoinStartedGameServerAsync(GameClient gameClient)
    {
        if (Current == null) throw new InvalidOperationException($"You need to call {nameof(StartGameServerAsync)} before starting this method!");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        Task.Factory.StartNew(
            () => Current.JoinGameTask = gameClient.OnJoinLanGameAsync(Current.ServerIpAdress, Current.ServerPort, Current.PlayerName));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed


        return Task.CompletedTask;
    }



}

