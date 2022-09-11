using Microsoft.Maui.Platform;
using t.App.Controls;
using t.App.View;

namespace t.App
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            //var navigationPage = serviceProvider.GetService<NavigationPage>();
            var navigationPage = serviceProvider.GetService<AppShell>();
            navigationPage.BindingContext = serviceProvider.GetService<AppShellViewModel>();
            var mainPageViewModel = serviceProvider.GetService<MainPageViewModel>();
            if (navigationPage != null) navigationPage.ShellContent.BindingContext = mainPageViewModel;
            MainPage = navigationPage;

          

        }
    }
}
