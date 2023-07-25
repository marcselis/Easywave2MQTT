using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Mqtt
{
  public class Availability
  {
    [JsonPropertyName("payload_available")]
    public string PayloadAvailable { get; set; } = "available";
    [JsonPropertyName("payload_not_available")]
    public string PayloadNotAvailable { get; set; } = "unavailable";
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = "easywave2mqtt";
  }
}