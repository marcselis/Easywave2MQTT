using System.Text.Json.Serialization;
using Easywave2Mqtt.Easywave;

namespace Easywave2Mqtt.Mqtt
{
  public class Light
  {
    public const string OnCommand = "ON";
    public const string OffCommand = "OFF";

    [JsonPropertyName("unique_id")]
    public string Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("suggested_area")]
    public string? Area { get; set; }

    [JsonPropertyName("qos")]
    public uint QoS { get; set; } = 1;

    [JsonPropertyName("retain")]
    public bool Retain { get; set; } = true;

    [JsonPropertyName("state_topic")]
    public string StateTopic { get; set; }

    [JsonPropertyName("command_topic")]
    public string CommandTopic { get; set; }

    [JsonPropertyName("payload_on")]
    public string PayloadOn { get; set; } = OnCommand;
    [JsonPropertyName("payload_off")]
    public string PayloadOff { get; set; } = OffCommand;

    [JsonPropertyName("device")]
    public Device Device { get; set; }
    [JsonPropertyName("availability")]
    public Availability Availability { get; set; }

    public Light(string id, string name, string? area)
    {
      if (id.Length > 6)
      {
        throw new ArgumentOutOfRangeException(nameof(id), "Maximum size is 6 characters");
      }
      Id = id;
      Name = name;
      Area = area;
      StateTopic = $"easywave2mqtt/{id}/state";
      CommandTopic = $"mqtt2easywave/{id}/set";
      Device = new Device(id, "Niko", "Easywave Switch", name);
      Availability = new Availability();
    }
  }
}