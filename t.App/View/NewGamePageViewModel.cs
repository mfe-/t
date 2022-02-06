using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using t.App.Service;

namespace t.App.View;

public class NewGamePageViewModel : BaseViewModel
{
    private readonly NavigationService navigationService;
    private readonly GameService gameService;

    public NewGamePageViewModel(ILogger<NewGamePageViewModel> logger, NavigationService navigationService, GameService gameService)
        : base(logger)
    {
        StartGameCommand = new Command(async () => await StartGameAsync());
        this.navigationService = navigationService;
        this.gameService = gameService;
    }
    public string Title { get; set; } = "Create a new game";

    public ICommand StartGameCommand { get; }

    private async Task StartGameAsync()
    {
        await gameService.StartGameServerAsync(GameName,PlayerName, int.Parse(GameRounds), int.Parse(RequiredPlayers));
        await navigationService.NavigateToAsync(typeof(GamePageViewModel));
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

