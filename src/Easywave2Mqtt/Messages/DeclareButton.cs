namespace Easywave2Mqtt.Messages
{
  internal sealed record DeclareButton(string Address, char KeyCode, string Name, string? Area, int Count);
}
