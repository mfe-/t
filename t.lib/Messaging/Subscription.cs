using System;
using System.Threading.Tasks;

namespace t.lib.Messaging
{
    //Does used by EventAggregator to reserve subscription  
    public class Subscription<Tmessage> : IDisposable
    {
        public Func<Tmessage, Task> Action { get; private set; }
        private readonly EventAggregator EventAggregator;
        private bool isDisposed;
        public Subscription(Func<Tmessage, Task> action, EventAggregator eventAggregator)
        {
            Action = action;
            EventAggregator = eventAggregator;
        }

        ~Subscription()
        {
            if (!isDisposed)
                Dispose();
        }

        public void Dispose()
        {
            EventAggregator.UnSbscribe(this);
            isDisposed = true;
        }
    }
}