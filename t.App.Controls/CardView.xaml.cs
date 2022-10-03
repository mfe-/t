using System.Linq;
using t.lib.Game;

namespace t.App.Controls;

public partial class CardView : ContentView
{
    public delegate Task EventHandlerAsync<in TEventArgs>(object? sender, TEventArgs e);

    public event EventHandlerAsync<TappedEventArgs>? TappedEvent;

    public CardView()
    {
        PropertyChanged += CardView_PropertyChanged;
    }
    public void RaiseTappedEvent(TappedEventArgs tappedEventArgs)
    {
        TappedEvent?.Invoke(this, tappedEventArgs);
    }

    private void CardView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Bounds))
        {
            ResolveControlTemplate();
        }
    }

    public static readonly BindableProperty CardProperty = BindableProperty.Create(nameof(Card), typeof(Card), typeof(CardView), default(Card));

    public Card Card
    {
        get { return (Card)GetValue(CardProperty); }
        set { SetValue(CardProperty, value); }
    }

    public static readonly BindableProperty IsMouseOverProperty = BindableProperty.Create(nameof(Card), typeof(bool), typeof(CardView), false, propertyChanged: MouseOverPropertyChanged);

    private static void MouseOverPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CardView cardView)
        {
            cardView.Dispatcher.Dispatch(() => cardView.OnMouseOverChanged(cardView.IsMouseOver));
        }
    }
    private async void OnMouseOverChanged(bool isMouseover)
    {
        if (IsEnabled)
        {
            if (isMouseover)
            {
                await this.TranslateTo(0, -TranslationYOffset, 250, Easing.Linear);
                await this.TranslateTo(0, 0, 250, Easing.Linear);
            }
        }
    }

    public async Task HighlightAnimationAsync()
    {
        double scale = this.Scale;

        await this.ScaleTo(scale * 1.5, 250);
        await this.ScaleTo(scale, 250);
    }

    public bool IsMouseOver
    {
        get { return (bool)GetValue(IsMouseOverProperty); }
        set { SetValue(IsMouseOverProperty, value); }
    }


    public static readonly BindableProperty TranslationYOffsetProperty = BindableProperty.Create(nameof(TranslationYOffset), typeof(double), typeof(CardView), 3d);
    /// <summary>
    /// How much additional space the animation requires for Y
    /// </summary>
    public double TranslationYOffset
    {
        get { return (double)GetValue(TranslationYOffsetProperty); }
        set { SetValue(TranslationYOffsetProperty, value); }
    }
    /// <summary>
    /// Calculates how much size the control will require
    /// </summary>
    /// <param name="widthConstraint">the available width for the control</param>
    /// <param name="heightConstraint">the available height for the control</param>
    /// <returns></returns>
    /// <remarks>https://www.dotnetforall.com/understanding-measureoverride-and-arrangeoverride/</remarks>
    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var size = Size.Zero;
        if (WidthRequest == -1 && HeightRequest == -1)
        {
            size = CalculateCardSize(
                widthConstraint == 0 ? double.PositiveInfinity : widthConstraint,
                heightConstraint == 0 ? double.PositiveInfinity : heightConstraint);
        }
        else
        {
            size = new Size(WidthRequest, HeightRequest);
        }
        this.DesiredSize = size;
        base.MeasureOverride(size.Width, size.Height);

        return size;
    }
    /// <summary>
    /// Arrange the control
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    protected override Size ArrangeOverride(Rect bounds)
    {
        double width;
        double height;
        //since we set the DesizredSize in the MeasureOverride method,
        //we use the lower value because its propably from our previous computing using CalculateCardSize
        if (this.DesiredSize.Width < bounds.Width)
        {
            width = this.DesiredSize.Width;
        }
        else
        {
            width = bounds.Width;
        }
        if (this.DesiredSize.Height < bounds.Height)
        {
            height = DesiredSize.Height;
        }
        else
        {
            height = bounds.Height;
        }

        this.DesiredSize = new Size(width, height);

        var rect = new Rect(bounds.X, bounds.Y, width, height);

        base.ArrangeOverride(rect);
        return new Size(width, height);
    }
    /// <summary>
    /// calculates the size of the control
    /// </summary>
    /// <param name="widthAvailable"></param>
    /// <param name="heightAvailable"></param>
    /// <returns></returns>
    private Size CalculateCardSize(double widthAvailable, double heightAvailable)
    {
        const double defaultWidth = 55;
        const double defaultHeight = 85;
        double width = 0;
        double height = 0;

        if (widthAvailable > heightAvailable)
        {
            var ratioh = defaultHeight / defaultWidth;
            width = heightAvailable / ratioh;
            height = heightAvailable;
        }
        else
        {
            var ratioh = defaultHeight / defaultWidth;
            height = widthAvailable;
            width = widthAvailable / ratioh;
        }
        return new Size(width, height);
    }
}