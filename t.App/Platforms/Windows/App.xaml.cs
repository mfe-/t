﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using t.App.View;
using Windows.ApplicationModel;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace t.App.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            Microsoft.Maui.Essentials.Platform.OnLaunched(args);
            //https://stackoverflow.com/a/70419653/740651
            var currentWindow = Application.Windows[0]?.Handler?.NativeView;
            IntPtr _windowHandle = WindowNative.GetWindowHandle(currentWindow);
            var windowId = Win32Interop.GetWindowIdFromWindow(_windowHandle);

            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Title = Services.GetService<MainPageViewModel>()?.Title;
        }
    }
}
