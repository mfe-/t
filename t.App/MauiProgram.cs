using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using t.App.Service;
using t.App.View;
using t.lib;

namespace t.App
{
    public static class MauiProgram
    {
        public static MauiApp? MauiApp;
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });


            builder.Services.AddSingleton(
                provider =>
                {
                    return new NavigationPage(provider.GetRequiredService<MainPage>())
                    {
                        //BindingContext = provider.GetRequiredService<MainPageViewModel>()
                    };
                });

            AppConfig appConfig = new AppConfig();

            builder.Services.AddSingleton(appConfig);

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddSingleton<MainPageViewModel>();

            builder.Services.AddTransient<GamePage>();
            builder.Services.AddSingleton<GamePageViewModel>();

            builder.Services.AddTransient<NewGamePage>();
            builder.Services.AddSingleton<NewGamePageViewModel>();

            builder.Services.AddTransient<JoinGamePage>();
            builder.Services.AddSingleton<JoinGamePageViewModel>();

            builder.Services.AddTransient<SettingPage>();
            builder.Services.AddSingleton<SettingPageViewModel>();

            builder.Services.AddSingleton(
                provider => new NavigationService(
                provider.GetRequiredService<IServiceProvider>(),
                provider.GetRequiredService<ILogger<NavigationService>>(),
                provider.GetRequiredService<NavigationPage>()));

            MauiApp = builder.Build();

            return MauiApp;
        }
    }
}