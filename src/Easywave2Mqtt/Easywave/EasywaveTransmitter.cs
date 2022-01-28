using System.Collections.Concurrent;

namespace Easywave2Mqtt.Easywave
{
    public partial class EasywaveTransmitter : IEasywaveDevice
    {
        private readonly ILogger<EasywaveTransmitter> _logger;
        private readonly ConcurrentDictionary<char, EasywaveButton> _buttons = new();

        public EasywaveTransmitter(string id, string name, string? area, int count, ILogger<EasywaveTransmitter> logger)
        {
            Id = id;
            Name = name;
            Area = area;
            Count = count;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id { get; }
        public string Name { get; }
        public string? Area { get; }

        public int Count { get; }

        public Task HandleCommand(string command)
        {
            return Task.CompletedTask;
        }

        public async Task HandleButton(char buttonId)
        {
            LogHandleButtonStart(Id, buttonId);
            if (_buttons.TryGetValue(buttonId, out EasywaveButton? button))
            {
                await button.HandlePress().ConfigureAwait(false);
            }
            LogHandleButtonEnd(Id, buttonId);
        }

        [LoggerMessage(EventId = 13, Level = LogLevel.Trace, Message = "-->HandleButton {Id}:{KeyCode}")]
        public partial void LogHandleButtonStart(string id, char keyCode);

        [LoggerMessage(EventId = 14, Level = LogLevel.Trace, Message = "<--HandleButton {Id}:{KeyCode}")]
        public partial void LogHandleButtonEnd(string id, char keyCode);

        public void AddButton(EasywaveButton button)
        {
            _buttons[button.KeyCode] = button;
        }
    }
}