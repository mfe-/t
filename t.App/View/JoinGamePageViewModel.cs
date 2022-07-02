using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using t.App.Models;
using t.App.Service;
using t.lib;
using t.lib.Network;

namespace t.App.View;

internal class JoinGamePageViewModel : BaseViewModel
{
    private new readonly ILogger<JoinGamePageViewModel> logger;
    private readonly NavigationService navigationService;
    private readonly AppConfig appConfig;
    private readonly GameService gameService;
    private readonly DialogService dialogService;
    private readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
    private CancellationTokenSource? cancellationTokenSourceSearchGamesAsync;
    public JoinGamePageViewModel(ILogger<JoinGamePageViewModel> logger,
        NavigationService navigationService,
        AppConfig appConfig,
        GameService gameService,
        DialogService dialogService) : base(logger)
    {
        JoinGameCommand = new Command(async () => await OnJoinGameAsync());
        AddPublicGameToListCommand = new Command(OnAddPublicGameToList);
        AddPublicGameCommand = new Command(() => AddManuallPublicGame = !AddManuallPublicGame);
        this.logger = logger;
        this.navigationService = navigationService;
        this.appConfig = appConfig;
        this.gameService = gameService;
        this.dialogService = dialogService;
        this.gameService.Current = null;
        _PublicGames = new();
        cancellationTokenSourceSearchGamesAsync = new CancellationTokenSource();
        //this task will never be aborted
        Task.Factory.StartNew(SearchGamesAsync, cancellationTokenSourceSearchGamesAsync.Token);
    }


    private async Task SearchGamesAsync()
    {
        while (cancellationTokenSourceSearchGamesAsync != null && !cancellationTokenSourceSearchGamesAsync.IsCancellationRequested)
        {
            using (CancellationTokenSource cancellationTokenSource2 = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
                var publicGames = await GameSocketClient.FindLanGamesAsync(appConfig.BroadcastPort, cancellationTokenSource2.Token);

                synchronizationContext?.Post((arg) =>
                {
                    foreach (var pg in publicGames)
                    {
                        if (!PublicGames.Any(a => a.GameName == pg.GameName))
                        {
                            PublicGames.Add(pg);
                        }
                    }
                }, null);
            }
        }
    }

    public String Title { get; set; } = "Join a game";


    private ObservableCollection<PublicGame> _PublicGames;
    public ObservableCollection<PublicGame> PublicGames
    {
        get { return _PublicGames; }
        set { SetProperty(ref _PublicGames, value, nameof(PublicGames)); }
    }


    private PublicGame? _SelectedGame;
    public PublicGame? SelectedGame
    {
        get { return _SelectedGame; }
        set { SetProperty(ref _SelectedGame, value, nameof(SelectedGame)); }
    }



    private bool _AddManuallPublicGame = false;
    public bool AddManuallPublicGame
    {
        get { return _AddManuallPublicGame; }
        set { SetProperty(ref _AddManuallPublicGame, value, nameof(AddManuallPublicGame)); }
    }


    public ICommand AddPublicGameCommand { get; }
    public ICommand AddPublicGameToListCommand { get; }
    private void OnAddPublicGameToList(object obj)
    {
        if (System.Net.IPAddress.TryParse(ServerIp, out var adress))
        {
            int.TryParse(ServerPort, out var port);
            PublicGame publicGame = new PublicGame(adress, port, 0, 0, 0, "Manually added public game");
            PublicGames.Add(publicGame);
            SelectedGame = publicGame;
            AddManuallPublicGame = !AddManuallPublicGame;
        }
    }

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
        try
        {
            //abort SearchGamesAsync
            cancellationTokenSourceSearchGamesAsync?.Cancel();
            cancellationTokenSourceSearchGamesAsync?.Dispose();
            cancellationTokenSourceSearchGamesAsync = null;
            if (SelectedGame == null)
            {
                await dialogService.DisplayAsync("Select Game", "Please select the game to join", "ok");
                return;
            }

            gameService.Current = new Models.GameConfig(SelectedGame.GameName, PlayerName, SelectedGame.GameRounds, SelectedGame.RequiredAmountOfPlayers, SelectedGame.ServerIpAddress.ToString(), SelectedGame.ServerPort, 0, new CancellationTokenSource());
#if ANDROID
            await navigationService.NavigateToAsync(typeof(GameMobilePageViewModel), gameService.Current);
#else
            await navigationService.NavigateToAsync(typeof(GamePageViewModel), gameService.Current);
#endif

            PublicGames.Clear();
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
    }
}
