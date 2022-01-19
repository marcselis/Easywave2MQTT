// See https://aka.ms/new-console-template for more information
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

const string TOPIC_BTN_CONFIG = "homeassistant/button/slk1/config";
const string TOPIC_TEMP_CONF = "homeassistant/sensor/office_temp/config";
const string TOPIC_STATE = "homeassistant/sensor/office/state";
const string TEMP_NAME = "Office Temp";
const string TEMP_CLASS = "temperature";

Console.WriteLine("Hello, World!");


var button = new Button { Name = "Licht Slaapkamer ouders", UniqueId = "slk1", Device = new Device { Manufacturer = "Niko", Identifiers = "44554:0500" } };
var buttonConfig = JsonSerializer.Serialize(button);

var sensorConfig = JsonSerializer.Serialize(new Temperature {  Name= TEMP_NAME, DeviceClass=TEMP_CLASS, TopicState=TOPIC_STATE, UnitOfMeasure="°C", Value= "{{value_json.temperature}}", Available="online", NotAvailable="offline" });
var options = new MqttClientOptionsBuilder()
    .WithKeepAlivePeriod(TimeSpan.FromSeconds(1))
    .WithClientId("BasicTestClient")
    .WithTcpServer("192.168.0.12", 1883)
    .WithCredentials("mqtt", "mqtt").Build();
var factory = new MqttFactory();
using (var client = factory.CreateMqttClient())
{

    await client.ConnectAsync(options);

    await client.PublishAsync(new MqttApplicationMessageBuilder()
        .WithTopic(TOPIC_TEMP_CONF)
        .WithPayload(sensorConfig)
        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
        .WithRetainFlag(true)
        .Build());
    await client.PublishAsync(TOPIC_STATE, "{ \"temperature\": \"22\" }");
    await client.PublishAsync(new MqttApplicationMessageBuilder()
        .WithTopic(TOPIC_BTN_CONFIG)
        .WithPayload(buttonConfig)
        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
        .WithRetainFlag(true).Build());
    await client.DisconnectAsync();
}

public class Button
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("unique_id")]
    public string? UniqueId { get; set; }

    [JsonPropertyName("device")]
    public Device? Device { get; set; }

    [JSon]
    public string CommandTopic { get; set; }
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
    [JsonPropertyName("payload_available")]
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

