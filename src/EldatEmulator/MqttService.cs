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

  public sealed class MqttService(ILogger<MqttService> logger, IBus bus) : BackgroundService
  {
    private static readonly MqttFactory MqttFactory = new();
    private readonly IManagedMqttClient _client = MqttFactory.CreateManagedMqttClient();

    public override void Dispose()
    {
      _client.Dispose();
      base.Dispose();
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
      logger.LogInformation("Starting MqttService");
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
      logger.LogInformation("Running MqttService");
      using (bus.Subscribe<SendMqttMessage>(msg => Send(msg.Topic, msg.Payload)))
      {
        MqttTopicFilter outgoing = new MqttTopicFilterBuilder().WithAtLeastOnceQoS().WithTopic("easywave2mqtt/#").Build();
        MqttTopicFilter incoming = new MqttTopicFilterBuilder().WithAtLeastOnceQoS().WithTopic("mqtt2easywave/#").Build();
        await _client.SubscribeAsync([outgoing, incoming]).ConfigureAwait(false);
        while (!stoppingToken.IsCancellationRequested)
        {
          await Task.Delay(100, stoppingToken).ConfigureAwait(false);
        }
      }
      stoppingToken.ThrowIfCancellationRequested();
      logger.LogInformation("Running MqttService ended");
    }

    private async Task MessageHandler(MqttApplicationMessageReceivedEventArgs arg)
    {
      logger.LogDebug("MQTT Topic {Topic} received with payload {Payload}", arg.ApplicationMessage.Topic, arg.ApplicationMessage.ConvertPayloadToString());
      arg.IsHandled = true;
      await arg.AcknowledgeAsync(CancellationToken.None).ConfigureAwait(false);
    }

    internal Task Send(string topic, string payload, bool retain = false)
    {
      logger.LogDebug("Sending topic {Topic} with payload {Payload} to MQTT", topic, payload);
      MqttApplicationMessage message = new MqttApplicationMessageBuilder().WithTopic(topic)
        .WithPayload(payload)
        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
        .WithRetainFlag(retain)
        .Build();
      return _client.EnqueueAsync(message);
    }

  }

}