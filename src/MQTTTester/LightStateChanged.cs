// See https://aka.ms/new-console-template for more information
namespace Easywave2MQTT
{
  public record LightStateChanged(string LightId, string NewState);
}