﻿using Microsoft.Extensions.Logging;
using System.Windows.Input;
using t.App.Service;

namespace t.App.View;

public class NewGamePageViewModel : BaseViewModel
{
    private readonly NavigationService navigationService;
    private readonly GameService gameService;
    private readonly DialogService dialogService;

    public NewGamePageViewModel(ILogger<NewGamePageViewModel> logger, NavigationService navigationService, GameService gameService, DialogService dialogService)
        : base(logger)
    {
        StartGameCommand = new Command(async () => await StartGameAsync());
        this.navigationService = navigationService;
        this.gameService = gameService;
        this.dialogService = dialogService;
    }
    public string Title { get; set; } = "Create new game";

    public ICommand StartGameCommand { get; }

    private async Task StartGameAsync()
    {
        try
        {
            await gameService.StartGameServerAsync(GameName, PlayerName, int.Parse(GameRounds), int.Parse(RequiredPlayers));
            await navigationService.NavigateToAsync(typeof(GamePageViewModel));
        }
        catch (InvalidOperationException ex)
        {
            await dialogService.DisplayAsync("Error", ex.Message, "Ok");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Could not start game.");
        }
    }

    private string _GameName = string.Empty;
    public string GameName
    {
        get { return _GameName; }
        set { SetProperty(ref _GameName, value, nameof(GameName)); }
    }

    private string _PlayerName = "Player1";
    public string PlayerName
    {
        get { return _PlayerName; }
        set { SetProperty(ref _PlayerName, value, nameof(PlayerName)); }
    }

    private string _GameRounds = "2";
    public string GameRounds
    {
        get { return _GameRounds; }
        set { SetProperty(ref _GameRounds, value, nameof(GameRounds)); }
    }

    private string _RequiredPlayers = "2";
    public string RequiredPlayers
    {
        get { return _RequiredPlayers; }
        set { SetProperty(ref _RequiredPlayers, value, nameof(RequiredPlayers)); }
    }



}

