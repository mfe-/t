using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.App.Service;

namespace t.App.View;

internal class GameMobilePageViewModel : GamePageViewModel
{
    public GameMobilePageViewModel(ILogger<GamePageViewModel> logger, NavigationService navigationService, GameService gameService) : base(logger, navigationService, gameService)
    {
    }
}
