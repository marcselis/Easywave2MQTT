using System.Globalization;
using Easywave2Mqtt.Configuration;
using Easywave2Mqtt.Easywave;
using Easywave2Mqtt.Mqtt;
using InMemoryBus;
using Serilog;
using Serilog.Core;
using Serilog.Events;

[assembly: CLSCompliant(false)]

namespace Easywave2Mqtt
{

  public static class Program
  {
    public static Settings? Settings { get; set; }

    public static void Main(string[] args)
    {
      LoggingLevelSwitch logLevelSwitch = ConfigureSerilog();
      var app = Host.CreateDefaultBuilder(args)
       .UseSerilog()
        .ConfigureAppConfiguration((context, bld) => bld
          .SetBasePath(context.HostingEnvironment.ContentRootPath)
          .AddJsonFile("appsettings.json", false, true)
          .AddJsonFile(Path.Combine(Directory.GetDirectoryRoot("."), "data", "options.json"), true, true)
          .AddEnvironmentVariables())
      .ConfigureServices((_, services) =>
          //Configure the services needed to run everything
          services.AddSingleton<IBus, Bus>()
                  .AddSingleton(svc =>
                  {
                    var config = new Settings();
                    svc.GetRequiredService<IConfiguration>().Bind(config);
                    return config;
                  })
                  .AddHostedService<MessagingService>()
                  .AddHostedService<Worker>()
                  .AddHostedService<EldatRx09Transceiver>()
                  )
      .UseConsoleLifetime()
      .Build();

      //Configure default loglevel from settings
      Settings = app.Services.GetRequiredService<Settings>();
      logLevelSwitch.MinimumLevel = Settings.LogLevel;
      try
      {
        app.Run();
      }
      catch (OperationCanceledException)
      {
        //ignore
      }
    }

    private static LoggingLevelSwitch ConfigureSerilog()
    {
      var logLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Verbose);
      Log.Logger = new LoggerConfiguration()
          .MinimumLevel.ControlledBy(logLevelSwitch)
          .Enrich.FromLogContext()
          .WriteTo.Console(
              outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}",
              formatProvider: CultureInfo.InvariantCulture)
          .CreateLogger();
      return logLevelSwitch;
    }
  }

}