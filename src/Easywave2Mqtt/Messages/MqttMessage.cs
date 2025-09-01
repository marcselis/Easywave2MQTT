namespace Easywave2Mqtt.Messages
{
  internal sealed record MqttMessage(string Address, char KeyCode, string Action);
}