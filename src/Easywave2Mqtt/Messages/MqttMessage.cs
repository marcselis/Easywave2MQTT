namespace Easywave2Mqtt.Messages
{
    public record MqttMessage
    {
        public MqttMessage(string address, char keyCode, string action)
        {
            Address = address;
            KeyCode = keyCode;
            Action = action;
        }
        public string Address { get; }
        public char KeyCode { get; }
        public string Action { get; }
    }
}