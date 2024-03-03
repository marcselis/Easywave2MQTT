using InMemoryBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tools.Messages;

namespace EldatEmulator
{

  internal class Worker(IBus bus, ILogger<Worker> logger) : BackgroundService
  {
    private readonly Dictionary<ConsoleKey, Tuple<string, char>> _keys = new() { { ConsoleKey.T, new Tuple<string, char>("229ad6", 'A') } };
    private readonly Dictionary<ConsoleKey, string> _lampen = new() { { ConsoleKey.T, "terras" } };

    public override Task StartAsync(CancellationToken cancellationToken)
    {
      logger.LogInformation("Starting Worker");
      return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
      logger.LogInformation("Stopping Worker");
      return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      logger.LogInformation("Running Worker");
      while (!stoppingToken.IsCancellationRequested)
      {
        if (!Console.KeyAvailable)
        {
          await Task.Delay(5, stoppingToken).ConfigureAwait(false);
          continue;
        }
        await ProcessKey(Console.ReadKey(true)).ConfigureAwait(false);
      }
      stoppingToken.ThrowIfCancellationRequested();
    }

    private async Task ProcessKey(ConsoleKeyInfo keyInfo)
    {
      if ((keyInfo.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
      {
        await ProcessControlKey(keyInfo).ConfigureAwait(false);
        return;
      }
      if (_keys.TryGetValue(keyInfo.Key, out Tuple<string, char>? sw))
      {
        var val = sw.Item2;
        if ((keyInfo.Modifiers & ConsoleModifiers.Shift) > 0)
        {
          val = val switch
          {
            'A' => 'B'
                    ,
            'C' => 'D'
                    ,
            _ => throw new NotSupportedException($"Unsupported keycode {val}")
          };
        }
        await bus.PublishAsync(new SendEasywaveCommand(sw.Item1, val)).ConfigureAwait(false);
      }
    }

    private async Task ProcessControlKey(ConsoleKeyInfo keyInfo)
    {
      if (_lampen.TryGetValue(keyInfo.Key, out var val))
      {
        await bus.PublishAsync(new SendMqttMessage($"mqtt2easywave/{val}/switch", (keyInfo.Modifiers & ConsoleModifiers.Shift) > 0 ? "off" : "on")).ConfigureAwait(false);
      }
    }
  }

}