namespace Easywave2Mqtt.Messages
{
  public record SendButtonTriplePress
  {
    public SendButtonTriplePress(string address, char keyCode)
    {
      Address = address;
      KeyCode = keyCode;
    }

    public string Address { get; }
    public char KeyCode { get; }
  }
}
