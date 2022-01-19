// See https://aka.ms/new-console-template for more information
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

const string ConfigTopic = "thuis/button/slk1/config";
const string TOPIC_TEMP_CONF = "home-assistant/sensor/office_temp/config";
const string TOPIC_STATE = "home-assistant/sensor/office/state";
const string TEMP_NAME = "Office Temp";
const string TEMP_CLASS = "temperature";

Console.WriteLine("Hello, World!");


var button = new Button { Name = "Licht Slaapkamer ouders", UniqueId = "slk1", Device = new Device { Manufacturer = "Niko", Identifiers = "44554:0500" } };
var jsonString = JsonSerializer.Serialize(button);

var temp = JsonSerializer.Serialize(new Temperature {  Name= TEMP_NAME, DeviceClass=TEMP_CLASS, TopicState=TOPIC_STATE, UnitOfMeasure="°C", Value= "{{value_json.temperature}}", Available="online", NotAvailable="offline" });
var options = new MqttClientOptionsBuilder().WithKeepAlivePeriod(TimeSpan.FromSeconds(120)).WithClientId("BasicTestClient").WithTcpServer("192.168.0.12").WithCredentials("mqtt", "mqtt").Build();
var factory = new MqttFactory();
using (var client = factory.CreateMqttClient())
{

    await client.ConnectAsync(options);

    await client.PublishAsync(TOPIC_TEMP_CONF, temp);
    await client.PublishAsync(TOPIC_STATE, "{ \"temperature\": \"21\" }");
    //await client.PublishAsync(ConfigTopic, jsonString);
    await client.DisconnectAsync();
}

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
    [JsonPropertyName("pl_avail")]
    public string Available { get; set; }
    [JsonPropertyName("pl_not_avail")]
    public string NotAvailable { get; set; }
}
public class Device
{
    [JsonPropertyName("manufacturer")]
   public string? Manufacturer { get; set; } 

    [JsonPropertyName("identifiers")]
    public string? Identifiers { get; set; }

}

public class Button
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("unique_id")]
    public string? UniqueId { get; set; }

    [JsonPropertyName("device")]
    public Device? Device { get; set; }
}