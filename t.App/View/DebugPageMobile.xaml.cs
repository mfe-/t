namespace t.App.View;

public partial class DebugPageMobile : ContentPage
{
    public DebugPageMobile()
    {
        InitializeComponent();
        Loaded += DebugPageMobile_Loaded;
    }

    private void DebugPageMobile_Loaded(object? sender, EventArgs e)
    {
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Parent = card;
        tapGestureRecognizer.Tapped += TapGestureRecognizer_Tapped; ;
        card.GestureRecognizers.Add(tapGestureRecognizer);
    }

    private void TapGestureRecognizer_Tapped(object? sender, EventArgs e)
    {

    }
}