using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Mqtt
{
    public class Button
    {
        [JsonPropertyName("unique_id")]
        public string Id { get; set; }
        [JsonPropertyName("automation_type")]
        public string AutomationType { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("subtype")]
        public string? SubType { get; set; }
        [JsonPropertyName("payload")]
        public string? Payload { get; set; }
        [JsonPropertyName("topic")]
        public string? Topic { get; set; }
        [JsonPropertyName("device")]
        public Device? Device { get; set; }

        [JsonPropertyName("suggested_area")]
        public string? Area { get; set; }
        [JsonPropertyName("availability")]
        public Availability Availability { get; set; }

        public Button(string id, char btn, string name, string? area, string eventName, int count)
        {
            Id = id + btn;
            Availability = new Availability();
            AutomationType = "trigger";
            Type = $"button_{eventName}";
            SubType = $"button_{btn}";
            Payload = $"button_{btn}_{eventName}";
            Topic = $"easywave2mqtt/{id}/action";
            Area = area;
            Device = new Device(id, "Niko", $"Easywave {count}-button Transmitter", name);
        }
    }
}