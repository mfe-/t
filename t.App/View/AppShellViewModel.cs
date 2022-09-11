using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using t.App.Service;

namespace t.App.View
{
    public class AppShellViewModel : BaseViewModel
    {
        private readonly NavigationService navigationService;

        public AppShellViewModel(ILogger<AppShellViewModel> logger, NavigationService navigationService) : base(logger)
        {
            this.navigationService = navigationService;
            GoBackCommand = new Command<string>(async (s) => await GoBackAsync(s));
        }

        public ICommand GoBackCommand { get; }

        protected virtual Task GoBackAsync(string param)
        {
            return Task.CompletedTask;
        }
    }
}
