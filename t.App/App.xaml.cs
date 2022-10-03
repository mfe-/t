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
            var navigationPage = serviceProvider.GetService<NavigationPage>();
            var mainPageViewModel = serviceProvider.GetService<MainPageViewModel>();
            if (navigationPage != null) navigationPage.BindingContext = mainPageViewModel;
            MainPage = navigationPage;

          

        }
    }
}
