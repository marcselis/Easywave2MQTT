namespace Easywave2Mqtt.Easywave
{
  public interface IEasywaveDevice
  {
    string Id { get; }
    Task HandleCommand(string command);
  }

  public interface IEasywaveEventListener : IEasywaveDevice
  {
    Task HandleEvent(string address, char keyCode, string action);
  }
}
