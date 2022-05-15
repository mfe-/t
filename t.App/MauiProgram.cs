using Microsoft.Extensions.Logging;
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
                //fonts.AddFont("Halogen-Bold.ttf", "HalogenBold");
                fonts.AddEmbeddedResourceFont(typeof(Controls.CardView).Assembly, "Halogen-Bold.ttf", "HalogenBold");
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
        appConfig.ServerPort = 12000;
        appConfig.BroadcastPort = 15000;

        builder.Services.AddSingleton(appConfig);

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<MainPageViewModel>();

        builder.Services.AddTransient<GamePage>();
        builder.Services.AddSingleton<GamePageViewModel>();

        builder.Services.AddTransient<NewGamePage>();
        builder.Services.AddTransient<NewGamePageViewModel>();

        builder.Services.AddTransient<JoinGamePage>();
        builder.Services.AddTransient<JoinGamePageViewModel>();

        builder.Services.AddTransient<SettingPage>();
        builder.Services.AddSingleton<SettingPageViewModel>();

        builder.Services.AddTransient<DebugPage>();
        builder.Services.AddTransient<DebugPageViewModel>();

        builder.Services.AddSingleton(
            provider => new NavigationService(
            provider.GetRequiredService<IServiceProvider>(),
            provider.GetRequiredService<ILogger<NavigationService>>(),
            provider.GetRequiredService<NavigationPage>()));

        builder.Services.AddSingleton<GameService>();

        return builder.Build();
    }
}