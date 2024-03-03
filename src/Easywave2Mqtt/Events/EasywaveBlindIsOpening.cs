namespace Easywave2Mqtt.Events
{
  public record EasywaveBlindIsOpening
  {
    public EasywaveBlindIsOpening(string id)
    {
      Id = id;
    }

    public string Id { get; }
  }
}