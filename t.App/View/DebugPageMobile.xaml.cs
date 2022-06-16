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
        label.Text = $"grid.w {grid1.DesiredSize} grid.h {grid1.Height} ";
    }
}