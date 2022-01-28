using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Mqtt
{
    public class Light
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

        [JsonPropertyName("payload_on")]
        public string PayloadOn { get; set; }
        [JsonPropertyName("payload_off")]
        public string PayloadOff { get; set; }

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
            PayloadOn = "on";
            PayloadOff = "off";
            Device = new Device(id, "Niko", "Easywave Switch", name);
            Availability = new Availability();
        }
    }

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