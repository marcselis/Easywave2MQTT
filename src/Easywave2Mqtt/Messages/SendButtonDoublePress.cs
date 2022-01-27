namespace Easywave2Mqtt.Messages
{
    public record SendButtonDoublePress
    {
        public SendButtonDoublePress(string address, char keyCode)
        {
            Address=address;
            KeyCode=keyCode;
        }

        public string Address { get; }
        public char KeyCode { get; }
    }
}
