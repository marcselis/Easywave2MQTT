namespace Tools.Messages
{
  public class SendEasywaveCommand(string address, char keyCode)
  {
    public string Address { get; } = address;
    public char KeyCode { get; } = keyCode;
  }
}
