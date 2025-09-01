namespace Easywave2Mqtt.Easywave
{
  internal sealed record EasywaveSubscription(string Address, char KeyCode, bool CanSend = false);
}
