// See https://aka.ms/new-console-template for more information
using System.Text.Json.Serialization;

public class Device
{
    const string version = "Easywave2Mqtt 0.1 beta";

    public Device(uint id, string manufacturer, string model, string name)
    {
        SoftwareVersion=version;
        Manufacturer = manufacturer;
        Model = model;
        Name = name;
        Identifiers = new[] { $"easywave2mqtt_{id:X8}" };
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


