using Android.App;
using Android.Content.PM;
using Microsoft.Maui;
using t.App.Service;

namespace t.App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : MauiAppCompatActivity
    {
        public override void OnBackPressed()
        {
            var navigationService = MauiApplication.Current.Services.GetRequiredService<NavigationService>();
            navigationService.LeavePage(navigationService.CurrentPage);
            base.OnBackPressed();
        }
    }
}