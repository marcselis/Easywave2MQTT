using System.ComponentModel.DataAnnotations;

namespace Easywave2Mqtt.Dtos
{
  public record Subscription
  {
    [Required]
    public string Address { get; set; }
    [Required]
    public char KeyCode { get; set; }
    public bool CanSend { get; set; }

    public Subscription(string address, char keyCode, bool canSend)
    {
      Address = address;
      KeyCode = keyCode;
      CanSend = canSend;
    }
  }
}
