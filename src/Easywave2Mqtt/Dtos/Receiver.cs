using System.ComponentModel.DataAnnotations;

namespace Easywave2Mqtt.Dtos
{
  public record Receiver
  {
    [Required]
    public string Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string? Area { get; set; }
    public bool IsToggle { get; set; }

    [Required]
    public List<Subscription> Subscriptions { get; } = new List<Subscription>();

    public Receiver(string id, string name, string? area, bool isToggle, IEnumerable<Subscription> subscriptions)
    {
      Id = id;
      Name = name;
      Area = area;
      IsToggle = isToggle;
      Subscriptions.AddRange(subscriptions);
    }
  }
}
