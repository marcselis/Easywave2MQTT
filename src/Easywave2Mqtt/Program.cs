using Easywave2Mqtt.Configuration;
using Easywave2Mqtt.Easywave;
using Easywave2Mqtt.Mqtt;
using Easywave2Mqtt.Tools;
using Microsoft.EntityFrameworkCore;

namespace Easywave2Mqtt
{

  internal static class Program
  {
    public static Settings? Settings { get; set; }

    public static void Main(string[] args)
    {
      try
      {
        var builder = WebApplication.CreateBuilder(args);
        _ = builder.Services.AddControllers();
        _ = builder.Services.AddEndpointsApiExplorer();
        _ = builder.Services.AddSwaggerGen();
        _ = builder.Services.AddSingleton<IBus, Bus>();
        _ = builder.Services.AddSingleton<Settings>();
        //        _ = builder.Services.AddHostedService<MessagingService>();
        _ = builder.Services.AddDbContext<AppDbContext>();
        _ = builder.Services.AddHostedService<Worker>();
//        _ = builder.Services.AddHostedService<EldatRx09Transceiver>();
        var app = builder.Build();
        Settings = app.Services.GetRequiredService<Settings>();
        _ = app.UseSwagger();
        _ = app.UseSwaggerUI();
        _ = app.UseHttpsRedirection();
        _ = app.UseAuthorization();
        _ = app.MapControllers();
        SetupDb.Prepare(app);
        app.Run();
      }
      catch (OperationCanceledException)
      {
        //ignore
      }
    }
  }

}