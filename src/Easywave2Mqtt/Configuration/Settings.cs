using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace Easywave2Mqtt.Configuration
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Settings
    {
        public string? SerialPort { get; set; }
        public Collection<Device> Devices { get; set; } = new Collection<Device>();
    }
}