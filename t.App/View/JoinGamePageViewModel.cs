using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace t.App.View;

internal class JoinGamePageViewModel : BaseViewModel
{
    public JoinGamePageViewModel(ILogger<JoinGamePageViewModel> logger) : base(logger)
    {
        JoinGameCommand = new Command(OnJoinGame);
    }

    public String Title { get; set; } = "Join a game";


    private string _ServerIp="10.51.51.100";
    public string ServerIp
    {
        get { return _ServerIp; }
        set { SetProperty(ref _ServerIp, value, nameof(ServerIp)); }
    }


    private string _ServerPort="12000";
    public string ServerPort
    {
        get { return _ServerPort; }
        set { SetProperty(ref _ServerPort, value, nameof(ServerPort)); }
    }


    private string _PlayerName="";
    public string PlayerName
    {
        get { return _PlayerName; }
        set { SetProperty(ref _PlayerName, value, nameof(PlayerName)); }
    }


    public ICommand JoinGameCommand { get; }
    private void OnJoinGame()
    {
        
    }
}
