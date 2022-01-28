namespace Easywave2Mqtt.Messages
{
    public record SendButtonPress
    {
        public SendButtonPress(string address, char keyCode)
        {
            Address = address;
            KeyCode = keyCode;
        }

        public string Address { get; }
        public char KeyCode { get; }
    }
}
