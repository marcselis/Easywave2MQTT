namespace Easywave2Mqtt.Messages
{
    public record DeclareButton
    {
        public DeclareButton(string address, char keyCode, string name, string? area, int count)
        {
            Address=address;
            KeyCode=keyCode;
            Name = name;
            Area = area;
            Count = count;
        }

        public string Address { get; }
        public char KeyCode { get; }
        public string Name { get; }
        public string? Area { get; }
        public int Count { get; }
    }
}
