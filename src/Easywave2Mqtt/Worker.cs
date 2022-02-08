using System.Collections.Concurrent;
using Easywave2Mqtt.Configuration;
using Easywave2Mqtt.Easywave;
using Easywave2Mqtt.Events;
using Easywave2Mqtt.Messages;
using Easywave2Mqtt.Tools;
using Microsoft.EntityFrameworkCore;

namespace Easywave2Mqtt
{

  public partial class Worker : BackgroundService
  {
    private readonly IBus _bus;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Settings _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, IEasywaveDevice> _devices = new();
    private readonly ILogger<Worker> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public Worker(IBus bus, ILoggerFactory loggerFactory, Settings config, IServiceProvider serviceProvider)
    {
      _logger = loggerFactory.CreateLogger<Worker>();
      _bus = bus;
      _loggerFactory = loggerFactory;
      _config = config;
      _serviceProvider = serviceProvider;
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
      LogServiceStart();
      await CreateDevices().ConfigureAwait(false);
      await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
      LogServiceStop();
      return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      LogExecuteAsyncStart();
      try
      {
        using (_bus.Subscribe<EasywaveTelegram>(HandleEasywaveEvent))
        using (_bus.Subscribe<MqttCommand>(HandleMqttCommand))
        using (_bus.Subscribe<MqttMessage>(HandleMqttMessage))
        using (_bus.Subscribe<TransmitterAdded>(HandleTransmitterAdded))
        using (_bus.Subscribe<ReceiverAdded>(HandleReceiverAdded))
        {
          while (!_cancellationTokenSource.IsCancellationRequested)
          {
            await Task.Delay(1000, stoppingToken);
          }
        }
      }
      catch (OperationCanceledException) { }
      LogExecuteAsyncEnd();
    }

    private Task HandleTransmitterAdded(TransmitterAdded msg)
    {
      return AddNewTransmitter(msg.Id, msg.Name, msg.Area, msg.Buttons);
    }

    private Task HandleReceiverAdded(ReceiverAdded msg)
    {
      return AddNewLight(msg.Id, msg.Name, msg.Area, msg.IsToggle, msg.ListensTo);
    }


    private async Task CreateDevices()
    {
      using (var scope = _serviceProvider.CreateAsyncScope())
      {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (Device device in context.Devices.AsNoTracking())
        {
          var id = device.Id ?? throw new Exception("Device without Id found in settings");
          switch (device.Type)
          {
            case DeviceType.Light:
            {
              await AddNewLight(device.Id, device.Name, device.Area, device.IsToggle, device.ListensTo).ConfigureAwait(false);
              break;
            }
            case DeviceType.Transmitter:
            {
              await AddNewTransmitter(device.Id, device.Name, device.Area, device.Buttons).ConfigureAwait(false);
              break;
            }
            default:
              throw new NotSupportedException($"Device {device.Id} has an unsupported type {device.Type}");
          }
        }
      }
    }

    public async Task AddNewTransmitter(string id, string? name, string? area, string? buttons)
    {
      if (string.IsNullOrWhiteSpace(name))
      {
        name = $"Transmitter {id}";
      }
      if (string.IsNullOrWhiteSpace(buttons))
      {
        throw new Exception($"Transmitter {id} {name} has no buttons defined");
      }
      EasywaveTransmitter? transmitter = await CreateTransmitter(id, name, area, buttons.Length);
      var count = buttons.Length;
      foreach (var button in buttons)
      {
        transmitter.AddButton(await AddButton(id, button, name, area, count).ConfigureAwait(false));
      }
    }

    private async Task AddNewLight(string id, string? name, string? area, bool isToggle, IEnumerable<ListensTo>? subscriptions)
    {
      if (string.IsNullOrWhiteSpace(name))
      {
        name = $"Light {id}";
      }
      EasywaveSwitch light = await CreateLight(id, name, area, isToggle);
      if (subscriptions != null)
      {
        foreach (var sub in subscriptions)
        {
          var address = sub.Address ?? throw new Exception($"Device {id} has a subscription without address");
          if (sub.CanSend)
          {
            light.AddSubscription(address, sub.KeyCode, true);
          }
          else
          {
            if (_devices.Values.FirstOrDefault(d => d.Id == address) is not IEasywaveTransmitter transmitter)
            {
              throw new Exception($"Device {id} has a subscription for a non-existing transmitter with id {address}");
            }
            if (transmitter.HasButton(sub.KeyCode))
            {
              light.AddSubscription(address, sub.KeyCode);
            }
            else
            {
              throw new Exception($"Device {id} has a subscription for a non-existing button {sub.KeyCode} on transmitter {address}");
            }
          }
        }
      }
    }

    private async Task<EasywaveSwitch> CreateLight(string id, string? name, string? area, bool isToggle)
    {
      var switchName = name ?? $"Lamp {id}";
      var newSwitch = new EasywaveSwitch(id, switchName, isToggle, _loggerFactory.CreateLogger<EasywaveSwitch>());
      newSwitch.StateChanged += HandleEasywaveSwitchStateChanged;
      newSwitch.RequestSend += HandleEasywaveRequest;
      await _bus.PublishAsync(new DeclareLight(id, switchName, area)).ConfigureAwait(false);
      AddDevice(newSwitch);
      return newSwitch;
    }

    private Task HandleEasywaveRequest(string address, char keyCode)
    {
      return _bus.PublishAsync(new SendEasywaveCommand(address, keyCode));
    }

    private Task<EasywaveTransmitter> CreateTransmitter(string id, string name, string? area, int count)
    {
      var transmitter = new EasywaveTransmitter(id, name, area, count, _loggerFactory.CreateLogger<EasywaveTransmitter>());
      AddDevice(transmitter);
      return Task.FromResult(transmitter);
    }

    private async Task<EasywaveButton> AddButton(string id, char keyCode, string name, string? area, int count)
    {
      LogButtonDeclare(id, keyCode, name, area);
      var button = new EasywaveButton(id, keyCode, name, area, _loggerFactory.CreateLogger<EasywaveButton>());
      button.Pressed += HandleButtonPressed;
      button.DoublePressed += HandleButtonDoublePressed;
      button.TriplePressed += HandleButtonTriplePressed;
      button.Held += HandleButtonHeld;
      button.Released += HandleButtonReleased;
      //buttons are not put on the device list, as they are embedded in a transmitter
      await _bus.PublishAsync(new DeclareButton(id, keyCode, name, area, count)).ConfigureAwait(false);
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
      return _bus.PublishAsync(new SendButtonRelease(button.Id, button.KeyCode));
    }

    private Task HandleButtonHeld(EasywaveButton button)
    {
      return _bus.PublishAsync(new SendButtonHold(button.Id, button.KeyCode));
    }

    private Task HandleButtonTriplePressed(EasywaveButton button)
    {
      return _bus.PublishAsync(new SendButtonTriplePress(button.Id, button.KeyCode));
    }

    private Task HandleButtonDoublePressed(EasywaveButton button)
    {
      return _bus.PublishAsync(new SendButtonDoublePress(button.Id, button.KeyCode));
    }

    private Task HandleButtonPressed(EasywaveButton button)
    {
      return _bus.PublishAsync(new SendButtonPress(button.Id, button.KeyCode));
    }

    private async Task HandleEasywaveSwitchStateChanged(EasywaveSwitch sender)
    {
      if (sender.State == State.On)
      {
        await _bus.PublishAsync(new EasywaveSwitchTurnedOn(sender.Id));
      }
      else
      {
        await _bus.PublishAsync(new EasywaveSwitchTurnedOff(sender.Id));
      }
    }

    #region Logging Methods

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Worker service is starting...")]
    public partial void LogServiceStart();

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Worker service is stopping...")]
    public partial void LogServiceStop();

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Ignored incoming easywave message for unknown device {Address}")]
    public partial void LogIgnoredEasywaveDevice(string address);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Ignored incoming MQTT message for unknown device {Address}")]
    public partial void LogIgnoredMqttDevice(string address);

    [LoggerMessage(EventId = 22, Level = LogLevel.Trace, Message = "-->ExecuteAsync")]
    public partial void LogExecuteAsyncStart();

    [LoggerMessage(EventId = 23, Level = LogLevel.Trace, Message = "<--ExecuteAsync")]
    public partial void LogExecuteAsyncEnd();

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "    Declaring button {Name} ({Id}:{KeyCode}) in {Area}")]
    public partial void LogButtonDeclare(string id, char keyCode, string name, string? area = "<<no area passed>>");

    #endregion
  }

}