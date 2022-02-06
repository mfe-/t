using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using t.App.Service;
using t.App.View;

namespace t.App
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            var navigationPage = serviceProvider.GetService<NavigationPage>();
            var mainPageViewModel = serviceProvider.GetService<MainPageViewModel>();
            if (navigationPage != null) navigationPage.BindingContext = mainPageViewModel;
            MainPage = navigationPage;

        }
        //protected override Window CreateWindow(IActivationState activationState)
        //{
        //    //object v = ServiceProvider.GetService<MainPage>();
        //    MainPage page = new MainPage();

        //    var navigationPage = new NavigationPage(page)
        //    {

        //    };

        //    Window window = new Window();
        //    window.Page = navigationPage;

        //    return window;
        //}
    }
}
