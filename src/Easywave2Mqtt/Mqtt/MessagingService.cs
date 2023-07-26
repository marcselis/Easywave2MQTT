﻿using System.Text.Json;
using Easywave2Mqtt.Events;
using Easywave2Mqtt.Messages;
using InMemoryBus;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace Easywave2Mqtt.Mqtt
{

  public partial class MessagingService : BackgroundService
  {
    private readonly IMqttClient _client = new MqttFactory().CreateMqttClient();
    private readonly MqttClientOptions _clientOptions;
    private readonly IBus _bus;
    private readonly ILogger<MessagingService> _logger;
    private readonly MqttTopicFilter _outgoing = new MqttTopicFilterBuilder().WithAtLeastOnceQoS().WithTopic("easywave2mqtt/#").Build();
    private readonly MqttTopicFilter _incoming = new MqttTopicFilterBuilder().WithAtLeastOnceQoS().WithTopic("mqtt2easywave/#").Build();
    private readonly MqttClientSubscribeOptions _subscribeOptions;
    private bool _reconnecting = false;

    public MessagingService(ILogger<MessagingService> logger, IBus bus)
    {
      _logger = logger;
      _bus = bus;
      _clientOptions = new MqttClientOptionsBuilder()
        .WithClientId("Easywave2MQTT")
        .WithTcpServer(Program.Settings!.MQTTServer, Program.Settings!.MQTTPort)
        .WithCredentials(Program.Settings!.MQTTUser, Program.Settings!.MQTTPassword)
        .Build();
      ;
      _subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(_outgoing).WithTopicFilter(_incoming).Build();
    }

    public override void Dispose()
    {
      _client.Dispose();
      GC.SuppressFinalize(this);
      base.Dispose();
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
      LogServiceStart();
      _client.ApplicationMessageReceivedAsync += MessageHandler;
      _client.DisconnectedAsync += DisconnectedHandler;
      var connectResult = await ConnectAsync().ConfigureAwait(false);
      if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
      {
        LogClientConnectionFailed(connectResult.ResultCode);
        return;
      }

      var subscribeResult = await SubscribeAsync().ConfigureAwait(false);
      foreach (var item in subscribeResult.Items)
      {
        if (item.ResultCode != MqttClientSubscribeResultCode.GrantedQoS0)
        {
          LogClientSubscriptionFailed(item.TopicFilter.Topic, item.ResultCode);
        }
      }
      await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DisconnectedHandler(MqttClientDisconnectedEventArgs arg)
    {
      if (_reconnecting)
        return;
      _reconnecting = true;
      LogClientConnectionBroken();

      while (!_client.IsConnected)
      {
        try
        {
          _=await ConnectAsync().ConfigureAwait(false);
          _=await SubscribeAsync().ConfigureAwait(false);
          LogClientReconnected();
          _reconnecting=false;
          break;
        }
        catch (Exception ex)
        {
          LogClientReconnectFailed(ex.Message);
          await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
      }
    }

    private Task<MqttClientConnectResult> ConnectAsync()
    {
      return _client.ConnectAsync(_clientOptions);
    }

    private Task<MqttClientSubscribeResult> SubscribeAsync()
    {
      return _client.SubscribeAsync(_subscribeOptions);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      LogServiceStop();
      _client.ApplicationMessageReceivedAsync -= MessageHandler;
      await _client.DisconnectAsync().ConfigureAwait(false);
      await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Concurrency", "PH_P008:Missing OperationCanceledException in Task", Justification = "We want to stop gracefully")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      LogServiceRunning();
      await Send("easywave2mqtt", "available", true).ConfigureAwait(false);
      try
      {
        using (_bus.Subscribe<DeclareButton>(btn => DeclareButton(btn.Address, btn.KeyCode, btn.Name, btn.Area, btn.Count)))
        using (_bus.Subscribe<DeclareLight>(sw => DeclareLight(sw.Id, sw.Name, sw.Area)))
        using (_bus.Subscribe<DeclareBlind>(sw=>DeclareBlind(sw.Id,sw.Name,sw.Area)))
        using (_bus.Subscribe<SendButtonPress>(btn => SendButtonPress(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<SendButtonDoublePress>(btn => SendButtonDoublePress(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<SendButtonTriplePress>(btn => SendButtonTriplePress(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<SendButtonHold>(btn => SendButtonHold(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<SendButtonRelease>(btn => SendButtonRelease(btn.Address, btn.KeyCode)))
        using (_bus.Subscribe<EasywaveSwitchTurnedOn>(SendSwitchTurnedOn))
        using (_bus.Subscribe<EasywaveSwitchTurnedOff>(SendSwitchTurnedOff))
        using (_bus.Subscribe<EasywaveBlindIsOpen>(SendBlindOpen))
        using (_bus.Subscribe<EasywaveBlindIsClosed>(SendBlindClosed))
        {
          while (!stoppingToken.IsCancellationRequested)
          {
            await Task.Delay(100, stoppingToken).ConfigureAwait(false);
          }
        }
      }
      catch (OperationCanceledException) { }
      await Send("easywave2mqtt", "unavailable", true).ConfigureAwait(false);
      LogServiceStopped();
    }

    private Task SendSwitchTurnedOff(EasywaveSwitchTurnedOff sw)
    {
      return Send($"easywave2mqtt/{sw.Id}/state", "off", true);
    }

    private Task SendSwitchTurnedOn(EasywaveSwitchTurnedOn sw)
    {
      return Send($"easywave2mqtt/{sw.Id}/state", "on", true);
    }

    private Task SendBlindOpen(EasywaveBlindIsOpen bl)
    {
      return Send($"easywave2mqtt/{bl.Id}/state", "open", true);
    }

    private Task SendBlindClosed(EasywaveBlindIsClosed bl)
    {
      return Send($"easywave2mqtt/{bl.Id}/state", "closed", true);
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
              _logger.LogDebug("Received {Action} on {Address}:{ButtonCode}", bodyParts[2], address, buttonCode);
              await _bus.PublishAsync(new MqttMessage(address, buttonCode, bodyParts[2])).ConfigureAwait(false);
              break;
          }
          break;
        case "mqtt2easywave":
          _logger.LogDebug("Received {Action} on {Address}", body, address);
          await _bus.PublishAsync(new MqttCommand(address, body)).ConfigureAwait(false);
          break;
      }
    }

    private async Task DeclareButton(string id, char btn, string name, string? area, int count)
    {
      var eventNames = new[] { "press", "double_press", "triple_press", "hold", "release" };
      foreach (var eventName in eventNames)
      {
        var button = new Button(id, btn, name, area, eventName, count);
        var payload = JsonSerializer.Serialize(button, MyJsonContext.Default.Button);
        await Send($"homeassistant/device_automation/{id}/button_{btn}_{eventName}/config", payload).ConfigureAwait(false);
      }
    }

    private Task DeclareLight(string id, string name, string? area)
    {
      var sw = new Light(id, name, area);
      var payload = JsonSerializer.Serialize(sw, MyJsonContext.Default.Light);
      return Send($"homeassistant/light/{id}/config", payload);
    }

    private Task DeclareBlind(string id, string name, string? area)
    {
      var sw = new Cover(id, name, area);
      var payload = JsonSerializer.Serialize(sw, MyJsonContext.Default.Cover);
      return Send($"homeassistant/cover/{id}/config", payload);
    }

    private Task SendButtonPress(string id, char btn)
    {
      LogButtonPress(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_press");
    }

    private Task SendButtonDoublePress(string id, char btn)
    {
      LogButtonDoublePress(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_double_press");
    }

    private Task SendButtonTriplePress(string id, char btn)
    {
      LogButtonTriplePress(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_triple_press");
    }

    private Task SendButtonRelease(string id, char btn)
    {
      LogButtonRelease(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_release");
    }

    private Task SendButtonHold(string id, char btn)
    {
      LogButtonHold(id, btn);
      return Send($"easywave2mqtt/{id}/action", $"button_{btn}_hold");
    }

    private async Task Send(string topic, string payload, bool retain = false)
    {
      LogTopicPublish(topic, payload);
      MqttClientPublishResult publishResult = await _client.PublishAsync(new MqttApplicationMessageBuilder()
         .WithTopic(topic)
         .WithPayload(payload)
         .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
         .WithRetainFlag(retain)
         .Build());

      if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success)
      {
        LogPublishFailed(publishResult.ReasonCode, publishResult.ReasonString);
        return;
      }
    }

    #region LoggingMethods
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Service starting...")]
    private partial void LogServiceStart();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Service stopping...")]
    private partial void LogServiceStop();

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Topic {Topic} received with payload {Payload}")]
    private partial void LogTopicReceived(string topic, string payload);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Publishing topic {Topic} with payload {Payload}")]
    private partial void LogTopicPublish(string topic, string payload);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "MQTT Reconnect failed with result code {ResultCode} and reason {ReasonString}")]
    private partial void LogReconnectFailed(MqttClientConnectResultCode resultCode, string reasonString);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "MQTT Publish failed with result code {ReasonCode} and readon {ReasonString}")]
    private partial void LogPublishFailed(MqttClientPublishReasonCode reasonCode, string reasonString);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Connection to MQTT broker closed. Attempting to reconnect...")]
    private partial void LogClientConnectionBroken();

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Successfully reconnected to MQTT broker.")]
    private partial void LogClientReconnected();

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Failed to reconnect to MQTT broker: {Message}")]
    private partial void LogClientReconnectFailed(string message);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Failed to connect to MQTT broker: {Result}")]
    private partial void LogClientConnectionFailed(MqttClientConnectResultCode result);

    [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Failed to subscribe to topic {Topic}: {Result}")]
    private partial void LogClientSubscriptionFailed(string topic, MqttClientSubscribeResultCode result);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} pressed")]
    private partial void LogButtonPress(string id, char buttonCode);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} double pressed")]
    private partial void LogButtonDoublePress(string id, char buttonCode);

    [LoggerMessage(EventId = 19, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} triple pressed")]
    private partial void LogButtonTriplePress(string id, char buttonCode);

    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} held")]
    private partial void LogButtonHold(string id, char buttonCode);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "Button {Id}:{ButtonCode} released")]
    private partial void LogButtonRelease(string id, char buttonCode);

    [LoggerMessage(EventId = 24, Level = LogLevel.Information, Message = "Service running...")]
    private partial void LogServiceRunning();

    [LoggerMessage(EventId = 25, Level = LogLevel.Error, Message = "Service stopped...")]
    private partial void LogServiceStopped();

    #endregion
  }

}