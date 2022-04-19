using Microsoft.Maui.Controls;

namespace t.App.View;

public partial class DebugPage : ContentPage
{
	public DebugPage()
	{
		InitializeComponent();
        Loaded += DebugPage_Loaded;
	}

    private void DebugPage_Loaded(object? sender, EventArgs e)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += Tap_Tapped;
        label1.GestureRecognizers.Add(tap);
    }

    private void Tap_Tapped(object? sender, EventArgs e)
    {
        
    }
}