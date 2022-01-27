namespace Easywave2Mqtt.Messages
{
    public record SendButtonHold
    {
        public SendButtonHold(string address, char keyCode)
        {
            Address=address;
            KeyCode=keyCode;
        }

        public string Address { get; }
        public char KeyCode { get; }
    }
}
