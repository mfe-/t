﻿using Microsoft.Maui.Platform;
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

            Microsoft.Maui.Handlers.ElementHandler.ElementMapper.AppendToMapping("IsMouseOver", (handler, view) =>
            {
#if WINDOWS
                if (view is CardView cardView && handler.PlatformView is ContentPanel contentPanel)
                {
                    contentPanel.PointerEntered += (sender, e) =>
                    {
                        cardView.IsMouseOver = true;
                    };
                    contentPanel.PointerExited += (sender, e) =>
                    {
                        cardView.IsMouseOver = false;
                    };
                }
#endif
            });

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
