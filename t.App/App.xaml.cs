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

#if ANDROID
            Microsoft.Maui.Handlers.ElementHandler.ElementMapper.AppendToMapping("OnTouch", (handler, view) =>
            {
                if (view is CardView cardView && handler.PlatformView is ContentViewGroup contentViewGroup)
                {
                    var children = VisualTreeHelper.GetAllChildren(contentViewGroup);
                    var myTouchListener = new MyTouchListener(cardView);
                    foreach (var child in children)
                    {
                        child.SetOnTouchListener(myTouchListener);
                    }
                }
            });
#endif

            Microsoft.Maui.Handlers.ButtonHandler.ElementMapper.AppendToMapping("ButtonMouseOver", (handler, view) =>
            {
#if WINDOWS
                if (view is Button button && handler.PlatformView is MauiButton mauiButton)
                {
                    mauiButton.PointerEntered += (sender, e) =>
                    {
                        button.BorderWidth = 1;
                        button.BackgroundColor = Color.FromHex("#FFFFFA");
                    };
                    mauiButton.PointerExited += (sender, e) =>
                    {
                        button.BorderWidth = 0;
                        button.BackgroundColor = Color.FromHex("#F3EFE5");
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

#if ANDROID
    public class MyTouchListener : Java.Lang.Object, Android.Views.View.IOnTouchListener
    {
        private readonly CardView cardView;

        public MyTouchListener(CardView cardView)
        {
            this.cardView = cardView;
        }
        private DateTime startTime;
        //constant for defining the time duration between the click that can be considered as double-tap
        private const int MAX_DURATION = 200;
        public bool OnTouch(Android.Views.View? v, Android.Views.MotionEvent? e)
        {
            if (v == null || e == null) return false;

            if (e.Action == Android.Views.MotionEventActions.Up)
            {
                startTime = DateTime.Now;
                cardView.RaiseTappedEvent(new Controls.TappedEventArgs(false));
                return true;
            }
            else if (e.Action == Android.Views.MotionEventActions.Down)
            {
                var timeSpan = DateTime.Now.Subtract(startTime);
                
                if (timeSpan.TotalMilliseconds < MAX_DURATION)
                {
                    startTime = DateTime.Now;
                    cardView.RaiseTappedEvent(new Controls.TappedEventArgs(true));
                    return true;
                }
            }


            return false;
        }
    }
#endif
}
