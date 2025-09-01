using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Configuration
{
  [JsonConverter(typeof(JsonStringEnumConverter))]
  // ReSharper disable once MissingXmlDoc
  #pragma warning disable CA1515
  public enum DeviceType
    #pragma warning restore CA1515
  {
    [EnumMember(Value = "unknown")]
    Unknown,
    [EnumMember(Value = "light")]
    Light,
    [EnumMember(Value = "transmitter")]
    Transmitter,
    [EnumMember(Value ="blind")]
    Blind
  }
}