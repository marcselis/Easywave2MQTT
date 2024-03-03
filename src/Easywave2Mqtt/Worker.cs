using System.Collections.Concurrent;
using System.Reflection;
using Easywave2Mqtt.Configuration;
using Easywave2Mqtt.Easywave;
using Easywave2Mqtt.Events;
using Easywave2Mqtt.Messages;
using InMemoryBus;

namespace Easywave2Mqtt
{

  public sealed partial class Worker(IBus bus, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, Settings config) : BackgroundService
  {
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, IEasywaveDevice> _devices = new();
    private readonly ILogger<Worker> _logger = loggerFactory.CreateLogger<Worker>();

    public override void Dispose()
    {
      _cancellationTokenSource.Dispose();
      base.Dispose();
    }

    private async Task HandleMqttCommand(MqttCommand command)
    {
      if (_devices.TryGetValue(command.Address, out IEasywaveDevice? device))
      {
        await device.HandleCommand(command.Command).ConfigureAwait(false);
      }
      else
      {
        LogIgnoredMqttDevice(command.Address);
      }
    }

    private async Task HandleMqttMessage(MqttMessage message)
    {
      foreach (IEasywaveDevice device in _devices.Values)
      {
        if (device is IEasywaveEventListener eventListener)
        {
          await eventListener.HandleEvent(message.Address, message.KeyCode, message.Action).ConfigureAwait(false);
        }
      }
    }

    private async Task HandleEasywaveEvent(EasywaveTelegram telegram)
    {
      if (_devices.TryGetValue(telegram.Address, out IEasywaveDevice? device))
      {
        if (device is EasywaveTransmitter transmitter)
        {
          await transmitter.HandleButton(telegram.KeyCode).ConfigureAwait(false);
        }
      }
      else
      {
        LogIgnoredEasywaveDevice(telegram.Address);
      }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
      LogServiceStarting();
      await CreateDevices().ConfigureAwait(false);
      await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
      _cancellationTokenSource.Cancel();
      LogServiceStopping();
      return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      LogServiceRunning();
      try
      {
        using (bus.Subscribe<EasywaveTelegram>(HandleEasywaveEvent))
        using (bus.Subscribe<MqttCommand>(HandleMqttCommand))
        using (bus.Subscribe<MqttMessage>(HandleMqttMessage))
        {
          while (!_cancellationTokenSource.IsCancellationRequested)
          {
            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
          }
        }
      }
      catch(TaskCanceledException)
      {
        // Ignore
      }
      catch (OperationCanceledException) 
      {
        // Ignore
      }
      LogServiceStopped();
    }

    private async Task CreateDevices()
    {
      foreach (var device in config.Devices)
      {
        var id = device.Id ?? throw new InvalidConfigurationException("Device without Id found in settings");
        switch (device.Type)
        {
          case DeviceType.Blind:
          {
            var name = device.Name ?? $"Blind {device.Name}";
            EasywaveBlind? blind = await AddBlind(id, name, device.Area).ConfigureAwait(false);
            foreach (Subscription sub in device.Subscriptions)
            {
              var address = sub.Address ?? throw new InvalidConfigurationException($"Device {id} has a subscription without address");
              if (sub.CanSend)
              {
                blind.AddSubscription(address, sub.KeyCode, true);
              }
              else
              {
                var transmitter = config.Devices.FirstOrDefault(d => d.Type == DeviceType.Transmitter && d.Id == address) ?? throw new InvalidConfigurationException($"Blind {id} has a subscription for a non-existing device {address}");
                if (transmitter.Buttons.Contains(sub.KeyCode))
                {
                  blind.AddSubscription(address, sub.KeyCode);
                }
                else
                {
                  throw new InvalidConfigurationException($"Blind {id} has a subscription for a non-existing button {sub.KeyCode} on transmitter {address}");
                }
              }
            }
            break;
          }
          case DeviceType.Light:
          {
            var name = device.Name ?? $"Light {device.Name}";
            EasywaveSwitch? light = await AddLight(id, name, device.Area, device.IsToggle).ConfigureAwait(false);
            foreach (Subscription sub in device.Subscriptions)
            {
              var address = sub.Address ?? throw new InvalidConfigurationException($"Device {id} has a subscription without address");
              if (sub.CanSend)
              {
                light.AddSubscription(address, sub.KeyCode, true);
              }
              else
              {
                var transmitter = config.Devices.FirstOrDefault(d => d.Type == DeviceType.Transmitter && d.Id == address) ?? throw new InvalidConfigurationException($"Light {id} has a subscription for a non-existing device {address}");
                if (transmitter.Buttons.Contains(sub.KeyCode))
                {
                  light.AddSubscription(address, sub.KeyCode);
                }
                else
                {
                  throw new InvalidConfigurationException($"Light {id} has a subscription for a non-existing button {sub.KeyCode} on transmitter {address}");
                }
              }
            }
            break;
          }
          case DeviceType.Transmitter:
          {
            var name = device.Name ?? $"Transmitter {device.Name}";
            EasywaveTransmitter? transmitter = await AddTransmitter(id, name, device.Area, device.Buttons.Count).ConfigureAwait(false);
            var count = device.Buttons.Count;
            foreach (var button in device.Buttons)
            {
              transmitter.AddButton(await AddButton(id, button, name, device.Area, count).ConfigureAwait(false));
            }
            break;
          }
          default:
            throw new NotSupportedException($"Device {device.Id} has an unsupported type {device.Type}");
        }
      }
    }

    private async Task<EasywaveSwitch> AddLight(string id, string? name, string? area, bool isToggle)
    {
      LogLightDeclare(id, name, area, isToggle);
      var switchName = name ?? $"Lamp {id}";
      var newSwitch = new EasywaveSwitch(id, switchName, isToggle, loggerFactory.CreateLogger<EasywaveSwitch>());
      newSwitch.StateChanged += HandleEasywaveSwitchStateChanged;
      newSwitch.RequestSend += HandleEasywaveRequest;
      await bus.PublishAsync(new DeclareLight(id, switchName, area)).ConfigureAwait(false);
      AddDevice(newSwitch);
      return newSwitch;
    }

    private async Task<EasywaveBlind> AddBlind(string id, string? name, string? area)
    {
      LogBlindDeclare(id, name, area);
      var blindName = name ?? $"Blind {id}";
      var newBlind = new EasywaveBlind(id, blindName, loggerFactory.CreateLogger<EasywaveBlind>());
      newBlind.StateChanged += HandleEasywaveBlindStateChanged;
      newBlind.RequestSend += HandleEasywaveRequest;
      await bus.PublishAsync(new DeclareBlind(id, blindName, area)).ConfigureAwait(false);
      AddDevice(newBlind);
      return newBlind;
    }

    private Task HandleEasywaveRequest(string address, char keyCode)
    {
      return bus.PublishAsync(new SendEasywaveCommand(address, keyCode));
    }

    private Task<EasywaveTransmitter> AddTransmitter(string id, string name, string? area, int count)
    {
      var transmitter = new EasywaveTransmitter(id, name, area, count, loggerFactory.CreateLogger<EasywaveTransmitter>());
      AddDevice(transmitter);
      return Task.FromResult(transmitter);
    }

    private async Task<EasywaveButton> AddButton(string id, char keyCode, string name, string? area, int count)
    {
      LogButtonDeclare(id, keyCode, name, area);
      var button = new EasywaveButton(id, keyCode, name, area, loggerFactory.CreateLogger<EasywaveButton>());
      button.Pressed += HandleButtonPressed;
      button.DoublePressed += HandleButtonDoublePressed;
      button.TriplePressed += HandleButtonTriplePressed;
      button.Held += HandleButtonHeld;
      button.Released += HandleButtonReleased;
      //buttons are not put on the device list, as they are embedded in a transmitter
      await bus.PublishAsync(new DeclareButton(id, keyCode, name, area, count)).ConfigureAwait(false);
      return button;
    }

    private void AddDevice(IEasywaveDevice device)
    {
      if (!_devices.TryAdd(device.Id, device))
      {
        throw new ArgumentOutOfRangeException(nameof(device), $"Duplicate device id {device.Id} detected");
      }
    }

    private Task HandleButtonReleased(EasywaveButton button)
    {
      return bus.PublishAsync(new SendButtonRelease(button.Id, button.KeyCode));
    }

    private Task HandleButtonHeld(EasywaveButton button)
    {
      return bus.PublishAsync(new SendButtonHold(button.Id, button.KeyCode));
    }

    private Task HandleButtonTriplePressed(EasywaveButton button)
    {
      return bus.PublishAsync(new SendButtonTriplePress(button.Id, button.KeyCode));
    }

    private Task HandleButtonDoublePressed(EasywaveButton button)
    {
      return bus.PublishAsync(new SendButtonDoublePress(button.Id, button.KeyCode));
    }

    private Task HandleButtonPressed(EasywaveButton button)
    {
      return bus.PublishAsync(new SendButtonPress(button.Id, button.KeyCode));
    }

    private Task HandleEasywaveSwitchStateChanged(EasywaveSwitch sender)
    {
      if (sender.State == SwitchState.On)
      {
        return bus.PublishAsync(new EasywaveSwitchTurnedOn(sender.Id));
      }
      return bus.PublishAsync(new EasywaveSwitchTurnedOff(sender.Id));
    }

    private Task HandleEasywaveBlindStateChanged(EasywaveBlind sender)
    {
      return sender.State switch
      {
        BlindState.Opening => bus.PublishAsync(new EasywaveBlindIsOpening(sender.Id)),
        BlindState.Open => bus.PublishAsync(new EasywaveBlindIsOpen(sender.Id)),
        BlindState.Closing => bus.PublishAsync(new EasywaveBlindIsClosing(sender.Id)),
        BlindState.Closed => bus.PublishAsync(new EasywaveBlindIsClosed(sender.Id)),
        BlindState.Stopped => bus.PublishAsync(new EasywaveBlindIsStopped(sender.Id)),
        _ => HandleUnsupprtedState(sender.Id, sender.State),
      };
    }

    private Task HandleUnsupprtedState(string id, BlindState state)
    {
      LogUnsupportedBlindState(id, state); 
      return Task.CompletedTask;
    }

    #region Logging Methods

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Service is starting...")]
    public partial void LogServiceStarting();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Service is stopping...")]
    public partial void LogServiceStopping();

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Ignored incoming easywave message for unknown device {Address}")]
    public partial void LogIgnoredEasywaveDevice(string address);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Ignored incoming MQTT message for unknown device {Address}")]
    public partial void LogIgnoredMqttDevice(string address);

    [LoggerMessage(EventId = 22, Level = LogLevel.Information, Message = "Service running...")]
    public partial void LogServiceRunning();

    [LoggerMessage(EventId = 23, Level = LogLevel.Error, Message = "Service stopped...")]
    public partial void LogServiceStopped();

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "    Declaring button {Name} ({Id}:{KeyCode}) in {Area}")]
    public partial void LogButtonDeclare(string id, char keyCode, string name, string? area = "<<no area passed>>");

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "    Declaring light {Name} ({Id}:{IsToggle}) in {Area}")]
    public partial void LogLightDeclare(string id, string? name = "<<no name passed>>", string? area = "<<no area passed>>", bool isToggle = false);

    [LoggerMessage(EventId=8, Level =LogLevel.Information, Message    = "    Declaring blind {Name} ({Id}) in {Area}")]
    public partial void LogBlindDeclare(string id, string? name = "<<no name passed>>", string? area = "<<no area passed>>");

    [LoggerMessage(EventId=9, Level =LogLevel.Warning, Message="Detected an unsupported state {State} for blind {Id}!")]
    public partial void LogUnsupportedBlindState(string id, BlindState state);
    #endregion
  }

}