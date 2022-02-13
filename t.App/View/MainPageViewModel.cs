using Microsoft.Extensions.Logging;
using System.Windows.Input;
using t.App.Service;

namespace t.App.View
{
    public class MainPageViewModel : BaseViewModel
    {
        private new readonly ILogger<MainPageViewModel> logger;
        private readonly NavigationService navigationService;

        public MainPageViewModel(ILogger<MainPageViewModel> logger, NavigationService navigationService)
            : base(logger)
        {
            this.logger = logger;
            this.navigationService = navigationService;
            NavigateCommand = new Command<string>(async (s) => await NavigateAsync(s));
            ExitCommand = new Command(Exit);
        }
        public String Title { get; set; } = "t.App";

        public ICommand NavigateCommand { get; }

        private async Task NavigateAsync(string uri)
        {
            try
            {
                var t = Type.GetType(uri);
                if (t == null) throw new InvalidOperationException($"Could not resolve {t}");
                await navigationService.NavigateToAsync(t);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, nameof(NavigateAsync));
            }
        }

        public ICommand ExitCommand { get; }
        private void Exit() => Environment.Exit(0);
    }
}
