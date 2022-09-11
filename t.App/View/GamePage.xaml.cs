namespace t.App.View
{
    public partial class GamePage : ContentPage
    {
        public GamePage()
        {
            InitializeComponent();
            Loaded += GamePage_Loaded;
        }

        private void GamePage_Loaded(object? sender, EventArgs e)
        {
            //workaround for https://github.com/dotnet/maui/issues/8525 (requires .net7)
            _animationGrid.ZIndex = 1;
        }
    }
}
