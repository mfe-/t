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
