// See https://aka.ms/new-console-template for more information
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

internal static class Extensions
{
    public static async Task<Device> DeclareDevice(this IMqttClient client, string manufacturer, string model, uint id, string name)
    {
        switch (manufacturer)
        {
            case "Niko":
                {
                    switch (model)
                    {
                        case "410-00001": //4 button swith
                            {
                                var device = new NikoButton(client, id, model, name);
                                for (int btn = 1; btn < 5; btn++)
                                {
                                    await client.DeclareButton(device, btn).ConfigureAwait(false);
                                }
                                return device;
                            }
                        default:
                            throw new NotSupportedException($"Unsupported model: {model}");
                    }
                }
            default:
                throw new NotSupportedException($"Unsupported manufacturer: {manufacturer}");
        }

    }

    public static async Task DeclareButton(this IMqttClient client, Device device, int btn)
    {
        string[] eventNames = new[] { "short_press", "long_press", "long_release" };
        foreach (var eventName in eventNames)
        {
            var button = new RootObject { AutomationType = "trigger", Type = $"button_{eventName}", SubType = $"button_{btn}", Payload = $"button_{btn}_{eventName}", Topic = $"easywave2mqtt/{device.Identifiers[0]}/action", Device = device };
            var payload = JsonSerializer.Serialize(button);
            while (!client.IsConnected)
            {
                await client.ReconnectAsync().ConfigureAwait(false);
            }
            await client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic($"homeassistant/device_automation/{device.Identifiers[0]}/button_{btn}_{eventName}/config")
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                .WithRetainFlag(false)
                .Build());
        }
    }

    public static async Task SendButtonShortPress(this IMqttClient client, NikoButton button, int buttonId)
    {
        Console.WriteLine($"Short press button {buttonId}");
        while (!client.IsConnected)
        {
            await client.ReconnectAsync().ConfigureAwait(false);
        }
        await client.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"easywave2mqtt/{button.Identifiers[0]}/action")
            .WithPayload($"button_{buttonId}_short_press")
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
            .WithRetainFlag(false).Build());
    }


    public static async Task SendButtonLongRelease(this IMqttClient client, NikoButton button, int buttonId)
    {
        Console.WriteLine($"Release of button {buttonId}");
        while (!client.IsConnected)
        {
            await client.ReconnectAsync().ConfigureAwait(false);
        }
        await client.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"easywave2mqtt/{button.Identifiers[0]}/action")
            .WithPayload($"button_{buttonId}_long_release")
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
            .WithRetainFlag(false).Build());
    }

    public static async Task SendButtonLongHold(this IMqttClient client, NikoButton button, int buttonId)
    {
        Console.WriteLine($"Long press button {buttonId}");
        while (!client.IsConnected)
        {
            await client.ReconnectAsync().ConfigureAwait(false);
        }
        await client.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"easywave2mqtt/{button.Identifiers[0]}/action")
            .WithPayload($"button_{buttonId}_long_press")
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
            .WithRetainFlag(false).Build());
    }
}
