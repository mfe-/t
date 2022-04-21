using System.Linq;
using t.lib.Game;

namespace t.App.Controls;

public partial class CardView : ContentView
{

    public CardView()
    {
        InitializeComponent();
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
            else
            {
                //BackgroundColor = Colors.Yellow;
            }
        }
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


    //https://www.dotnetforall.com/understanding-measureoverride-and-arrangeoverride/
    //protected override Size ArrangeOverride(Rect bounds)
    //{
    //    var size = base.ArrangeOverride(bounds);

    //    //we only need 1/3 of the width
    //    var newSize = new Size(size.Height * (0.3d), size.Height);

    //    // Get the collection of children
    //    var mychildren = Children;

    //    // Get total number of children
    //    int count = mychildren.Count;

    //    // Arrange children
    //    if (count > 1) throw new NotSupportedException();

    //    var child = mychildren.First();
    //    if (child is VisualElement visual)
    //    {
    //        visual.Arrange(new Rect(0,0,newSize.Width,newSize.Height));
    //        //if(visual is ContentView contentView)
    //        //{
    //        //    contentView.DesiredSize
    //        //    contentView.Arrange(new Rect(0, 0, newSize.Width, newSize.Height));
    //        //}
    //    }

    //    // Return final size of the panel
    //    return newSize;
    //}
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        //VisualStateManager.GoToState .GoToElementState(rect, "MouseEnter", true);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
    }
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
    }

}