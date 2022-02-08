using System.ComponentModel.DataAnnotations;

namespace Easywave2Mqtt.Dtos
{
  public record Transmitter
  {
    [Required]
    public string Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string? Area { get; set; }
    [Required]
    public string Buttons { get; set; }

    public Transmitter(string id, string name, string? area, string buttons)
    {
      Id = id;
      Name = name;
      Area = area;
      Buttons = buttons;
    }
  }
}
