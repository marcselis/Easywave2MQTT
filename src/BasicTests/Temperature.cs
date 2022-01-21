// See https://aka.ms/new-console-template for more information
using System.Text.Json.Serialization;

public class Temperature
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("dev_cla")]
    public string? DeviceClass { get; set; }
    [JsonPropertyName("stat_t")]
    public string? TopicState { get; set; }
    [JsonPropertyName("unit_of_meas")]
    public string? UnitOfMeasure { get; set; }
    [JsonPropertyName("val_tpl")]
    public string Value { get; set; }
    [JsonPropertyName("payload_available")]
    public string Available { get; set; }
    [JsonPropertyName("pl_not_avail")]
    public string NotAvailable { get; set; }
}


