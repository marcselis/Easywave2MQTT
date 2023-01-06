namespace Easywave2Mqtt.Messages
{
  public record DeclareLight
  {
    public DeclareLight(string id, string name, string? area)
    {
      Id = id;
      Name = name;
      Area = area;
    }

    public string Id { get; }
    public string Name { get; }
    public string? Area { get; }
  }
}
