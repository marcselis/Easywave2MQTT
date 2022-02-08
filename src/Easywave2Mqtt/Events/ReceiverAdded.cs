using Easywave2Mqtt.Configuration;

namespace Easywave2Mqtt.Events
{
  public record ReceiverAdded
  {
    public ReceiverAdded(string id, string name, string? area, bool isToggle, List<ListensTo> subcriptions)
    {
      Id = id;
      Name = name;
      Area = area;
      IsToggle = isToggle;
      ListensTo = subcriptions;
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public string? Area { get; set; }

    public bool IsToggle { get; set; }  

    public List<ListensTo> ListensTo { get; }
  }
}
