namespace t.App.Models;

public class EventArgs<T> : EventArgs
{
    public EventArgs(T? data) : base()
    {
        Data = data;
    }
    public T? Data { get; }
}
