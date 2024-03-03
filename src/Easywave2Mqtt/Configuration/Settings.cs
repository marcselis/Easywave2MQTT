using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Serilog.Events;

namespace Easywave2Mqtt.Configuration
{
  public class Settings
  {
    public LogEventLevel LogLevel { get; set; }
    public string? SerialPort { get; set; }
    public int EasywaveActionTimeout { get; set; }
    public int EasywaveRepeatTimeout { get; set; }
    public string? MQTTServer { get; set; }
    public int MQTTPort { get; set; }
    public string? MQTTUser { get; set; }
    public string? MQTTPassword { get; set; }

    public Collection<Device> Devices { get; set; } = [];
  }
}