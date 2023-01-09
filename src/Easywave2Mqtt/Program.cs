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

  internal static class Program
  {
    public static Settings? Settings { get; set; }

    public static void Main(string[] args)
    {
      var logLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Verbose);
      Log.Logger = new LoggerConfiguration()
          .MinimumLevel.ControlledBy(logLevelSwitch)
          .Enrich.FromLogContext()
          .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}")
          .CreateLogger();
      IHost app = CreateHostBuilder(args).Build();
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

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
      IHostBuilder? builder = Host.CreateDefaultBuilder(args);
      _ = builder.UseSerilog()
        .ConfigureAppConfiguration((context, bld) => bld
          .SetBasePath(context.HostingEnvironment.ContentRootPath)
          .AddJsonFile("appsettings.json", false, true)
          .AddJsonFile(Path.Combine(Directory.GetDirectoryRoot("."), "data", "options.json"), true, true)
          .AddEnvironmentVariables());
      return builder.ConfigureServices((_, services) =>
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
                    .UseConsoleLifetime();
    }
  }

}