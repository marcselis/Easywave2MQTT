namespace Easywave2Mqtt.Events
{
    public record EasywaveSwitchTurnedOff
    {
        public EasywaveSwitchTurnedOff(string id)
        {
            Id=id;
        }

        public string Id { get; }
    }
}
