using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib.Messaging
{
    public class EventAggregator
    {
        private Dictionary<Type, IList> subscriber;

        public EventAggregator()
        {
            subscriber = new Dictionary<Type, IList>();
        }

        public async Task PublishAsync<TMessageType>(TMessageType message)
        {
            Type t = typeof(TMessageType);
            IList actionlst;
            if (subscriber.ContainsKey(t))
            {
                actionlst = new List<Subscription<TMessageType>>(subscriber[t].Cast<Subscription<TMessageType>>());

                foreach (Subscription<TMessageType> a in actionlst)
                {
                    await a.Action(message);
                }
            }
        }

        public Subscription<TMessageType> Subscribe<TMessageType>(Func<TMessageType, Task> action)
        {
            Type t = typeof(TMessageType);
            IList actionlst;
            var actiondetail = new Subscription<TMessageType>(action, this);

            if (!subscriber.TryGetValue(t, out actionlst))
            {
                actionlst = new List<Subscription<TMessageType>>();
                actionlst.Add(actiondetail);
                subscriber.Add(t, actionlst);
            }
            else
            {
                actionlst.Add(actiondetail);
            }

            return actiondetail;
        }

        public void UnSbscribe<TMessageType>(Subscription<TMessageType> subscription)
        {
            Type t = typeof(TMessageType);
            if (subscriber.ContainsKey(t))
            {
                subscriber[t].Remove(subscription);
            }
        }

    }
}
