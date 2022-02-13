using Microsoft.Extensions.Logging;
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

        private Task NavigationService_DisappearedEvent(object? sender, Models.EventArgs<object> e)
        {
            if (sender != this) return Task.CompletedTask;
            return Task.CompletedTask;
        }

        private async Task NavigationService_AppearedEvent(object? sender, Models.EventArgs<object> e)
        {
            if (sender != this) return;
            if (GameClientViewModel == null && gameService.Current != null)
            {
                GameClientViewModel = new GameClientViewModel(logger, new lib.AppConfig());
                GameClientViewModel.Title = $"{gameService.Current.Gamename} Waiting players";
                await gameService.JoinStartedGameServerAsync(GameClientViewModel);

            }
        }

        private GameClientViewModel? _GameClientViewModel = null;
        public GameClientViewModel? GameClientViewModel
        {
            get { return _GameClientViewModel; }
            set { SetProperty(ref _GameClientViewModel, value, nameof(GameClientViewModel)); }
        }
    }
}
