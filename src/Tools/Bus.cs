using System.Collections.Concurrent;

namespace InMemoryBus
{
  public class Bus : IBus
  {
    private readonly ConcurrentDictionary<Type, ICollection<ISubscription>> _subscriptions = new();

    public async Task PublishAsync<T>(T message)
    {
      if (_subscriptions.TryGetValue(typeof(T), out ICollection<ISubscription>? list))
      {
        foreach (ISubscription sub in list)
        {
          if (sub is ISubscription<T> subscription)
          {
            await subscription.Handle(message).ConfigureAwait(false);
          }
        }
      }
    }

    public ISubscription<T> Subscribe<T>(Func<T, Task> handler)
    {
      Type type = typeof(T);
      var subscription = new Subscription<T>(this, handler);
      ICollection<ISubscription> list = _subscriptions.GetOrAdd(type, _ => []);
      list.Add(subscription);
      return subscription;
    }

    private void Remove<T>(ISubscription subscription)
    {
      _ = _subscriptions[typeof(T)].Remove(subscription);
    }

    private sealed class Subscription<T>(Bus parent, Func<T, Task> handler) : ISubscription<T>
    {
      public void Dispose()
      {
        parent.Remove<T>(this);
      }

      public Task Handle(T message)
      {
        return handler.Invoke(message);
      }
    }
  }
}