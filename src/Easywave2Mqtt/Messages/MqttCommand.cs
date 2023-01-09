namespace Easywave2Mqtt.Messages
{
  public record MqttCommand
  {
    public MqttCommand(string address, string command)
    {
      Address = address;
      Command = command;
    }

    public string Address { get; }
    public string Command { get; }
  }
}