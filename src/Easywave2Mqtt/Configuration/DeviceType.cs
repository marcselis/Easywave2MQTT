using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Easywave2Mqtt.Configuration
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DeviceType
    {
        [EnumMember(Value = "unknown")]
        Unknown,
        [EnumMember(Value = "light")]
        Light,
        [EnumMember(Value = "transmitter")]
        Transmitter
    }
}