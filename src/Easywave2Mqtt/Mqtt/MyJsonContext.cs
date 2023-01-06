using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Mqtt
{
  [JsonSerializable(typeof(Light))]
  [JsonSerializable(typeof(Button))]
  internal partial class MyJsonContext : JsonSerializerContext
  {
  }
}