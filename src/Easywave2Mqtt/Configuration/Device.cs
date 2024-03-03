using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace Easywave2Mqtt.Configuration
{
  public class Device
  {
    public string? Id { get; set; }
    public DeviceType Type { get; set; } = DeviceType.Unknown;
    public string? Name { get; set; }
    public string? Area { get; set; }
    public bool IsToggle { get; set; }

    public Collection<char> Buttons { get; set; } = [];
    public Collection<Subscription> Subscriptions { get; } = [];
  }
}
