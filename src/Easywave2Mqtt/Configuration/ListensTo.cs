using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Easywave2Mqtt.Configuration
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class ListensTo
  {
    [Required]
    public string Address { get; set; }
    [Required]
    public char KeyCode { get; set; }
    public bool CanSend { get; set; }
    [JsonIgnore]
    public Device? Device { get; set; }

    public ListensTo()
    {
      Address = "";
    }

    public ListensTo(string address, char keyCode, bool canSend)
    {
      Address = address;
      KeyCode = keyCode;
      CanSend = canSend;
    }
  }
}