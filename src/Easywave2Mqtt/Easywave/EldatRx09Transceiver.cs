using System.Globalization;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using Easywave2Mqtt.Configuration;
using Easywave2Mqtt.Messages;
using InMemoryBus;

namespace Easywave2Mqtt.Easywave
{
  /// <summary>
  /// <see cref="IHostedService"/> implementation for the Eldat RX09 USB Easywave Transceiver.
  /// </summary>
  /// <remarks>
  /// You need to have the Eldat driver installed in order for this to work.
  /// <see cref="https://www.eldat.de/produkte/_div/rx09e_USBTcEasywaveInstall_XP_Win7.zip"/>
  /// </remarks>
  public sealed partial class EldatRx09Transceiver : BackgroundService
  {
    private readonly ILogger _logger;
    private readonly IBus _bus;
    private bool _isOpen;
    private readonly SerialPort? _port;
    private IDisposable? _subscription;
    private const int PauseTime = 100;
    private const int TimeoutResult = unchecked((int)0x800705B4);

    public EldatRx09Transceiver(ILogger<EldatRx09Transceiver> logger, IBus bus, Settings settings)
    {
      _logger = logger;
      _bus = bus;
      var portNames = SerialPort.GetPortNames();
      if (portNames.Length==0)
      {
        logger.LogError("No serial ports found!  Do you have the Eldat RX09 stick installed?");
        return;
      }
      var port = settings.SerialPort ?? throw new InvalidConfigurationException("SerialPort is not set in settings");
      logger.LogInformation("Checking serial port {port}", port);
      if (!SerialPort.GetPortNames().Contains(port))
      {
        logger.LogError("Serial port {port} is not found!", port);
        logger.LogInformation("List of serial ports found");
        foreach (var portName in portNames)
        {
          logger.LogInformation("   {port}", portName);
        }
        return;
      }
      _port = new SerialPort(port, 57600, Parity.None, 8, StopBits.One)
      {
        Handshake = Handshake.None,
        DtrEnable = true,
        RtsEnable = true,
        ReadTimeout = 1000,
        NewLine = "\r"
      };
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
      LogServiceStarting();
      if (_port == null)
      {
        _logger.LogError("No port available, service not started");
        return Task.CompletedTask;
      }
      Open();
      return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      LogServiceStopping();
      await base.StopAsync(cancellationToken);
      Close();
    }

    /// <summary>
    /// Gets the vendor id of the transceiver.
    /// </summary>
    /// <remarks>
    /// Works only after the transceiver is opened
    /// </remarks>
    public string? VendorId { get; private set; }
    /// <summary>
    /// Gets the device id of the transceiver.
    /// </summary>
    /// <remarks>
    /// Works only after the transceiver is opened
    /// </remarks>
    public string? DeviceId { get; private set; }
    /// <summary>
    /// Opens the transceiver and makes it listen for broadcasted Easywave <see cref="EasywaveTelegram"/>s.
    /// </summary>
    /// <remarks>
    /// This method also initializes the AddressCount, VendorId, DeviceId & Version properties with the
    /// information returned from the device.
    /// </remarks>
    private void Open()
    {
      LogOpenPort(_port!.PortName);
      _port.Open();
      _subscription = _bus.Subscribe<SendEasywaveCommand>(SendEasywaveCommand);
      _isOpen = true;
      Send("GETP?");
      Send("ID?");
    }

    private Task SendEasywaveCommand(SendEasywaveCommand message)
    {
      var address = int.Parse(message.Address, NumberStyles.HexNumber);
      if (address >= MaxAddress)
      {
        throw new ArgumentOutOfRangeException(nameof(message), $"Unable to send to address {address}, this transceiver only supports {MaxAddress} addresses");
      }
      Send($"TXP,{address:x2},{message.KeyCode}");
      return Task.CompletedTask;
    }

    private void Send(string message)
    {
      LogSendLine(message);
      _port!.WriteLine(message);
    }

    /// <summary>
    /// Closes the transceiver so that it stops listening.
    /// </summary>
    private void Close()
    {
      if (_port==null || !_isOpen)
      {
        return;
      }
      LogClosePort(_port.PortName);
      _port.Close();
      _subscription?.Dispose();
      _subscription = null;
      _isOpen = false;
      _port.Dispose();
    }

    //[System.Diagnostics.CodeAnalysis.SuppressMessage("Concurrency", "PH_P008:Missing OperationCanceledException in Task", Justification = "Background service should gracefully stop when cancellation token is set")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      LogServiceRunning();
      try
      {
        while (!stoppingToken.IsCancellationRequested && _port!.IsOpen)
        {
          try
          {
            var line = _port.ReadLine();
            LogReceivedLine(line);
            EasywaveTelegram telegram = Parse(line);
            if (!telegram.Equals(EasywaveTelegram.Empty))
            {
              //Only wait if timeout has occurred, otherwise the double- & triple-press detection mechanism doesn't work.
              await _bus.PublishAsync(telegram).ConfigureAwait(false);
            }
          }
          catch (TimeoutException)
          {
            if (!stoppingToken.IsCancellationRequested)
            {
              await Task.Delay(PauseTime, stoppingToken).ConfigureAwait(false);
            }
          }
          catch (IOException ex) when (ex.HResult == TimeoutResult)
          {
            if (!stoppingToken.IsCancellationRequested)
            {
              await Task.Delay(PauseTime, stoppingToken).ConfigureAwait(false);
            }
          }
        }
      }
      catch (OperationCanceledException) { 
        //ignore
      }
      LogServiceStopped();
    }

    public override void Dispose()
    {
      Close();
    }

    private EasywaveTelegram Parse(string line)
    {
      EasywaveTelegram? result = EasywaveTelegram.Empty;
      LogMethodStart1(line);
      ReadOnlySpan<char> span = line.AsSpan();
      if (span[0..2].CompareTo("OK", StringComparison.Ordinal) == 0)
      {
        LogConfirmation();
      }
      else if (span[0..2].CompareTo("ID", StringComparison.Ordinal) == 0)
      {
        VendorId = new string(span[3..7]);
        DeviceId = new string(span[8..12]);
        LogEldatDetected(VendorId, DeviceId);
      }
      else if (span[0..3].CompareTo("REC", StringComparison.Ordinal) == 0)
      {
        var address = new string(span[4..10]);
        var button = span[11];
        result = new EasywaveTelegram(address, button);
      }
      else if (span[0..4].CompareTo("GETP", StringComparison.Ordinal) == 0)
      {
        var addresses = new string(span[5..7]);
        MaxAddress = int.Parse(addresses, NumberStyles.HexNumber);
      }
      LogMethodEnd1(result.ToString());
      return result;
    }

    public int MaxAddress { get; set; }

    #region Logging Methods

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Easywave service is starting...")]
    private partial void LogServiceStarting();

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Easywave service is stopping...")]
    private partial void LogServiceStopping();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Service is running...")]
    private partial void LogServiceRunning();

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Service has stopped...")]
    private partial void LogServiceStopped();

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Sent {Line}")]
    private partial void LogSendLine(string line);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Received {Line}")]
    private partial void LogReceivedLine(string line);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Eldat Transceiver detected with identifier {VendorId}:{DeviceId}")]
    private partial void LogEldatDetected(string vendorId, string deviceId);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Eldat Transceiver confirmed last command")]
    private partial void LogConfirmation();

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Opening serial port {Port}")]
    private partial void LogOpenPort(string port);

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "Closing serial port {Port}")]
    private partial void LogClosePort(string port);

    [LoggerMessage(EventId = 98, Level = LogLevel.Trace, Message = "-->{Method}({Obj}) start")]
    private partial void LogMethodStart1(string obj, [CallerMemberName] string method = "");

    [LoggerMessage(EventId = 99, Level = LogLevel.Trace, Message = "<--{Method} end")]
    public partial void LogMethodEnd([CallerMemberName] string method = "");

    [LoggerMessage(EventId = 100, Level = LogLevel.Trace, Message = "<--{Method} returns {Result}")]
    private partial void LogMethodEnd1(string result, [CallerMemberName] string method = "");

    #endregion
  }
}