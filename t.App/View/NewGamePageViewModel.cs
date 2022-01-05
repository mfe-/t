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

namespace t.App.View;

public class NewGamePageViewModel : BaseViewModel
{
    public NewGamePageViewModel(ILogger<NewGamePageViewModel> logger) : base(logger)
    {
        StartGameCommand = new Command(async () => await StartGameAsync());
    }
    public string Title { get; set; } = "Create a new game";

    public ICommand StartGameCommand { get; }

    private Task StartGameAsync()
    {
        return Task.CompletedTask;
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

