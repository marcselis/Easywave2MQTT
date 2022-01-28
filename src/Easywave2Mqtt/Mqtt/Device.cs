using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Mqtt
{
    public class Device
    {
        private const string Version = "Easywave2Mqtt 0.1 beta";

        public Device(string id, string manufacturer, string model, string name)
        {
            SoftwareVersion = Version;
            Manufacturer = manufacturer;
            Model = model;
            Name = name;
            Identifiers = new[] { $"easywave2mqtt_{id}" };
        }

        [JsonPropertyName("identifiers")]
        public string[] Identifiers { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("sw_version")]
        public string SoftwareVersion { get; set; }
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; }
    }
}