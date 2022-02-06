using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.App.Service;

namespace t.App.View
{
    internal class GamePageViewModel : BaseViewModel
    {
        private readonly NavigationService navigationService;
        private readonly GameService gameService;

        public GamePageViewModel(ILogger<GamePageViewModel> logger, NavigationService navigationService, GameService gameService) : base(logger)
        {
            this.navigationService = navigationService;
            this.gameService = gameService;
            this.navigationService.AppearedEvent += NavigationService_AppearedEvent;
            this.navigationService.DisappearedEvent += NavigationService_DisappearedEvent;

        }
        public string Title { get; set; } = "Game";
        private Task NavigationService_DisappearedEvent(object? sender, EventArgs e)
        {
            if (sender != this) return Task.CompletedTask;
            return Task.CompletedTask;
        }

        private async Task NavigationService_AppearedEvent(object? sender, EventArgs e)
        {
            if (sender != this) return;
            if (GameClientViewModel == null)
            {
                GameClientViewModel = new GameClientViewModel(logger, new lib.AppConfig(), null);
                await gameService.JoinStartedGameServerAsync(GameClientViewModel);
            }
        }

        public GameClientViewModel? GameClientViewModel { get; set; }
    }
}
