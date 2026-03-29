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
        var tasks = list.Where(s => s is ISubscription<T>).Cast<ISubscription<T>>().Select(s => s.Handle(message)).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
      }
    }

    public ISubscription<T> Subscribe<T>(Func<T, Task> handler)
    {
      var type = typeof(T);
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
        return handler(message);
      }
    }
  }
}