namespace Easywave2Mqtt.Easywave
{
    public record EasywaveTelegram
    {
        public static readonly EasywaveTelegram Empty = new(string.Empty, char.MinValue);

        public EasywaveTelegram(string address, char keyCode)
        {
            Address = address;
            KeyCode = keyCode;
        }

        public string Address { get; }
        public char KeyCode { get; }
    }
}