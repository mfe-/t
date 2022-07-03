#if ANDROID
using Android.Content;
using Android.Views;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Controls;

public partial class CardViewGroup : ContentViewGroup
{
    private readonly CardView cardView;

    public CardViewGroup(CardView cardView, Context context) : base(context)
    {
        this.cardView = cardView;
    }
    private DateTime startTime;
    //constant for defining the time duration between the click that can be considered as double-tap
    private const int MAX_DURATION = 200;
    public override bool OnInterceptTouchEvent(MotionEvent? ev)
    {
        if (ev == null) return false;

        if (ev.Action == Android.Views.MotionEventActions.Up)
        {
            startTime = DateTime.Now;
            cardView.RaiseTappedEvent(new Controls.TappedEventArgs(false));
            return true;
        }
        else if (ev.Action == Android.Views.MotionEventActions.Down)
        {
            var timeSpan = DateTime.Now.Subtract(startTime);

            if (timeSpan.TotalMilliseconds < MAX_DURATION)
            {
                startTime = DateTime.Now;
                cardView.RaiseTappedEvent(new Controls.TappedEventArgs(true));
                return true;
            }
        }
        return base.OnInterceptTouchEvent(ev);
    }
}
#endif