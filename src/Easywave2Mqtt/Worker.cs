using System.Collections.Concurrent;
using System.Globalization;
using Easywave2Mqtt.Configuration;
using Easywave2Mqtt.Easywave;
using Easywave2Mqtt.Events;
using Easywave2Mqtt.Messages;
using Easywave2Mqtt.Tools;

namespace Easywave2Mqtt
{
    public partial class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBus _bus;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Settings _config;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ConcurrentDictionary<string, IEasywaveDevice> _devices = new();

        public Worker(IBus bus, ILoggerFactory loggerFactory, Settings config)
        {
            _logger = loggerFactory.CreateLogger<Worker>();
            _bus = bus;
            _loggerFactory = loggerFactory;
            _config = config;
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
            LogWorkerStart();
            await CreateDevices().ConfigureAwait(false);
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            LogWorkerStop();
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
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            { }
            LogExecuteAsyncEnd();
        }

        private async Task CreateDevices()
        {
            foreach (Device device in _config.Devices)
            {
                var id = device.Id ?? throw new Exception("Device without Id found in settings");
                switch (device.Type)
                {
                    case DeviceType.Light:
                    {
                        var name = device.Name ?? $"Light {device.Name}";
                        var light = await AddLight(id, name, device.Area);
                        foreach (Subscription sub in device.Subscriptions)
                        {
                            var address = sub.Address ?? throw new Exception($"Device {id} has a subscription without address");
                            if (sub.CanSend)
                            {
                                light.AddSubscription(address, sub.KeyCode, true);
                            }
                            else
                            {
                                var transmitter = _config.Devices.FirstOrDefault(d => d.Type == DeviceType.Transmitter && d.Id == address);
                                if (transmitter == null)
                                {
                                    throw new Exception($"Device {id} has a subscription for a non-existing device {address}");
                                }

                                if (transmitter.Buttons.Contains(sub.KeyCode))
                                {
                                    light.AddSubscription(address, sub.KeyCode, false);
                                }
                                else
                                {
                                    throw new Exception($"Device {id} has a subscription for a non-existing button {sub.KeyCode} on transmitter {address}");
                                }
                            }
                        }
                        break;
                    }
                    case DeviceType.Transmitter:
                    {
                        var name = device.Name ?? $"Transmitter {device.Name}";
                        var transmitter = await AddTransmitter(id, name, device.Area, device.Buttons.Count);
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

        private async Task<EasywaveSwitch> AddLight(string id, string? name, string? area)
        {
            var switchName = name ?? $"Lamp {id}";
            var newSwitch = new EasywaveSwitch(id, switchName, _loggerFactory.CreateLogger<EasywaveSwitch>());
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

        private Task<EasywaveTransmitter> AddTransmitter(string id, string name, string? area, int count)
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

        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Ignored incoming easywave message for unknown device {Address}")]
        public partial void LogIgnoredEasywaveDevice(string address);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Ignored incoming MQTT message for unknown device {Address}")]
        public partial void LogIgnoredMqttDevice(string address);

        [LoggerMessage(EventId = 22, Level = LogLevel.Trace, Message = "-->ExecuteAsync")]
        public partial void LogExecuteAsyncStart();

        [LoggerMessage(EventId = 23, Level = LogLevel.Trace, Message = "<--ExecuteAsync")]
        public partial void LogExecuteAsyncEnd();

        [LoggerMessage(EventId = 13, Level = LogLevel.Trace, Message = "Worker service is starting...")]
        public partial void LogWorkerStart();

        [LoggerMessage(EventId = 14, Level = LogLevel.Trace, Message = "Worker service is stopping...")]
        public partial void LogWorkerStop();

        [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "    Declaring button {Name} ({Id}:{KeyCode}) in {Area}")]
        public partial void LogButtonDeclare(string id, char keyCode, string name, string? area = "<<no area passed>>");

        #endregion
    }
}
