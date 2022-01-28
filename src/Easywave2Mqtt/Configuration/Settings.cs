using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace Easywave2Mqtt.Configuration
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class Settings
  {
    public string? SerialPort { get; set; }
    public int EasywaveActionTimeout { get; set; }
    public int EasywaveRepeatTimeout { get; set; }
    public string? MQTTServer { get; set; }
    public int MQTTPort { get; set; }
    public string? MQTTUser { get; set; }
    public string? MQTTPassword { get; set; }

    public Collection<Device> Devices { get; set; } = new Collection<Device>();
  }
}