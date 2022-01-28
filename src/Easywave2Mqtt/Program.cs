using Easywave2Mqtt.Configuration;
using Easywave2Mqtt.Easywave;
using Easywave2Mqtt.Mqtt;
using Easywave2Mqtt.Tools;

[assembly: CLSCompliant(false)]

namespace Easywave2Mqtt
{

  internal static class Program
  {
    public static void Main(string[] args)
    {
      try
      {
        CreateHostBuilder(args).Build().Run();
      }
      catch (OperationCanceledException)
      {
        //ignore
      }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
      IHostBuilder? builder = Host.CreateDefaultBuilder(args);
      switch (Environment.OSVersion.Platform)
      {
        case PlatformID.Unix:
          _ = builder.UseSystemd();
          break;
        case PlatformID.Win32NT:
          _ = builder.UseWindowsService(options =>
                                        {
                                          options.ServiceName = "Easywave2Mqtt Service";
                                        });
          break;
        default:
          throw new NotSupportedException($"Unsupported platform {Environment.OSVersion.Platform}");
      }
      return builder.ConfigureServices((_, services) =>
                                         //Configure the services needed to run everything
                                         services.AddSingleton<IBus, Bus>()
                                                 .AddSingleton(svc =>
                                                               {
                                                                 var config = new Settings();
                                                                 svc.GetRequiredService<IConfiguration>().Bind("Settings", config);
                                                                 return config;
                                                               })
                                                 .AddHostedService<MessagingService>()
                                                 .AddHostedService<Worker>()
                                                 .AddHostedService<EldatRx09Transceiver>())
                    .UseConsoleLifetime();
    }
  }

}