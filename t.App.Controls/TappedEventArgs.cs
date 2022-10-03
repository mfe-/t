namespace t.App.Controls;

public class TappedEventArgs : EventArgs
{
    public TappedEventArgs(bool doubleTapped)
    {
        DoubleTapped = doubleTapped;
    }
    public bool DoubleTapped { get; private set; }
}
