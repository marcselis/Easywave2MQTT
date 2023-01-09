namespace Easywave2Mqtt.Messages
{
  public class SendEasywaveCommand
  {
    public SendEasywaveCommand(string address, char keyCode)
    {
      Address = address;
      KeyCode = keyCode;
    }

    public string Address { get; }
    public char KeyCode { get; }
  }
}
