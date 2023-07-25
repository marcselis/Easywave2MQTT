using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Mqtt
{
  public class Cover
  {
    [JsonPropertyName("unique_id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("suggested_area")]
    public string? Area { get; set; }

    [JsonPropertyName("state_topic")]
    public string StateTopic { get; set; }

    [JsonPropertyName("command_topic")]
    public string CommandTopic { get; set; }

    [JsonPropertyName("payload_close")]
    public string PayloadClose { get; set; }

    [JsonPropertyName("payload_open")]
    public string PayloadOpen { get; set; }

    [JsonPropertyName("payload_stop")]
    public string PayloadStop { get; set; }

    [JsonPropertyName("device")]
    public Device Device { get; set; }

    [JsonPropertyName("availability")]
    public Availability Availability { get; set; }

    public Cover(string id, string name, string? area)
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
      PayloadClose = "CLOSE";
      PayloadOpen = "OPEN";
      PayloadStop = "STOP";
      Device = new Device(id, "Niko", "Easywave Blind", name);
      Availability = new Availability();
    }
  }
}