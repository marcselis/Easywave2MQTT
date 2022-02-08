namespace Easywave2Mqtt.Events
{
  public record TransmitterAdded
  {
    public TransmitterAdded(string id, string name, string? area, string buttons)
    {
      Id = id;
      Name = name;
      Area = area;
      Buttons = buttons;
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public string? Area { get; set; }

    public string Buttons { get; set; }
  }
}
