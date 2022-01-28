namespace Easywave2Mqtt.Easywave
{
    public class EasywaveSubscription
    {
        public EasywaveSubscription(string address, char keyCode, bool canSend = false)
        {
            Address = address;
            KeyCode = keyCode;
            CanSend = canSend;
        }

        public string Address { get; }
        public char KeyCode { get; }
        public bool CanSend { get; }
    }
}
