using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Easywave2Mqtt.Configuration
{
  [Serializable]
  #pragma warning disable CA1515
  public class Device
    #pragma warning restore CA1515
  {
    public string? Id { get; set; }
    public DeviceType Type { get; set; } = DeviceType.Unknown;
    public string? Name { get; set; }
    public string? Area { get; set; }
    public bool IsToggle { get; set; }

    public Collection<char> Buttons { get; } = [];
    public Collection<Subscription> Subscriptions { get; } = [];
  }
}
