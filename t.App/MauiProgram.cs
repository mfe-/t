using Microsoft.Extensions.Logging;
using t.App.Controls;
using t.App.Service;
using t.App.View;
using t.lib;

namespace t.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("NunitoSans-Bold.ttf", "NunitoSansBold");
                fonts.AddFont("NunitoSans-Regular.ttf", "NunitoSansRegular");
            });

        builder.UseControls();

        builder.Services.AddSingleton(
            provider =>
            {
                return new NavigationPage(provider.GetRequiredService<MainPage>())
                {
                    //BindingContext = provider.GetRequiredService<MainPageViewModel>()
                };
            });

        AppConfig appConfig = new AppConfig();
        appConfig.ServerPort = 12000;
        appConfig.BroadcastPort = 15000;

        builder.Services.AddSingleton(appConfig);

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<MainPageViewModel>();

        builder.Services.AddTransient<GamePage>();
        builder.Services.AddSingleton<GamePageViewModel>();

#if ANDROID
        builder.Services.AddTransient<GameMobilePage>();
        builder.Services.AddTransient<GameMobilePageViewModel>();
#endif

        builder.Services.AddTransient<NewGamePage>();
        builder.Services.AddTransient<NewGamePageViewModel>();

        builder.Services.AddTransient<JoinGamePage>();
        builder.Services.AddSingleton<JoinGamePageViewModel>();

        builder.Services.AddTransient<SettingPage>();
        builder.Services.AddSingleton<SettingPageViewModel>();

        builder.Services.AddTransient<DebugPage>();
        builder.Services.AddTransient<DebugPageViewModel>();

        builder.Services.AddTransient<DebugPageMobile>();
        builder.Services.AddSingleton<DebugPageMobileViewModel>();

        builder.Services.AddSingleton(
            provider => new NavigationService(
            provider.GetRequiredService<IServiceProvider>(),
            provider.GetRequiredService<ILogger<NavigationService>>(),
            provider.GetRequiredService<NavigationPage>()));

        builder.Services.AddSingleton<GameService>();
        builder.Services.AddTransient<DialogService>();

        builder.Services.AddLogging(logbuilder =>
        {
#if DEBUG
            logbuilder.SetMinimumLevel(LogLevel.Trace);
            logbuilder.AddOutput();
#endif
        });

        return builder.Build();
    }
}