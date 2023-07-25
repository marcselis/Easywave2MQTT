namespace Easywave2Mqtt.Events
{
  public record EasywaveBlindIsOpen
  {
    public EasywaveBlindIsOpen(string id)
    {
      Id = id;
    }

    public string Id { get; }

  }
}
