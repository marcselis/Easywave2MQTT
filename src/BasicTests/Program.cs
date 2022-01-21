// See https://aka.ms/new-console-template for more information
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

const string DISCOVERY_PREFIX = "homeassistant";
const string TOPIC_DEVICETRIGGER_CONFIG = "<discovery_prefix>/device_automation/[<node_id>/]<object_id>/config";
const string TOPIC_BTN_CONFIG = "homeassistant/button/slk1/config";
const string TOPIC_TEMP_CONF = "homeassistant/sensor/office_temp/config";
const string TOPIC_STATE = "homeassistant/sensor/office/state";
const string TEMP_NAME = "Office Temp";
const string TEMP_CLASS = "temperature";

Console.WriteLine("Hello, World!");


var button = new Button { Name = "Licht Slaapkamer ouders", UniqueId = "slk1", Device = new Device { Manufacturer = "Niko", Identifiers = new[] { "44554:0500" } } };
var buttonConfig = JsonSerializer.Serialize(button);

var sensorConfig = JsonSerializer.Serialize(new Temperature { Name = TEMP_NAME, DeviceClass = TEMP_CLASS, TopicState = TOPIC_STATE, UnitOfMeasure = "°C", Value = "{{value_json.temperature}}", Available = "online", NotAvailable = "offline" });
var options = new MqttClientOptionsBuilder()
    .WithKeepAlivePeriod(TimeSpan.FromSeconds(1))
    .WithClientId("BasicTestClient")
    .WithTcpServer("192.168.0.12", 1883)
    .WithCredentials("mqtt", "mqtt").Build();
var factory = new MqttFactory();
using (var client = factory.CreateMqttClient())
{

    await client.ConnectAsync(options);

    //await client.PublishAsync(new MqttApplicationMessageBuilder()
    //    .WithTopic(TOPIC_TEMP_CONF)
    //    .WithPayload(sensorConfig)
    //    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
    //    .WithRetainFlag(true)
    //    .Build());
    //await client.PublishAsync(TOPIC_STATE, "{ \"temperature\": \"22\" }");
    //await client.PublishAsync(new MqttApplicationMessageBuilder()
    //    .WithTopic(TOPIC_BTN_CONFIG)
    //    .WithPayload(buttonConfig)
    //    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
    //    .WithRetainFlag(true).Build());

    //{"automation_type":"trigger",
    // "type":"action",
    // "subtype":"arrow_left_click",
    // "payload":"arrow_left_click",
    // "topic":"zigbee2mqtt/0x90fd9ffffedf1266/action",
    // "device":{"identifiers":["zigbee2mqtt_0x90fd9ffffedf1266"],"name":"0x90fd9ffffedf1266","sw_version":"Zigbee2mqtt 1.14.0","model":"TRADFRI remote control (E1524/E1810)","manufacturer":"IKEA"}}
    var slk1Button = new Device() { Identifiers = new string[] { "easywave2mqtt_0x001" }, Manufacturer = "Niko", Model = "410-00001", Name = "Slaapkamer Ouders 1", SoftwareVersion = "Easywave2Mqtt 0.1 beta" };
    var slk1_button1_short_press = new RootObject { AutomationType = "trigger", Type = "button_short_press", SubType = "button_1", Payload = "button_1_short_press", Topic = "easywave2mqtt/0x001/action", Device = slk1Button };
    var payload = JsonSerializer.Serialize(slk1_button1_short_press);
    await client.PublishAsync(new MqttApplicationMessageBuilder()
        .WithTopic("homeassistant/device_automation/0x001/button_1_short_press/config")
        .WithPayload(payload)
        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
        .WithRetainFlag(false)
        .Build());

    var slk1_button1_long_press = new RootObject { AutomationType = "trigger", Type = "button_long_press", SubType = "button_1", Payload = "button_1_long_press", Topic = "easywave2mqtt/0x001/action", Device = slk1Button };
    payload = JsonSerializer.Serialize(slk1_button1_long_press);
    await client.PublishAsync(new MqttApplicationMessageBuilder()
      .WithTopic("homeassistant/device_automation/0x001/button_1_long_press/config")
      .WithPayload(payload)
      .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
      .WithRetainFlag(false)
      .Build());

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

    [JsonPropertyName("command_topic")]
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

public class Device
{
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


