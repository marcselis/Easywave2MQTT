using Easywave2MQTT;
using InMemoryBus;

Console.WriteLine("Hello, World!");

var tokenSource = new CancellationTokenSource();
var bus = new Bus();

bus.Subscribe<LightStateChanged>((e) =>
{
  Console.WriteLine($"Light {e.LightId} changed to state {e.NewState}");
  return Task.CompletedTask;
});
bus.Subscribe<LightStateChanged>((e) =>
{
  Console.WriteLine($"Light {e.LightId} changed also to state {e.NewState}");
  return Task.CompletedTask;
});


await bus.PublishAsync(new LightStateChanged("LivingRoom", "On")).ConfigureAwait(false);

Console.ReadKey(true);

namespace Easywave2MQTT
{
}