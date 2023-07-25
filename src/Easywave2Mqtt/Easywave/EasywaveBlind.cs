using System.Collections.ObjectModel;

namespace Easywave2Mqtt.Easywave
{
  public partial class EasywaveBlind : IEasywaveEventListener
  {
    private readonly ILogger<EasywaveBlind> _logger;
    private BlindState _state = BlindState.Open;

    public EasywaveBlind(string id, string name, ILogger<EasywaveBlind> logger)
    {
      Id = id;
      Name = name;
      _logger=logger;
    }

    public string Name { get; set; }
    public bool IsToggle { get; }

    public BlindState State
    {
      get { return _state; }
      private set
      {
        if (_state == value)
        {
          return;
        }
        _state = value;
        LogStateSwitch(Name, _state);
        _ = StateChanged?.Invoke(this);
      }
    }

    public Collection<EasywaveSubscription> Subscriptions { get; } = new();

    public string Id { get; set; }

    public event BlindStateChanged? StateChanged;
    public event RequestSend? RequestSend;

    public async Task HandleCommand(string command)
    {
      EasywaveSubscription? trigger = Subscriptions.FirstOrDefault(sub => sub.CanSend);
      if (trigger != null)
      {
        if (RequestSend != null)
        {
          switch (command.ToLower())
          {
            case "open":
              await RequestSend(trigger.Address, trigger.KeyCode).ConfigureAwait(false);
              State = BlindState.Open;
              break;
            case "close":
              await RequestSend(trigger.Address, (char)(trigger.KeyCode + 1)).ConfigureAwait(false);
              State = BlindState.Closed;
              break;
            case "stop":
              await RequestSend(trigger.Address, (char)(trigger.KeyCode + 2)).ConfigureAwait(false);
              break;
          }
        }
      }
    }

    public Task HandleEvent(string address, char keyCode, string action)
    {
      EasywaveSubscription? subscription = Subscriptions.FirstOrDefault(s => s.Address == address);
      if (subscription != null)
      {
        switch (keyCode)
        {
          case 'A':
            State = BlindState.Open;
            break;
          case 'B':
            State = BlindState.Closed;
            break;
        }
      }
      return Task.CompletedTask;
    }

    public void AddSubscription(string address, char keyCode, bool canSend = false)
    {
      Subscriptions.Add(new EasywaveSubscription(address, keyCode, canSend));
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Blind {Name} is switched to {State}")]
    public partial void LogStateSwitch(string name, BlindState state);

  }

}