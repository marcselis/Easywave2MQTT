using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Mqtt
{
  public class Cover
  {
    public const string ClosingState = "closing";
    public const string ClosedState = "closed";
    public const string OpeningState = "opening";
    public const string OpenState = "open";
    public const string StoppedState = "stopped";
    public const string OpenCommand = "OPEN";
    public const string CloseCommand = "CLOSE";
    public const string StopCommand = "STOP";

    [JsonPropertyName("unique_id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("suggested_area")]
    public string? Area { get; set; }

    [JsonPropertyName("command_topic")]
    public string CommandTopic { get; set; }

    [JsonPropertyName("state_topic")]
    public string StateTopic { get; set; }

    [JsonPropertyName("qos")]
    public uint QoS { get; set; } = 0;

    [JsonPropertyName("retain")]
    public bool Retain { get; set; } = true;

    [JsonPropertyName("payload_open")]
    public string PayloadOpen { get; set; } = OpenCommand;

    [JsonPropertyName("payload_close")]
    public string PayloadClose { get; set; } = CloseCommand;

    [JsonPropertyName("payload_stop")]
    public string PayloadStop { get; set; } = StopCommand;

    [JsonPropertyName("state_open")]
    public string StateOpen { get; set; } = OpenState;

    [JsonPropertyName("state_opening")]
    public string StateOpening { get; set; } = OpeningState;

    [JsonPropertyName("state_closed")]
    public string StateClosed { get; set; } = ClosedState;

    [JsonPropertyName("state_closing")]
    public string StateClosing { get; set; } = ClosingState;

    [JsonPropertyName("state_stopped")]
    public string StateStopped { get; set; } = StoppedState;

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
      CommandTopic = $"mqtt2easywave/{id}/set";
      StateTopic = $"easywave2mqtt/{id}/state";
      Device = new Device(id, "Niko", "Easywave Blind", name);
      Availability = new Availability();
    }
  }
}