using System.Collections.Concurrent;

namespace Easywave2Mqtt.Tools
{
    public class Bus : IBus
    {
        private readonly ConcurrentDictionary<Type, IList<ISubscription>> _subscriptions = new();

        public async Task PublishAsync<T>(T message)
        {
            foreach (ISubscription sub in _subscriptions[typeof(T)])
            {
                if (sub is ISubscription<T> subscription)
                {
                    await subscription.Handle(message);
                }
            }
        }

        public ISubscription<T> Subscribe<T>(Func<T, Task> handler)
        {
            Type? type = typeof(T);
            var subscription = new Subscription<T>(this, handler);
            IList<ISubscription>? list = _subscriptions.GetOrAdd(type, (_) => new List<ISubscription>());
            list.Add(subscription);
            return subscription;
        }

        private void Remove<T>(ISubscription subscription)
        {
            _ = _subscriptions[typeof(T)].Remove(subscription);
        }

        private class Subscription<T> : ISubscription<T>
        {
            private readonly Bus _parent;
            private readonly Func<T, Task> _handler;

            public Subscription(Bus parent, Func<T, Task> handler)
            {
                _parent = parent;
                _handler = handler;
            }

            public void Dispose()
            {
                _parent.Remove<T>(this);
            }

            public Task Handle(T message)
            {
                return _handler.Invoke(message);
            }
        }
    }
}