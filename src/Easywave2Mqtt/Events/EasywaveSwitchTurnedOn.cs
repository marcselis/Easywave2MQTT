namespace Easywave2Mqtt.Events
{
    public record EasywaveSwitchTurnedOn
    {
        public EasywaveSwitchTurnedOn(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}
