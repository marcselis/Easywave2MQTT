namespace Easywave2Mqtt.Events
{
  public record EasywaveBlindIsClosed
  {
    public EasywaveBlindIsClosed(string id)
    {
      Id = id;
    }

    public string Id { get; }

  }
}
