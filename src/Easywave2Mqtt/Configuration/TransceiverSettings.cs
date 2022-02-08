using JetBrains.Annotations;

namespace Easywave2Mqtt.Configuration
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class TransceiverSettings
  {
    public string? Type { get; set; }
    public string? SerialPort { get; set; }
  }
}
