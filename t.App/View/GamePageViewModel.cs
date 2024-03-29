﻿using Microsoft.Extensions.Logging;
using t.App.Service;

namespace t.App.View
{
    internal class GamePageViewModel : BaseViewModel
    {
        private readonly NavigationService navigationService;
        private readonly GameService gameService;
        private readonly DialogService dialogService;

        public GamePageViewModel(ILogger<GamePageViewModel> logger, NavigationService navigationService, GameService gameService, DialogService dialogService) : base(logger)
        {
            this.navigationService = navigationService;
            this.gameService = gameService;
            this.dialogService = dialogService;
            this.navigationService.AppearedEvent += NavigationService_AppearedEvent;
            this.navigationService.PageLeftEvent += NavigationService_DisappearedEvent;
        }

        private Task NavigationService_DisappearedEvent(object? sender, Models.EventArgs<object> e)
        {
            if (sender != this) return Task.CompletedTask;

            //we exit the page
            //stop client
            GameClientViewModel?.Dispose();
            GameClientViewModel = null;
            //stop server instance if one was started
            if (gameService.Current != null)
            {
                gameService.Current.CancellationTokenServer.Cancel();
            }

            return Task.CompletedTask;
        }

        private async Task NavigationService_AppearedEvent(object? sender, Models.EventArgs<object> e)
        {
            if (sender != this) return;
            if (GameClientViewModel == null && gameService.Current != null)
            {
                GameClientViewModel = new GamePageClientViewModel(logger, navigationService, new lib.AppConfig(), dialogService);
                GameClientViewModel.Title = $"{gameService.Current.Gamename} Waiting players";
                await gameService.JoinStartedGameServerAsync(GameClientViewModel);

            }
        }

        private GamePageClientViewModel? _GameClientViewModel = null;
        public GamePageClientViewModel? GameClientViewModel
        {
            get { return _GameClientViewModel; }
            set { SetProperty(ref _GameClientViewModel, value, nameof(GameClientViewModel)); }
        }
    }
}
