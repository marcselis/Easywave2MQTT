// See https://aka.ms/new-console-template for more information
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System.Globalization;
using System.IO.Ports;
using System.Text.Json;
using static System.Runtime.CompilerServices.RuntimeHelpers;

const string DISCOVERY_PREFIX = "homeassistant";
const string TOPIC_DEVICETRIGGER_CONFIG = "<discovery_prefix>/device_automation/[<node_id>/]<object_id>/config";
const string TOPIC_BTN_CONFIG = "homeassistant/button/slk1/config";
const string TOPIC_TEMP_CONF = "homeassistant/sensor/office_temp/config";
const string TOPIC_STATE = "homeassistant/sensor/office/state";
const string TEMP_NAME = "Office Temp";
const string TEMP_CLASS = "temperature";

Console.WriteLine("Hello, World!");


//var button = new Button { Name = "Licht Slaapkamer ouders", UniqueId = "slk1", Device = new Device(0x0002, "Niko", "1245", "Licht Slaapkamer ouders" ) };
//var buttonConfig = JsonSerializer.Serialize(button);

//var sensorConfig = JsonSerializer.Serialize(new Temperature { Name = TEMP_NAME, DeviceClass = TEMP_CLASS, TopicState = TOPIC_STATE, UnitOfMeasure = "°C", Value = "{{value_json.temperature}}", Available = "online", NotAvailable = "offline" });
var options = new MqttClientOptionsBuilder()
    .WithKeepAlivePeriod(TimeSpan.FromSeconds(1))
    .WithClientId("BasicTestClient")
    .WithTcpServer("192.168.0.12", 1883)
    .WithCredentials("mqtt", "mqtt").Build();
var factory = new MqttFactory();
Dictionary<uint, NikoButton> buttons = new Dictionary<uint, NikoButton>();

using (var client = factory.CreateMqttClient())
{
    await client.ConnectAsync(options).ConfigureAwait(false);



    var port = new SerialPort("COM4", 57600, Parity.None, 8, StopBits.One)
    {
        Handshake = Handshake.None,
        DtrEnable = true,
        RtsEnable = true,
        NewLine = "\r"
    };
    //port.ErrorReceived += ErrorReceived;
    port.DataReceived += async (sender, e) =>
    {
        async Task ProcessLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var parts = line.Split(',', '\t');
            if (parts.Length == 0)
            {
                return;
            }

            switch (parts[0])
            {
                //case "ID":
                //    VendorId = parts[1];
                //    DeviceId = parts[2];
                //    Version = uint.Parse(parts[3], NumberStyles.HexNumber);
                //    _log.Debug($"Reveived ID {VendorId}:{DeviceId} Version {Version}");
                //    break;
                //case "GETP":
                //    AddressCount = uint.Parse(parts[1], NumberStyles.HexNumber);
                //    _log.Debug($"Transceiver has {AddressCount} addresses");
                //    break;
                case "REC":
                    var address = uint.Parse(parts[1], NumberStyles.HexNumber);
                    var code = (KeyCode)Enum.Parse(typeof(KeyCode), parts[2]);
                    if (!buttons.ContainsKey(address))
                    {
                        buttons.Add(address, (NikoButton)await client.DeclareDevice("Niko", "410-00001", address, address.ToString("X8")));
                    }
                    await buttons[address].HandleButton((int)code);
                    break;
                case "OK":
                    break;
                default:
                    //   _log.Debug($"Unexpected input: {line}");
                    break;
            }
        }

        var port = (SerialPort)sender;
        var line = port.ReadLine();
        await ProcessLine(line).ConfigureAwait(false);
    };

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

  //  var button = (NikoButton)await client.DeclareDevice("Niko", "410-00001", 0x0001, "Slaapkamer Ouders 1").ConfigureAwait(false);

    Console.WriteLine("Press buttons 1 to 4 or q to exit");
    ConsoleKeyInfo key;
    do
    {
        key = Console.ReadKey(true);
        //switch(key.Key)
        //{
        //    case ConsoleKey.D1:
        //        await button.HandleButton(1);
        //        break;
        //    case ConsoleKey.D2:
        //        await button.HandleButton(2);
        //        break;
        //    case ConsoleKey.D3:
        //        await button.HandleButton(3);
        //        break;
        //    case ConsoleKey.D4:
        //        await button.HandleButton(4);
        //        break;
        //}
    }
    while (key.Key != ConsoleKey.Q);
    port.Close();
    await client.DisconnectAsync().ConfigureAwait(false);
}

       public enum KeyCode
{
    A = 1,
    B,
    C,
    D
}
