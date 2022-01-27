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
            return Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((_, services) =>
                    //Configure the services needed to run everything
                    services
                        .AddSingleton<IBus, Bus>()
                        .AddSingleton(svc =>
                        {
                            var config = new Settings();
                            svc.GetRequiredService<IConfiguration>()
                                .Bind("Settings", config);
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