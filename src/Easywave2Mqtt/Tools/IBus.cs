namespace Easywave2Mqtt.Tools
{
    public interface IBus
    {
        ISubscription<T> Subscribe<T>(Func<T, Task> handler);

        Task PublishAsync<T>(T message);
    }
}