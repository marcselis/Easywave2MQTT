// See https://aka.ms/new-console-template for more information

using EldatEmulator;
using InMemoryBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    IHostBuilder? builder = Host.CreateDefaultBuilder(args);
    _ = builder.ConfigureLogging((_, bld) => bld.AddSimpleConsole(options =>
                                                                  {
                                                                      options.SingleLine = true;
                                                                      options.TimestampFormat = "HH:mm:ss ";
                                                                  }));
    _ = builder.ConfigureServices((__, services) =>
                                  {
                                      _ = services.AddSingleton<IBus, Bus>();
                                      _ = services.AddHostedService<Eldat>();
                                      _ = services.AddHostedService<Worker>();
                                      _ = services.AddHostedService<MqttService>();
                                  });
    _ = builder.UseConsoleLifetime();
    IHost app = builder.Build();
    await app.RunAsync().ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    //ignore
}