﻿using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Controls;

/// <summary>
/// Extensions for MauiAppBuilder
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Initializes the t.App.Controls lib
    /// </summary>
    /// <param name="builder"><see cref="MauiAppBuilder"/> generated by <see cref="MauiApp"/> </param>
    /// <returns><see cref="MauiAppBuilder"/> initialized for <see cref="CommunityToolkit.Maui"/></returns>
    public static MauiAppBuilder UseControls(this MauiAppBuilder builder)
    {
        //builder.ConfigureMauiHandlers(h =>
        //{
        //    h.AddHandler<DrawingView, DrawingViewHandler>();
        //    h.AddHandler<Popup, PopupHandler>();
        //});

        builder.ConfigureFonts(fonts =>
        {
            fonts.AddEmbeddedResourceFont(typeof(Controls.CardView).Assembly, "Halogen-Bold.ttf", "HalogenBold");
        });

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

        return builder;
    }
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
