using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Mqtt
{
  public class Device(string id, string manufacturer, string model, string name)
  {
    private const string Version = "Easywave2Mqtt 0.1 beta";

    [JsonPropertyName("identifiers")]
    public string[] Identifiers { get; set; } = [$"easywave2mqtt_{id}"];
    [JsonPropertyName("name")]
    public string Name { get; set; } = name;
    [JsonPropertyName("sw_version")]
    public string SoftwareVersion { get; set; } = Version;
    [JsonPropertyName("model")]
    public string Model { get; set; } = model;
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = manufacturer;
  }
}