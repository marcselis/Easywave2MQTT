using System.Collections.Concurrent;

namespace Easywave2Mqtt.Easywave
{
  internal sealed partial class EasywaveTransmitter(string id, string name, string? area, int count, ILogger<EasywaveTransmitter> logger) : IEasywaveDevice
  {
    private readonly ILogger<EasywaveTransmitter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConcurrentDictionary<char, EasywaveButton> _buttons = new();

    public string Id { get; } = id;
    public string Name { get; } = name;
    public string? Area { get; } = area;

    public int Count { get; } = count;

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

    /// <exception cref="ArgumentNullException"><paramref name="button" /> is <see langword="null" />.</exception>
    public void AddButton(EasywaveButton button)
    {
      ArgumentNullException.ThrowIfNull(button);
      _buttons[button.KeyCode] = button;
    }
  }
}