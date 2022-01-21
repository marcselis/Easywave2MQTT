// See https://aka.ms/new-console-template for more information
using System.Text.Json.Serialization;

public class Button
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("unique_id")]
    public string? UniqueId { get; set; }

    [JsonPropertyName("device")]
    public Device? Device { get; set; }

    [JsonPropertyName("command_topic")]
    public string CommandTopic { get; set; }
}


