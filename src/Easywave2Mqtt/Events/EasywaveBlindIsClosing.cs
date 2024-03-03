namespace Easywave2Mqtt.Events
{
  public record EasywaveBlindIsClosing
  {
    public EasywaveBlindIsClosing(string id)
    {
      Id = id;
    }

    public string Id { get; }
  }
}