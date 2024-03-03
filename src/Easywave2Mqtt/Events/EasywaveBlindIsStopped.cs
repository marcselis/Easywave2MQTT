namespace Easywave2Mqtt.Events
{
  public record EasywaveBlindIsStopped
  {
    public EasywaveBlindIsStopped(string id)
    {
      Id = id;
    }

    public string Id { get; }
  }
}