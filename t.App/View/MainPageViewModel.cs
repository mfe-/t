using Microsoft.Extensions.Logging;
using System.Windows.Input;
using t.App.Service;
using t.lib.Game;

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
        public String Title { get; set; } = "";

        public ICommand NavigateCommand { get; }



        private Card _Card = new Card(1);
        public Card Card
        {
            get { return _Card; }
            set { SetProperty(ref _Card, value, nameof(Card)); }
        }

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

        public bool ShowDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
    }
}
