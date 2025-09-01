using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Configuration
{
  [Serializable]
  #pragma warning disable CA1515
  public class Subscription
    #pragma warning restore CA1515
  {
    public string? Address { get; set; }
    public char KeyCode { get; set; }
    public bool CanSend { get; set; }
  }
}