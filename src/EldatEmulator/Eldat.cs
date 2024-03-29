﻿using System.Globalization;
using System.IO.Ports;
using InMemoryBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tools.Messages;

namespace EldatEmulator
{

  internal class Eldat : BackgroundService
  {
    private readonly SerialPort _port;
    private readonly IBus _bus;
    private readonly ILogger<Eldat> _logger;
    private ISubscription<SendEasywaveCommand>? _subscription;

    public Eldat(IBus bus, ILogger<Eldat> logger)
    {
      _bus = bus;
      _logger = logger;
      _port = new SerialPort("COM2", 57600, Parity.None, 8, StopBits.One)
      {
        Handshake = Handshake.None,
        DtrEnable = true,
        RtsEnable = true,
        ReadTimeout = 100,
        NewLine = "\r"
      };
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("Starting Eldat");
      if (!cancellationToken.IsCancellationRequested)
      {
        _port.Open();
        _subscription = _bus.Subscribe<SendEasywaveCommand>(SendEasywaveCommand);
      }
      _logger.LogDebug("Started Eldat");
      return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("Stopping Eldat");
      _port.Close();
      _subscription?.Dispose();
      _subscription = null;
      _logger.LogDebug("Stopped Eldat");
      return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("Running Eldat");
      while (!stoppingToken.IsCancellationRequested)
      {
        if (_port.IsOpen)
        {
          ReadAndProcessLine();
        }
        await Task.Delay(100, stoppingToken).ConfigureAwait(false);
      }
      stoppingToken.ThrowIfCancellationRequested();
    }

    private void ReadAndProcessLine()
    {
      if (_port.BytesToRead == 0)
      {
        return;
      }

      try
      {
        _logger.LogTrace("Reading line from port");
        var line = _port.ReadLine();
        _logger.LogTrace("Received {Line}",line);
        switch (line)
        {
          case "GETP?":
            _logger.LogTrace("-->GETP 80");
            _port.WriteLine("GETP 80");
            break;
          case "ID?":
            _logger.LogTrace("-->ID 1234:4321");
            _port.WriteLine("ID 1234:4321");
            break;
        }
      }
      catch (TimeoutException)
      {
        // Ignore
      }
      catch (IOException ex) when (ex.Message.StartsWith("This operation returned because the timeout period expired."))
      {
        // Ignore
      }
    }

    private Task SendEasywaveCommand(SendEasywaveCommand message)
    {
      var address = int.Parse(message.Address, NumberStyles.HexNumber);
      _logger.LogTrace("Sent REC,{Address:x2},{KeyCode}",address, message.KeyCode);
      _port.WriteLine($"REC,{address:x2},{message.KeyCode}");
      return Task.CompletedTask;
    }

  }

}