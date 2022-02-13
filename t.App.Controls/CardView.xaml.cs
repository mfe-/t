using t.lib.Game;

namespace t.App.Controls;

public partial class CardView : ContentView
{
	public CardView()
	{
		InitializeComponent();
	}
	public static readonly BindableProperty CardProperty = BindableProperty.Create(nameof(Card), typeof(Card), typeof(CardView), default(Card), propertyChanged: CardChanged);

    public Card Card
    {
        get { return (Card)GetValue(CardProperty); }
        set { SetValue(CardProperty, value); }
    }

    public static void CardChanged(BindableObject bindable, object oldValue, object newValue)
    {

    }

}