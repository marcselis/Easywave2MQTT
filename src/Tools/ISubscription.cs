namespace InMemoryBus
{
  public interface ISubscription : IDisposable
  { }

  public interface ISubscription<in T> : ISubscription
  {
    Task Handle(T message);
  }
}