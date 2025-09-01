namespace Easywave2Mqtt.Easywave
{
  internal interface IEasywaveDevice
  {
    string Id { get; }
    Task HandleCommand(string command);
  }

  internal interface IEasywaveEventListener : IEasywaveDevice
  {
    Task HandleEvent(string address, char keyCode, string action);
  }
}
