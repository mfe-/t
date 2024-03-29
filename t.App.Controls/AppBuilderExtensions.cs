﻿using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
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
    /// <returns><see cref="MauiAppBuilder"/> initialized for <see cref="t.App.Controls"/></returns>
    public static MauiAppBuilder UseControls(this MauiAppBuilder builder)
    {

#if ANDROID
        ContentViewGroup ContentViewFactory(ViewHandler<IContentView, ContentViewGroup> arg)
        {
            if (arg.VirtualView is CardView contentcontrols)
            {
                return new CardViewGroup(contentcontrols, arg.Context);
            }
            //return null to force a fall back 
            return null;
        }
        ContentViewHandler.PlatformViewFactory = ContentViewFactory;
#endif


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
