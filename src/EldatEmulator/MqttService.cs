//using System.Text.Json;
//using Easywave2Mqtt.Events;

using InMemoryBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace EldatEmulator
{

  public class MqttService : BackgroundService
  {
    private static readonly MqttFactory MqttFactory = new();
    private readonly IBus _bus;
    private readonly IManagedMqttClient _client;
    private readonly ILogger<MqttService> _logger;

    public MqttService(ILogger<MqttService> logger, IBus bus)
    {
      _logger = logger;
      _bus = bus;
      _client = MqttFactory.CreateManagedMqttClient();
    }

    public override void Dispose()
    {
      _client.Dispose();
      GC.SuppressFinalize(this);
      base.Dispose();
    }


    public override async Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("Starting MqttService");
      MqttClientOptions clientOptions = new MqttClientOptionsBuilder()
        .WithClientId("EldatEmulator")
        .WithTcpServer("127.0.0.1")
        .Build();
      ManagedMqttClientOptions managedClientOptions = new ManagedMqttClientOptionsBuilder()
        .WithClientOptions(clientOptions)
        .WithPendingMessagesOverflowStrategy(MQTTnet.Server.MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
        .Build();
      _client.ApplicationMessageReceivedAsync += MessageHandler;
      await _client.StartAsync(managedClientOptions).ConfigureAwait(false);
      await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      _client.ApplicationMessageReceivedAsync -= MessageHandler;
      await _client.StopAsync().ConfigureAwait(false);
      await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("Running MqttService");
      using (_bus.Subscribe<SendMqttMessage>(msg => Send(msg.Topic, msg.Payload)))
      {
        MqttTopicFilter outgoing = new MqttTopicFilterBuilder().WithAtLeastOnceQoS().WithTopic("easywave2mqtt/#").Build();
        MqttTopicFilter incoming = new MqttTopicFilterBuilder().WithAtLeastOnceQoS().WithTopic("mqtt2easywave/#").Build();
        await _client.SubscribeAsync(new List<MqttTopicFilter>() { outgoing, incoming }).ConfigureAwait(false);
        while (!stoppingToken.IsCancellationRequested)
        {
          await Task.Delay(100, stoppingToken).ConfigureAwait(false);
        }
      }
      stoppingToken.ThrowIfCancellationRequested();
      _logger.LogInformation("Running MqttService ended");
    }

    private async Task MessageHandler(MqttApplicationMessageReceivedEventArgs arg)
    {
      _logger.LogDebug("MQTT Topic {Topic} received with payload {Payload}", arg.ApplicationMessage.Topic, arg.ApplicationMessage.ConvertPayloadToString());
      arg.IsHandled = true;
      await arg.AcknowledgeAsync(CancellationToken.None).ConfigureAwait(false);
      //var parts = arg.ApplicationMessage.Topic.Split("/");
      ////Ignore uninteresting messages
      //if (parts.Length < 3)
      //{
      //  return;
      //}
      //var start = parts[0];
      //var address = parts[1];
      //var action = parts[2];
      //var body = arg.ApplicationMessage.ConvertPayloadToString();
      //switch (start)
      //{
      //  //case "easywave2mqtt":
      //  //  switch (action)
      //  //  {
      //  //    case "state":
      //  //      break;
      //  //    default:
      //  //      var bodyParts = body.Split('_');
      //  //      var buttonCode = bodyParts[1][0];
      //  //      _logger.LogInformation("Received {Action} on {Address}:{ButtonCode}", bodyParts[2], address, buttonCode);
      //  //      await _bus.PublishAsync(new MqttMessage(address, buttonCode, bodyParts[2]));
      //  //      break;
      //  //  }
      //  //  break;
      //  case "mqtt2easywave":
      //    _logger.LogInformation("Received {Action} on {Address}", body, address);
      //    await _bus.PublishAsync(new MqttCommand(address, body));
      //    break;
      //}
    }

    internal Task Send(string topic, string payload, bool retain = false)
    {
      _logger.LogDebug("Sending topic {Topic} with payload {Payload} to MQTT", topic, payload);
      MqttApplicationMessage message = new MqttApplicationMessageBuilder().WithTopic(topic)
        .WithPayload(payload)
        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
        .WithRetainFlag(retain)
        .Build();
      return _client.EnqueueAsync(message);
    }


  }

}