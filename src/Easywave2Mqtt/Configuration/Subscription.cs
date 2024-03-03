using JetBrains.Annotations;

namespace Easywave2Mqtt.Configuration
{
  public class Subscription
  {
    public string? Address { get; set; }
    public char KeyCode { get; set; }
    public bool CanSend { get; set; }
  }
}