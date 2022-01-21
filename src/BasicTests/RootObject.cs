// See https://aka.ms/new-console-template for more information
using System.Text.Json.Serialization;

public class RootObject
{
    [JsonPropertyName("automation_type")]
    public string AutomationType { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("subtype")]
    public string SubType { get; set; }
    [JsonPropertyName("payload")]
    public string Payload { get; set; }
    [JsonPropertyName("topic")]
    public string Topic { get; set; }
    [JsonPropertyName("device")]
    public Device Device { get; set; }
}


