using System.Text.Json;
using Easywave2Mqtt.Events;
using Easywave2Mqtt.Messages;
using Easywave2Mqtt.Tools;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Protocol;

namespace Easywave2Mqtt.Mqtt
{

  public partial class MessagingService : BackgroundService
  {
    private static readonly IMqttClientOptions Options = new MqttClientOptionsBuilder().WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                                                                                       .WithClientId("BasicTestClient")
                                                                                       .WithTcpServer("192.168.0.12", 1883)
                                                                                       .WithCredentials("mqtt", "mqtt")
                                                                                       .WithCommunicationTimeout(TimeSpan.FromSeconds(30))
                                                                                       .Build();
    private readonly IBus _bus;
    private readonly IMqttClient _client;
    private readonly ILogger<MessagingService> _logger;

    public MessagingService(ILogger<MessagingService> logger, IBus bus)
    {
      _logger = logger;
      _bus = bus;
      _client = BuildMqttClient().UseApplicationMessageReceivedHandler(MessageHandler);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
      LogServiceStart();
      await Send("easywave2mqtt", "available", true).ConfigureAwait(false);
      await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      LogServiceStop();
      await Send("easywave2mqtt", "unavailable", true).ConfigureAwait(false);
      await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      LogExecuteAsyncStart();
      try
      {
        using (_bus.Subscribe<DeclareButton>(btn => DeclareButton(btn.Address, btn.KeyCode, btn.Name, btn.Area, btn.Count)))
        using (_bus.Subscribe<DeclareLight>(sw => DeclareLight(sw.Id, sw.Name, sw.Area)))
        using (_bus.Subscribe<SendButtonPress>(btn => SendButtonPress(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<SendButtonDoublePress>(btn => SendButtonDoublePress(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<SendButtonTriplePress>(btn => SendButtonTriplePress(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<SendButtonHold>(btn => SendButtonHold(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<SendButtonRelease>(btn => SendButtonRelease(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<EasywaveSwitchTurnedOn>(SendSwitchTurnedOn))
        using (_bus.Subscribe<EasywaveSwitchTurnedOff>(SendSwitchTurnedOff))
        {
          MqttClientSubscribeResult? result = await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithAtLeastOnceQoS().WithTopic("easywave2mqtt/#").Build()
                                                                         , new MqttTopicFilterBuilder().WithAtLeastOnceQoS().WithTopic("mqtt2easywave/#").Build())
                                                           .ConfigureAwait(false);
          if (result != null)
          {
            Console.WriteLine(result);
          }
          while (!stoppingToken.IsCancellationRequested)
          {
            await Task.Delay(100, stoppingToken).ConfigureAwait(false);
          }
        }
      }
      catch (OperationCanceledException) { }
      LogExecuteAsyncEnd();
    }

    private Task SendSwitchTurnedOff(EasywaveSwitchTurnedOff sw)
    {
      return Send($"easywave2mqtt/{sw.Id}/state", "off");
    }

    private Task SendSwitchTurnedOn(EasywaveSwitchTurnedOn sw)
    {
      return Send($"easywave2mqtt/{sw.Id}/state", "on");
    }

    private async Task MessageHandler(MqttApplicationMessageReceivedEventArgs arg)
    {
      LogTopicReceived(arg.ApplicationMessage.Topic, arg.ApplicationMessage.ConvertPayloadToString());
      arg.IsHandled = true;
      await arg.AcknowledgeAsync(CancellationToken.None).ConfigureAwait(false);
      var parts = arg.ApplicationMessage.Topic.Split("/");
      //Ignore uninteresting messages
      if (parts.Length < 3)
      {
        return;
      }
      var start = parts[0];
      var address = parts[1];
      var action = parts[2];
      var body = arg.ApplicationMessage.ConvertPayloadToString();
      switch (start)
      {
        case "easywave2mqtt":
          switch (action)
          {
            case "state":
              break;
            default:
              var bodyParts = body.Split('_');
              var buttonCode = bodyParts[1][0];
              _logger.LogInformation("Received {Action} on {Address}:{ButtonCode}", bodyParts[2], address, buttonCode);
              await _bus.PublishAsync(new MqttMessage(address, buttonCode, bodyParts[2]));
              break;
          }
          break;
        case "mqtt2easywave":
          _logger.LogInformation("Received {Action} on {Address}", body, address);
          await _bus.PublishAsync(new MqttCommand(address, body));
          break;
      }
    }

    public async Task DeclareButton(string id, char btn, string name, string? area, int count)
    {
      var eventNames = new[] { "press", "double_press", "triple_press", "hold", "release" };
      foreach (var eventName in eventNames)
      {
        var button = new Button(id, btn, name, area, eventName, count);
        var payload = JsonSerializer.Serialize(button, MyJsonContext.Default.Button);
        await Send($"homeassistant/device_automation/{id}/button_{btn}_{eventName}/config", payload).ConfigureAwait(false);
      }
    }

    public Task DeclareLight(string id, string name, string? area)
    {
      var sw = new Light(id, name, area);
      var payload = JsonSerializer.Serialize(sw, MyJsonContext.Default.Light);
      return Send($"homeassistant/light/{id}/config", payload);
    }

    public Task SendButtonPress(string id, char btn)
    {
      LogButtonPress(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_press");
    }

    public Task SendButtonDoublePress(string id, char btn)
    {
      LogButtonDoublePress(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_double_press");
    }

    public Task SendButtonTriplePress(string id, char btn)
    {
      LogButtonTriplePress(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_triple_press");
    }

    public Task SendButtonRelease(string id, char btn)
    {
      LogButtonRelease(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_release");
    }

    public Task SendButtonHold(string id, char btn)
    {
      LogButtonHold(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_hold");
    }

    internal async Task Send(string topic, string payload, bool retain = false)
    {
      LogTopicPublish(topic, payload);
      while (!_client.IsConnected)
      {
        try
        {
          MqttClientConnectResult? reconnectResult = await _client.ReconnectAsync().ConfigureAwait(false);
          if (reconnectResult.ResultCode != MqttClientConnectResultCode.Success)
          {
            LogReconnectFailed(reconnectResult.ResultCode, reconnectResult.ReasonString);
          }
        }
        catch (InvalidOperationException) { }
      }
      MqttClientPublishResult? publishResult = await _client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic)
                                                                                                             .WithPayload(payload)
                                                                                                             .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                                                                                                             .WithRetainFlag(retain)
                                                                                                             .Build())
                                                            .ConfigureAwait(false);
      if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
      {
        LogPublishFailed(publishResult.ReasonCode, publishResult.ReasonString);
      }
    }

    private static IMqttClient BuildMqttClient()
    {
      IMqttClient client = new MqttFactory().CreateMqttClient();
      client.ConnectAsync(Options).Wait();
      return client;
    }

    #region LoggingMethods
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "MQTT service is starting...")]
    public partial void LogServiceStart();

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "MQTT service is stopping...")]
    public partial void LogServiceStop();
    
    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Topic {Topic} received with payload {Payload}")]
    public partial void LogTopicReceived(string topic, string payload);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Publishing topic {Topic} with payload {Payload}")]
    public partial void LogTopicPublish(string topic, string payload);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "MQTT Reconnect failed with result code {ResultCode} and reason {ReasonString}")]
    public partial void LogReconnectFailed(MqttClientConnectResultCode resultCode, string reasonString);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "MQTT Publish failed with result code {ReasonCode} and readon {ReasonString}")]
    public partial void LogPublishFailed(MqttClientPublishReasonCode reasonCode, string reasonString);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} pressed")]
    public partial void LogButtonPress(string id, char buttonCode);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} double pressed")]
    public partial void LogButtonDoublePress(string id, char buttonCode);

    [LoggerMessage(EventId = 19, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} triple pressed")]
    public partial void LogButtonTriplePress(string id, char buttonCode);

    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} held")]
    public partial void LogButtonHold(string id, char buttonCode);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} released")]
    public partial void LogButtonRelease(string id, char buttonCode);

    [LoggerMessage(EventId = 24, Level = LogLevel.Trace, Message = "-->ExecuteAsync")]
    public partial void LogExecuteAsyncStart();

    [LoggerMessage(EventId = 25, Level = LogLevel.Trace, Message = "<--ExecuteAsync")]
    public partial void LogExecuteAsyncEnd();

    #endregion
  }

}