using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Easywave2Mqtt.Configuration
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class Device
  {
    public Device()
    {
      Id = "";
      Name = "";
    }

    public Device(string id, string name, DeviceType type, string? area, string buttons)
    {
      Id = id;
      Name = name;
      Type= type;
      Area = area;
      Buttons = buttons;
    }

    public Device(string id, string name, DeviceType type, string? area, bool isToggle, IEnumerable<ListensTo> subscriptions)
    {
      Id = id;
      Name = name;
      Type = type;
      Area = area;
      IsToggle = isToggle;
      ListensTo = new List<ListensTo>(subscriptions);
    }

    [Key]
    public string Id { get; set; }
    public DeviceType Type { get; set; } = DeviceType.Unknown;
    public string Name { get; set; }
    public string? Area { get; set; }
    public bool IsToggle { get; set; }
    public string? Buttons { get; set; }
    public List<ListensTo>? ListensTo { get; set; }
  }
}
