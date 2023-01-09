namespace InMemoryBus
{
  public interface IBus
  {
    ISubscription<T> Subscribe<T>(Func<T, Task> handler);

    Task PublishAsync<T>(T message);
  }
}