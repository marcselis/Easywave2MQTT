using Easywave2Mqtt.Messages;

namespace Easywave2Mqtt.Easywave
{
  public interface IEasywaveTransceiver
  {
    bool IsOpen { get; }

    void Open();
    void Close();
    EasywaveTelegram ReadLine();
    Task SendEasywaveCommand(SendEasywaveCommand message);
  }
}