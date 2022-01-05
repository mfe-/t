using Microsoft.Extensions.Logging;

namespace t.App.View;

internal class JoinGamePageViewModel : BaseViewModel
{
    public JoinGamePageViewModel(ILogger<JoinGamePageViewModel> logger) : base(logger)
    {
    }
    public String Title { get; set; } = "Join a game";
}
