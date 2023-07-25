using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Mqtt
{
  [JsonSerializable(typeof(Light))]
  [JsonSerializable(typeof(Button))]
  [JsonSerializable(typeof(Cover))]
  internal partial class MyJsonContext : JsonSerializerContext
  {
  }
}