namespace t.lib
{
    public class EventArgs<T>
    {
        public EventArgs(T data)
        {
            Data = data;
        }

        public T Data { get; }
    }
}
