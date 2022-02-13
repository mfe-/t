using Microsoft.Extensions.Logging;
using System.Windows.Input;
using t.App.Service;

namespace t.App.View;

internal class JoinGamePageViewModel : BaseViewModel
{
    private new readonly ILogger<JoinGamePageViewModel> logger;
    private readonly NavigationService navigationService;
    private readonly GameService gameService;
    public JoinGamePageViewModel(ILogger<JoinGamePageViewModel> logger, NavigationService navigationService, GameService gameService) : base(logger)
    {
        JoinGameCommand = new Command(async () => await OnJoinGameAsync());
        this.logger = logger;
        this.navigationService = navigationService;
        this.gameService = gameService;
        this.gameService.Current = null;
    }

    public String Title { get; set; } = "Join a game";


    private string _ServerIp = "10.51.51.100";
    public string ServerIp
    {
        get { return _ServerIp; }
        set { SetProperty(ref _ServerIp, value, nameof(ServerIp)); }
    }


    private string _ServerPort = "12000";
    public string ServerPort
    {
        get { return _ServerPort; }
        set { SetProperty(ref _ServerPort, value, nameof(ServerPort)); }
    }


    private string _PlayerName = "";
    public string PlayerName
    {
        get { return _PlayerName; }
        set { SetProperty(ref _PlayerName, value, nameof(PlayerName)); }
    }


    public ICommand JoinGameCommand { get; }
    private async Task OnJoinGameAsync()
    {
        gameService.Current = new Models.GameConfig("", PlayerName, 0, 0, ServerIp, int.Parse(ServerPort), 0, new CancellationTokenSource());
        await navigationService.NavigateToAsync(typeof(GamePageViewModel), gameService.Current);
    }
}
